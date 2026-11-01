using Kirkenta.Helpers;
using Kirkenta.Helpers.RRHH;
using Kirkenta.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

namespace Kirkenta.Services.Asistente
{
    /// <summary>
    /// Acciones de gestión del negocio: cobranza, respaldo, compras sugeridas, panel del dueño
    /// y aprobaciones de RRHH (vacaciones, permisos, vales) con contraseña.
    /// </summary>
    public static partial class AsistenteCatalogo
    {
        private static IEnumerable<AccionDef> Negocio() => new List<AccionDef>
        {
            new() { Id = "recordatorios_cobro", Modulo = "Finanzas", Titulo = "Recordatorios de cobro", Descripcion = "Clientes con saldo vencido y acceso directo para recordarles por WhatsApp",
                Ejemplo = "a quién le toca cobrar", PermModulo = "Finanzas", PermSub = "Cobros",
                Frases = new[] { "recordatorio* cobro*", "recordatorio* cobranza", "recordatorio* pago*", "cobranza", "a quien cobr*", "quien me debe", "client* deben", "client* atrasado*", "client* vencido*", "mandar recordatorio*", "enviar recordatorio*", "cobrar client*" },
                Ejecutar = RecordatoriosCobro },

            new() { Id = "respaldo_estado", Modulo = "Seguridad", Titulo = "Estado del respaldo", Descripcion = "Cuándo fue el último respaldo de la base de datos y si hay que preocuparse",
                Ejemplo = "cuándo fue el último respaldo", PermModulo = "Seguridad", PermSub = "Index",
                Frases = new[] { "respaldo*", "backup*", "copia* seguridad", "ultimo respaldo", "respaldar" },
                Ejecutar = EstadoRespaldoAccion },

            new() { Id = "sugerencia_compras", Modulo = "Compras", Titulo = "Qué debo comprar", Descripcion = "Productos a reponer según lo que realmente se vende, con proveedor y costo estimado",
                Ejemplo = "qué debo comprar", PermModulo = "Compras", PermSub = "Ordenes",
                Frases = new[] { "que debo comprar", "que compr*", "que pedir", "que debo pedir", "que falta comprar", "sugerencia* compra*", "reponer", "reposicion", "necesito comprar", "por comprar", "lista* compra*" },
                Ejecutar = SugerenciaCompras },

            new() { Id = "panel_dueno", Modulo = "General", Titulo = "Cómo va el negocio", Descripcion = "Ventas del mes contra el mes pasado, utilidad, por cobrar y dinero parado en inventario",
                Ejemplo = "cómo va el negocio", PermModulo = "Reportes", PermSub = "PanelDueno",
                Frases = new[] { "como va* negocio", "va* negocio", "panel dueno", "panel* dueno", "comparativo mes*", "mes contra mes" },
                Ejecutar = PanelDuenoAccion },

            new() { Id = "isv_mes", Modulo = "Reportes", Titulo = "ISV del mes", Descripcion = "Cuánto ISV debes pagar este mes (o el pasado): lo cobrado en ventas menos lo pagado en compras",
                Ejemplo = "cuánto isv debo este mes", PermModulo = "Reportes", PermSub = "ISV",
                Frases = new[] { "isv", "isv mes", "cuanto isv*", "impuesto* venta*", "declaracion* isv", "declaracion* impuesto*", "declarar isv", "impuesto* pagar" },
                Ejecutar = IsvMesAccion },

            new() { Id = "aprobar_vacaciones", Modulo = "RRHH", Titulo = "Aprobar vacaciones", Descripcion = "Aprueba una solicitud de vacaciones (descuenta los días del saldo). Pide contraseña",
                Ejemplo = "aprobar vacaciones de Juan", Tipo = TipoAccion.Operacion, RequierePassword = true,
                PermModulo = "RRHH", PermSub = "VacacionesAprobar", PermAccion = "editar",
                Frases = new[] { "aprob* vacacion*", "autoriz* vacacion*", "visto bueno vacacion*" },
                Preparar = PrepararAprobarVacaciones, Ejecutar = EjecutarAprobarVacaciones },

            new() { Id = "aprobar_permiso", Modulo = "RRHH", Titulo = "Aprobar permiso", Descripcion = "Aprueba un permiso o licencia de un empleado. Pide contraseña",
                Ejemplo = "aprobar permiso de María", Tipo = TipoAccion.Operacion, RequierePassword = true,
                PermModulo = "RRHH", PermSub = "PermisosAprobar", PermAccion = "editar",
                Frases = new[] { "aprob* permiso*", "autoriz* permiso*", "visto bueno permiso*" },
                Preparar = PrepararAprobarPermiso, Ejecutar = EjecutarAprobarPermiso },

            new() { Id = "aprobar_vale", Modulo = "RRHH", Titulo = "Aprobar vale", Descripcion = "Da la aprobación que le toca a un vale o adelanto (son dos aprobaciones de personas distintas). Pide contraseña",
                Ejemplo = "aprobar el vale de Pedro", Tipo = TipoAccion.Operacion, RequierePassword = true,
                PermModulo = "RRHH", PermSub = "ValesAprobar", PermAccion = "editar",
                Frases = new[] { "aprob* vale*", "autoriz* vale*", "aprob* adelanto*", "autoriz* adelanto*" },
                Preparar = PrepararAprobarVale, Ejecutar = EjecutarAprobarVale },
        };

        // ════════════════════════ COBRANZA ════════════════════════
        private static RespuestaAsistente RecordatoriosCobro(ContextoAccion c)
        {
            var hoy = c.Hoy.Date;
            var ventas = c.Db.Ventas.AsNoTracking()
                .Where(v => v.EsCredito && v.SaldoPendiente > 0 && v.Estado != "Anulada" && v.ClienteId != null).ToList();
            var ids = ventas.Select(v => v.ClienteId!.Value).Distinct().ToList();
            var clientes = c.Db.Clientes.AsNoTracking().Where(x => ids.Contains(x.Id)).ToDictionary(x => x.Id);

            var filas = new List<(string cli, string tel, decimal saldo, decimal venc, int atraso)>();
            foreach (var g in ventas.GroupBy(v => v.ClienteId!.Value))
            {
                if (!clientes.TryGetValue(g.Key, out var cli)) continue;
                decimal saldo = 0, venc = 0; int atraso = 0;
                foreach (var v in g)
                {
                    saldo += v.SaldoPendiente;
                    var f = (v.FechaVencimientoCredito ?? (cli.DiasCredito > 0 ? v.Fecha.AddDays(cli.DiasCredito) : v.Fecha)).Date;
                    var d = (hoy - f).Days;
                    if (d > 0) { venc += v.SaldoPendiente; atraso = Math.Max(atraso, d); }
                }
                if (venc > 0) filas.Add((cli.Nombre, cli.Telefono ?? "—", saldo, venc, atraso));
            }

            var orden = filas.OrderByDescending(f => f.atraso).ThenByDescending(f => f.venc).ToList();
            var tabla = orden.Take(Limite).Select(f => Fila(f.cli, f.tel, L(f.saldo), L(f.venc), f.atraso + " días")).ToList();
            var r = RespuestaAsistente.Tabla("Clientes con saldo vencido",
                orden.Count == 0 ? "Nadie tiene saldo vencido. ✅"
                    : $"{orden.Count} cliente(s) con saldo vencido por {L(orden.Sum(f => f.venc))}{Nota(orden.Count)}. Abre los recordatorios para mandarles WhatsApp con un clic.",
                new[] { "Cliente", "Teléfono", "Saldo", "Vencido", "Atraso" }, tabla,
                orden.Count > 0 ? Fila("TOTAL", "", L(orden.Sum(f => f.saldo)), L(orden.Sum(f => f.venc)), "") : null, new[] { 2, 3 });
            r.Enlaces = new List<EnlaceAsistente> { new() { Texto = "Enviar recordatorios por WhatsApp", Url = "/Finanzas/Cobros/Recordatorios" } };
            return r;
        }

        // ════════════════════════ RESPALDO ════════════════════════
        private static RespuestaAsistente EstadoRespaldoAccion(ContextoAccion c)
        {
            var e = RespaldoEstado.Leer();
            var aviso = RespaldoEstado.Avisar(DateTime.Now);
            string msg;
            if (e.Ultimo == null)
                msg = "⚠️ No encontré ningún respaldo de la base de datos. Programa el respaldo diario con tools\\programar-respaldo.ps1.";
            else
            {
                var horas = (DateTime.Now - e.Ultimo.Value).TotalHours;
                var hace = horas < 1 ? "hace menos de una hora" : horas < 48 ? $"hace {Math.Floor(horas)} hora(s)" : $"hace {Math.Floor(horas / 24)} día(s)";
                msg = $"{(aviso == null ? "✅" : "⚠️")} El último respaldo fue {hace} ({e.Ultimo.Value:dd/MM/yyyy HH:mm}, {e.UltimoBytes / 1024.0 / 1024.0:0.##} MB). Hay {e.Cantidad} respaldo(s) guardado(s).";
                if (aviso != null) msg += " Revisa que la tarea programada siga funcionando.";
                else msg += " Recuerda probar la restauración de vez en cuando (tools\\probar-restauracion.ps1).";
            }
            if (aviso != null && aviso.Texto.Contains("falló")) msg += "\n⚠️ " + aviso.Texto;
            var r = RespuestaAsistente.Texto(msg, "Respaldo de la base de datos");
            r.Enlaces = new List<EnlaceAsistente> { new() { Texto = "Seguridad", Url = "/Seguridad/Index" } };
            return r;
        }

        // ════════════════════════ COMPRAS ════════════════════════
        private static RespuestaAsistente SugerenciaCompras(ContextoAccion c)
        {
            var items = SugerenciaComprasHelper.Generar(c.Db);
            var filas = items.Take(30).Select(i => Fila(i.Nombre, i.Proveedor, i.Stock.ToString("N2", Inv),
                i.DiasRestantes == null ? "sin ventas" : i.DiasRestantes.Value.ToString("0.#", Inv) + " d",
                i.Sugerido.ToString("N0", Inv), L(i.CostoTotal))).ToList();
            var r = RespuestaAsistente.Tabla("Qué conviene comprar",
                items.Count == 0 ? "Con el ritmo de ventas actual no hace falta pedir nada. ✅"
                    : $"{items.Count} producto(s) por reponer; inversión estimada {L(items.Sum(i => i.CostoTotal))}" + (items.Count > 30 ? " (se muestran los primeros 30)." : "."),
                new[] { "Producto", "Proveedor", "Stock", "Alcanza", "Pedir", "Costo est." }, filas,
                items.Count > 0 ? Fila("TOTAL", "", "", "", "", L(items.Sum(i => i.CostoTotal))) : null, new[] { 2, 3, 4, 5 });
            r.Enlaces = new List<EnlaceAsistente> { new() { Texto = "Ver sugerencia completa", Url = "/Compras/Sugerencias" } };
            return r;
        }

        // ════════════════════════ ISV DEL MES ════════════════════════
        private static RespuestaAsistente IsvMesAccion(ContextoAccion c)
        {
            var raw = AsistenteMotor.Normalizar(c.P.GetValueOrDefault("raw", "") ?? "");
            var anterior = raw.Contains("pasad") || raw.Contains("anterior") || raw.Contains("ultimo");
            var ini = new DateTime(c.Hoy.Year, c.Hoy.Month, 1);
            if (anterior) ini = ini.AddMonths(-1);
            var finExcl = ini.AddMonths(1);

            var ventas = DeclaracionISVHelper.Ventas(c.Db, ini, finExcl);
            var compras = DeclaracionISVHelper.Compras(c.Db, ini, finExcl);
            var r = DeclaracionISVHelper.Resumir(ini, finExcl.AddDays(-1), ventas, compras);

            var nombre = ini.ToString("MMMM 'de' yyyy", new System.Globalization.CultureInfo("es-HN"));
            var parcial = !anterior ? " (el mes todavía no termina)" : "";
            var msg = $"📊 ISV de {nombre}{parcial}:\n" +
                      $"• ISV cobrado en ventas: L. {r.IsvDebito:N2}\n" +
                      $"• ISV pagado en compras: L. {r.IsvCredito:N2}\n" +
                      (r.Resultado > 0 ? $"➡️ A pagar: L. {r.IsvAPagar:N2}" : $"➡️ Saldo a favor: L. {r.SaldoFavorSiguiente:N2}") +
                      "\nNo incluye saldo a favor anterior ni retenciones; en la pantalla de declaración los puedes agregar.";
            var resp = RespuestaAsistente.Texto(msg, "ISV del mes");
            resp.Enlaces = new List<EnlaceAsistente> { new() { Texto = "Abrir la declaración de ISV", Url = $"/Reportes/DeclaracionISV?anio={ini.Year}&mes={ini.Month}" } };
            return resp;
        }

        // ════════════════════════ PANEL DEL DUEÑO ════════════════════════
        private static RespuestaAsistente PanelDuenoAccion(ContextoAccion c)
        {
            var hoy = c.Hoy.Date;
            var ini = new DateTime(hoy.Year, hoy.Month, 1);
            var fin = ini.AddMonths(1);
            var iniAnt = ini.AddMonths(-1);
            var corte = iniAnt.AddDays(Math.Min(hoy.Day, DateTime.DaysInMonth(iniAnt.Year, iniAnt.Month)));

            var mes = c.Db.Ventas.AsNoTracking().Where(v => v.Fecha >= ini && v.Fecha < fin && v.Estado == "Completada").Select(v => v.Total).ToList();
            var ant = c.Db.Ventas.AsNoTracking().Where(v => v.Fecha >= iniAnt && v.Fecha < corte && v.Estado == "Completada").Select(v => v.Total).ToList();
            var lineas = c.Db.DetalleVentas.AsNoTracking()
                .Join(c.Db.Ventas.AsNoTracking().Where(v => v.Fecha >= ini && v.Fecha < fin && v.Estado == "Completada"), d => d.VentaId, v => v.Id, (d, v) => new { d.Subtotal, d.CostoTotalLinea })
                .ToList();
            var ingresos = lineas.Sum(x => x.Subtotal);
            var utilidad = ingresos - lineas.Sum(x => x.CostoTotalLinea);

            var sMes = mes.Sum(); var sAnt = ant.Sum();
            var pct = sAnt > 0 ? Math.Round((sMes - sAnt) / sAnt * 100, 1) : 0;
            var cxc = c.Db.Ventas.AsNoTracking().Where(v => v.EsCredito && v.SaldoPendiente > 0 && v.Estado != "Anulada")
                .Select(v => new { v.SaldoPendiente, v.Fecha, v.FechaVencimientoCredito }).ToList();
            var vencido = cxc.Where(v => (v.FechaVencimientoCredito ?? v.Fecha).Date < hoy).Sum(v => v.SaldoPendiente);

            var lineasTxt = new List<string>
            {
                $"🛒 Ventas del mes: {L(sMes)} ({(pct >= 0 ? "↑" : "↓")} {Math.Abs(pct)}% vs el mismo avance del mes pasado, {L(sAnt)})",
                $"💵 Utilidad bruta: {L(utilidad)}" + (ingresos > 0 ? $" (margen {Math.Round(utilidad / ingresos * 100, 1)}%)" : ""),
                $"🧾 {mes.Count} ventas, ticket promedio {L(mes.Count > 0 ? Math.Round(sMes / mes.Count, 2) : 0)}",
                $"💰 Por cobrar: {L(cxc.Sum(x => x.SaldoPendiente))}" + (vencido > 0 ? $" (vencido {L(vencido)})" : "")
            };
            var r = RespuestaAsistente.Texto(string.Join("\n", lineasTxt), "Cómo va el negocio");
            r.Enlaces = new List<EnlaceAsistente>
            {
                new() { Texto = "Abrir panel del dueño", Url = "/Reportes/PanelDueno" },
                new() { Texto = "Qué debo comprar", Url = "/Compras/Sugerencias" }
            };
            return r;
        }

        // ════════════════════════ APROBACIONES RRHH ════════════════════════
        private static readonly HashSet<string> IgnorarAprobacion = new()
        {
            "aprobar","aprobarle","aprobada","aprobado","aprueba","apruebala","apruebalo","autorizar","autoriza","autorizo","visto","bueno",
            "vacaciones","vacacion","permiso","permisos","vale","vales","adelanto","adelantos","solicitud","solicitudes","empleado","empleada",
            "de","del","la","el","los","las","a","al","para","por","favor","porfa","quiero","que","se","mi","un","una","necesito","puedes","me","lo","le","su"
        };

        private static string Plano(string? s) => Regex.Replace(AsistenteMotor.Normalizar(s), "[^a-z0-9]", "");

        /// <summary>
        /// Ubica la solicitud pendiente a la que se refiere el mensaje (por número o por nombre del empleado).
        /// Devuelve la solicitud, o la respuesta con la que hay que contestar (nada pendiente, no encontrada, varias).
        /// </summary>
        private static (T? item, RespuestaAsistente? resp) SeleccionarAprobable<T>(
            ContextoAccion c, string verboEnvio, string plural, List<T> pend,
            Func<T, int> id, Func<T, string> numero, Func<T, int> empleadoId, Func<T, string> detalle,
            Dictionary<int, string> nombres) where T : class
        {
            if (c.P.TryGetValue("id", out var sid) && int.TryParse(sid, out var idFijo))
            {
                var fijo = pend.FirstOrDefault(x => id(x) == idFijo);
                if (fijo != null) return (fijo, null);
            }
            if (pend.Count == 0) return (null, RespuestaAsistente.Texto($"No hay {plural} pendientes de aprobar. ✅"));

            var raw = c.P.GetValueOrDefault("raw", "") ?? "";
            var claves = AsistenteMotor.Tokens(AsistenteMotor.Normalizar(raw))
                .Where(t => !IgnorarAprobacion.Contains(t) && (t.Length >= 3 || t.Any(char.IsDigit)))
                .ToList();

            var halladas = pend.Where(x =>
            {
                var num = Plano(numero(x));
                var nom = AsistenteMotor.Normalizar(nombres.GetValueOrDefault(empleadoId(x), ""));
                return claves.All(k => num.Contains(Plano(k)) || nom.Contains(k));
            }).ToList();

            if (halladas.Count == 1) return (halladas[0], null);

            var lista = halladas.Count > 0 ? halladas : pend;
            var intro = halladas.Count == 0 && claves.Count > 0
                ? $"No encontré {plural} pendientes que coincidan con “{string.Join(" ", claves)}”. Estas son las que están pendientes, ¿cuál apruebo?"
                : $"Hay {lista.Count} {plural} pendientes. ¿Cuál apruebo?";
            return (null, RespuestaAsistente.Elegir(intro, lista.Take(6).Select(x => new OpcionAsistente
            {
                Texto = $"{numero(x)} · {nombres.GetValueOrDefault(empleadoId(x), "—")} · {detalle(x)}",
                Envio = $"{verboEnvio} {numero(x)}"
            })));
        }

        private static Dictionary<int, string> NombresEmpleados(ContextoAccion c, IEnumerable<int> ids)
        {
            var lista = ids.Distinct().ToList();
            return c.Db.Empleados.AsNoTracking().Where(e => lista.Contains(e.Id)).ToList().ToDictionary(e => e.Id, e => Nombre(e));
        }

        // ── Vacaciones ──
        private static RespuestaAsistente PrepararAprobarVacaciones(ContextoAccion c)
        {
            var pend = c.Db.VacacionesEmpleado.AsNoTracking().Where(v => v.Estado == "Solicitado").OrderBy(v => v.FechaInicio).ToList();
            var nombres = NombresEmpleados(c, pend.Select(v => v.EmpleadoId));
            var (v, resp) = SeleccionarAprobable(c, "aprobar vacaciones", "solicitudes de vacaciones", pend,
                x => x.Id, x => x.Numero, x => x.EmpleadoId,
                x => $"{F(x.FechaInicio)} al {F(x.FechaFin)}", nombres);
            if (v == null) return resp!;

            c.P["id"] = v.Id.ToString();
            return new RespuestaAsistente
            {
                Tipo = "confirmar",
                Titulo = "Confirmar aprobación de vacaciones",
                Mensaje = "Al aprobar se descuentan los días del saldo del empleado. Confirma con tu contraseña.",
                RequierePassword = true,
                Resumen = new List<string>
                {
                    $"Solicitud: {v.Numero}", $"Empleado: {nombres.GetValueOrDefault(v.EmpleadoId, "—")}",
                    $"Del {F(v.FechaInicio)} al {F(v.FechaFin)}", $"Días a descontar: {v.DiasADescontar.ToString("0.##", Inv)}"
                }
            };
        }

        private static RespuestaAsistente EjecutarAprobarVacaciones(ContextoAccion c)
        {
            if (!int.TryParse(c.P.GetValueOrDefault("id"), out var id)) return RespuestaAsistente.Error("Falta la solicitud.");
            var num = c.Db.VacacionesEmpleado.AsNoTracking().Where(v => v.Id == id).Select(v => v.Numero).FirstOrDefault() ?? id.ToString();
            var (ok, error) = VacacionHelper.Aprobar(c.Db, id, c.Usuario.Id);
            if (!ok) return RespuestaAsistente.Error(error ?? "No se pudo aprobar la solicitud.");
            ActividadHelper.Registrar(c.Db, c.Usuario.Id, "Asistente: aprobar vacaciones",
                $"Aprobó la solicitud de vacaciones {num}. Autorizó: {c.Autorizo}", c.Ip);
            var r = RespuestaAsistente.Texto($"✅ Vacaciones {num} aprobadas.");
            r.Enlaces = new List<EnlaceAsistente> { new() { Texto = "Ver vacaciones", Url = "/RRHH/Vacaciones" } };
            return r;
        }

        // ── Permisos ──
        private static RespuestaAsistente PrepararAprobarPermiso(ContextoAccion c)
        {
            var pend = c.Db.PermisosEmpleado.AsNoTracking().Where(p => p.Estado == "Solicitado").OrderBy(p => p.FechaInicio).ToList();
            var nombres = NombresEmpleados(c, pend.Select(p => p.EmpleadoId));
            var (p, resp) = SeleccionarAprobable(c, "aprobar permiso", "solicitudes de permiso", pend,
                x => x.Id, x => x.Numero, x => x.EmpleadoId,
                x => $"{x.Tipo} {F(x.FechaInicio)}", nombres);
            if (p == null) return resp!;

            c.P["id"] = p.Id.ToString();
            return new RespuestaAsistente
            {
                Tipo = "confirmar",
                Titulo = "Confirmar aprobación de permiso",
                Mensaje = "Confirma con tu contraseña para aprobar el permiso.",
                RequierePassword = true,
                Resumen = new List<string>
                {
                    $"Permiso: {p.Numero}", $"Empleado: {nombres.GetValueOrDefault(p.EmpleadoId, "—")}", $"Tipo: {p.Tipo}" + (p.ConGoceSueldo ? " (con goce de sueldo)" : " (sin goce de sueldo)"),
                    $"Del {F(p.FechaInicio)} al {F(p.FechaFin)} · {p.DiasSolicitados.ToString("0.##", Inv)} día(s)", $"Motivo: {p.Motivo}"
                }
            };
        }

        private static RespuestaAsistente EjecutarAprobarPermiso(ContextoAccion c)
        {
            if (!int.TryParse(c.P.GetValueOrDefault("id"), out var id)) return RespuestaAsistente.Error("Falta la solicitud.");
            var p = c.Db.PermisosEmpleado.FirstOrDefault(x => x.Id == id);
            if (p == null) return RespuestaAsistente.Error("No encontré el permiso.");
            if (p.Estado != "Solicitado") return RespuestaAsistente.Error($"El permiso {p.Numero} ya fue procesado.");

            p.Estado = "Aprobado";
            p.UsuarioApruebaId = c.Usuario.Id;
            p.FechaAprobacion = DateTime.Now;
            c.Db.SaveChanges();

            ActividadHelper.Registrar(c.Db, c.Usuario.Id, "Asistente: aprobar permiso",
                $"Aprobó el permiso {p.Numero}. Autorizó: {c.Autorizo}", c.Ip);
            var r = RespuestaAsistente.Texto($"✅ Permiso {p.Numero} aprobado.");
            r.Enlaces = new List<EnlaceAsistente> { new() { Texto = "Ver permisos", Url = "/RRHH/Permisos" } };
            return r;
        }

        // ── Vales (doble aprobación) ──
        private static RespuestaAsistente PrepararAprobarVale(ContextoAccion c)
        {
            var pend = c.Db.ValesEmpleado.AsNoTracking().Where(v => v.Estado == "Solicitado" || v.Estado == "AprobadoGerente").OrderBy(v => v.FechaSolicitud).ToList();
            var nombres = NombresEmpleados(c, pend.Select(v => v.EmpleadoId));
            var (v, resp) = SeleccionarAprobable(c, "aprobar vale", "vales", pend,
                x => x.Id, x => x.Numero, x => x.EmpleadoId,
                x => $"{L(x.Monto)} ({(x.Estado == "Solicitado" ? "1ª aprobación" : "2ª aprobación")})", nombres);
            if (v == null) return resp!;

            var segunda = v.Estado == "AprobadoGerente";
            if (segunda && c.Usuario.Rol != "Admin" && v.AprobadoPorGerenteId == c.Usuario.Id)
                return RespuestaAsistente.Error($"El vale {v.Numero} ya tiene tu primera aprobación. La segunda debe darla otra persona.");

            c.P["id"] = v.Id.ToString();
            return new RespuestaAsistente
            {
                Tipo = "confirmar",
                Titulo = segunda ? "Confirmar segunda aprobación del vale" : "Confirmar primera aprobación del vale",
                Mensaje = segunda ? "Con esta aprobación el vale queda listo para entregarse." : "Falta una segunda aprobación de otra persona antes de entregarlo.",
                RequierePassword = true,
                Resumen = new List<string>
                {
                    $"Vale: {v.Numero}", $"Empleado: {nombres.GetValueOrDefault(v.EmpleadoId, "—")}", $"Monto: {L(v.Monto)} en {v.Cuotas} cuota(s)", $"Motivo: {v.Motivo}"
                }
            };
        }

        private static RespuestaAsistente EjecutarAprobarVale(ContextoAccion c)
        {
            if (!int.TryParse(c.P.GetValueOrDefault("id"), out var id)) return RespuestaAsistente.Error("Falta el vale.");
            var v = c.Db.ValesEmpleado.FirstOrDefault(x => x.Id == id);
            if (v == null) return RespuestaAsistente.Error("No encontré el vale.");

            string paso;
            if (v.Estado == "Solicitado")
            {
                v.Estado = "AprobadoGerente";
                v.AprobadoPorGerenteId = c.Usuario.Id;
                v.FechaAprobacionGerente = DateTime.Now;
                paso = "primera aprobación";
            }
            else if (v.Estado == "AprobadoGerente")
            {
                if (c.Usuario.Rol != "Admin" && v.AprobadoPorGerenteId == c.Usuario.Id)
                    return RespuestaAsistente.Error("La segunda aprobación debe hacerla otra persona distinta a quien dio la primera.");
                v.Estado = "AprobadoRRHH";
                v.AprobadoPorRRHHId = c.Usuario.Id;
                v.FechaAprobacionRRHH = DateTime.Now;
                paso = "segunda aprobación";
            }
            else return RespuestaAsistente.Error($"El vale {v.Numero} ya fue procesado.");

            c.Db.SaveChanges();
            ActividadHelper.Registrar(c.Db, c.Usuario.Id, "Asistente: aprobar vale",
                $"Dio la {paso} al vale {v.Numero} por {L(v.Monto)}. Autorizó: {c.Autorizo}", c.Ip);
            var r = RespuestaAsistente.Texto($"✅ Vale {v.Numero}: {paso} registrada" + (v.Estado == "AprobadoRRHH" ? ". Ya se puede entregar." : ". Falta la segunda aprobación de otra persona."));
            r.Enlaces = new List<EnlaceAsistente> { new() { Texto = "Ver vales", Url = "/RRHH/Vales" } };
            return r;
        }
    }
}
