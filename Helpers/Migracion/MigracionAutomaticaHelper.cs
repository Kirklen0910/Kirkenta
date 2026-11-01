using Kirkenta.Data;
using Microsoft.EntityFrameworkCore;
using static Kirkenta.Helpers.Migracion.MigracionHistorialHelper;

namespace Kirkenta.Helpers.Migracion
{
    public class PasoMigracion
    {
        public int Orden { get; set; }
        /// <summary>Clave de catálogo (Productos, Clientes…) o «@ventas», «@cxc», «@cxp», «@kardex». Vacío = no migrar.</summary>
        public string Destino { get; set; } = "";
        public string Titulo { get; set; } = "";
        public int TablaIndice { get; set; }
        public string TablaNombre { get; set; } = "";
        public int Filas { get; set; }
        public bool Incluir { get; set; }
        public string? Motivo { get; set; }
        public string Confianza { get; set; } = "";
        public Dictionary<string, string> Mapa { get; set; } = new();
        public int? DetalleIndice { get; set; }
        public Dictionary<string, string> MapaDetalle { get; set; } = new();
        public bool EsHistorial => Destino.StartsWith("@");
        public string Tipo => Destino.TrimStart('@');
    }

    public class ResultadoPaso
    {
        public PasoMigracion Paso { get; set; } = new();
        public Resultado? Res { get; set; }
        public string? Error { get; set; }
    }

    public class ResultadoAutomatico
    {
        public bool Aplicado { get; set; }
        public bool SoloRevision { get; set; }
        public string? ErrorFatal { get; set; }
        public string? Respaldo { get; set; }
        public List<ResultadoPaso> Pasos { get; } = new();
        public bool Ok => ErrorFatal == null;
    }

    /// <summary>
    /// Migración automática de punta a punta: detecta qué es cada tabla, la ordena según sus dependencias
    /// (impuestos → unidades → categorías → productos → clientes → proveedores → ventas → saldos → kardex)
    /// y la importa TODA en una sola transacción: o entra completa o no entra nada.
    /// </summary>
    public static class MigracionAutomaticaHelper
    {
        private static int OrdenDe(string destino)
        {
            var i = Array.IndexOf(MigracionCatalogosHelper.Orden, destino);
            if (i >= 0) return i + 1;
            return destino switch { "@ventas" => 7, "@cxc" => 8, "@cxp" => 9, "@kardex" => 10, _ => 99 };
        }

        public static string TituloDe(string destino) =>
            destino.StartsWith("@") ? MigracionHistorialHelper.NombreTipo(destino.Substring(1)) :
            destino.Length == 0 ? "No migrar" : MigracionCatalogosHelper.Nombre(destino);

        /// <summary>Opciones de destino para el selector de cada tabla.</summary>
        public static readonly string[] Destinos =
        {
            "Impuestos", "UnidadesMedida", "Categorias", "Productos", "Clientes", "Proveedores",
            "@ventas", "@cxc", "@cxp", "@kardex"
        };

        public static List<PasoMigracion> Planificar(List<TablaOrigen> tablas, Dictionary<int, string>? elegidos = null)
        {
            var pasos = new List<PasoMigracion>();
            var detecciones = MigracionAnalizador.Analizar(tablas);

            // 1) destino sugerido por tabla
            var destino = new string[tablas.Count];
            var motivo = new string?[tablas.Count];
            var confianza = new string[tablas.Count];
            for (int i = 0; i < tablas.Count; i++)
            {
                var d = detecciones[i];
                confianza[i] = d.Confianza;
                if (elegidos != null && elegidos.TryGetValue(i, out var e))
                {
                    destino[i] = e ?? "";
                    confianza[i] = "Elegido";
                    continue;
                }
                if (d.Entidad != null && (d.Confianza == "Alta" || d.Confianza == "Media")) destino[i] = d.Entidad.Key;
                else if (d.TipoHistorial != null) destino[i] = "@" + d.TipoHistorial;
                else
                {
                    destino[i] = "";
                    motivo[i] = d.Entidad != null ? $"Parece «{d.Entidad.Nombre}» pero con poca seguridad: elige el destino si quieres migrarla."
                        : d.PosibleFuturo ?? "No se reconoció qué contiene.";
                }
            }

            // 2) tablas de líneas de venta: se unen a la tabla de ventas, no se importan solas
            var detalleDe = new Dictionary<int, int>();     // ventas → detalle
            for (int i = 0; i < tablas.Count; i++)
            {
                if (destino[i] != "@ventas") continue;
                var det = SugerirTablaDetalle(tablas, i);
                if (det != null && !detalleDe.ContainsValue(det.Value)) detalleDe[i] = det.Value;
            }
            foreach (var par in detalleDe)
            {
                if (elegidos != null && elegidos.ContainsKey(par.Value)) continue;
                destino[par.Value] = "";
                motivo[par.Value] = $"Líneas de venta: se unen a «{tablas[par.Key].Nombre}».";
            }

            // 3) si la tabla de ventas ya trae el saldo pendiente, la tabla de cuentas por cobrar sobra
            bool ventasConSaldo = Enumerable.Range(0, tablas.Count).Any(i => destino[i] == "@ventas"
                && MigracionHistorialHelper.Detectar(tablas[i].Headers, Campos(TipoVentas))["Saldo"] != null);

            // 4) armar pasos
            for (int i = 0; i < tablas.Count; i++)
            {
                var p = new PasoMigracion
                {
                    TablaIndice = i, TablaNombre = tablas[i].Nombre, Filas = tablas[i].Rows.Count,
                    Destino = destino[i], Titulo = TituloDe(destino[i]), Confianza = confianza[i],
                    Orden = OrdenDe(destino[i]), Motivo = motivo[i]
                };

                if (destino[i].Length == 0) { pasos.Add(p); continue; }

                if (p.EsHistorial)
                {
                    var tipo = p.Tipo;
                    var campos = Campos(tipo);
                    p.Mapa = MigracionHistorialHelper.Detectar(tablas[i].Headers, campos)
                        .Where(x => x.Value != null).ToDictionary(x => x.Key, x => x.Value!);
                    var falta = campos.Where(c => c.Requerido && !p.Mapa.ContainsKey(c.Key)).Select(c => c.Label).ToList();
                    if (tipo is TipoCxC or TipoCxP && !new[] { "Cliente", "ClienteCodigo", "ClienteRTN", "Proveedor", "ProveedorCodigo", "ProveedorRTN" }.Any(p.Mapa.ContainsKey))
                        falta.Add("cliente/proveedor");
                    if (tipo == TipoKardex && !p.Mapa.ContainsKey("ProductoCodigo") && !p.Mapa.ContainsKey("ProductoNombre"))
                        falta.Add("producto");

                    if (tipo == TipoVentas && detalleDe.TryGetValue(i, out var di))
                    {
                        p.DetalleIndice = di;
                        p.MapaDetalle = MigracionHistorialHelper.Detectar(tablas[di].Headers, CamposDetalle())
                            .Where(x => x.Value != null).ToDictionary(x => x.Key, x => x.Value!);
                    }

                    if (falta.Count > 0) p.Motivo = "No se encontró la columna: " + string.Join(", ", falta) + ".";
                    else if (tipo == TipoCxC && ventasConSaldo)
                        p.Motivo = "Los saldos pendientes ya vienen en la tabla de ventas; esta se omite para no duplicar.";
                    else p.Incluir = true;
                }
                else
                {
                    p.Mapa = MigracionCatalogosHelper.Detectar(tablas[i].Headers, destino[i]);
                    var falta = MigracionCatalogosHelper.Faltantes(destino[i], p.Mapa);
                    if (falta.Count > 0) p.Motivo = "No se encontró la columna: " + string.Join(", ", falta) + ".";
                    else p.Incluir = true;
                }
                pasos.Add(p);
            }

            return pasos.OrderBy(p => p.Orden).ThenBy(p => p.TablaIndice).ToList();
        }

        /// <summary>
        /// Ejecuta el plan. aplicar=false: lo corre completo y deshace al final (revisión exacta, no guarda nada).
        /// aplicar=true: guarda todo junto; si algo truena no queda nada.
        /// </summary>
        public static ResultadoAutomatico Ejecutar(ApplicationDbContext ctx, List<TablaOrigen> tablas,
            List<PasoMigracion> plan, int? usuarioId, bool aplicar)
        {
            var res = new ResultadoAutomatico { SoloRevision = !aplicar };

            if (!aplicar && !ctx.Database.IsRelational())
            {
                res.ErrorFatal = "La revisión completa necesita la base de datos del sistema (no está disponible en este entorno).";
                return res;
            }

            var propia = ctx.Database.CurrentTransaction == null;
            using var tx = propia ? ctx.Database.BeginTransaction() : null;
            PasoMigracion? actual = null;
            try
            {
                foreach (var paso in plan.Where(p => p.Incluir).OrderBy(p => p.Orden).ThenBy(p => p.TablaIndice))
                {
                    actual = paso;
                    var tabla = tablas[paso.TablaIndice];
                    var rp = new ResultadoPaso { Paso = paso };

                    if (paso.EsHistorial)
                    {
                        var op = new Opciones
                        {
                            Tipo = paso.Tipo, Mapa = paso.Mapa, Tolerante = true, TerceroNoExiste = "crear",
                            Detalle = paso.DetalleIndice.HasValue ? tablas[paso.DetalleIndice.Value] : null,
                            MapaDetalle = paso.MapaDetalle
                        };
                        rp.Res = Procesar(ctx, tabla, op, true, usuarioId);
                    }
                    else
                    {
                        rp.Res = MigracionCatalogosHelper.Procesar(ctx, tabla, paso.Destino, paso.Mapa, usuarioId, true);
                    }
                    res.Pasos.Add(rp);
                    ctx.ChangeTracker.Clear();
                }

                if (aplicar) { tx?.Commit(); res.Aplicado = true; }
                else tx?.Rollback();
            }
            catch (Exception ex)
            {
                tx?.Rollback();
                ctx.ChangeTracker.Clear();
                res.Aplicado = false;
                res.ErrorFatal = $"Se canceló todo y NO se guardó nada. Falló el paso «{actual?.Titulo} ({actual?.TablaNombre})»: {ex.InnerException?.Message ?? ex.Message}";
            }
            ctx.ChangeTracker.Clear();
            return res;
        }
    }
}
