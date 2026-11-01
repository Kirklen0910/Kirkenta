using Kirkenta.Helpers;
using Kirkenta.Helpers.Finanzas;
using Kirkenta.Models;
using Microsoft.EntityFrameworkCore;

namespace Kirkenta.Services.Asistente
{
    /// <summary>Acciones adicionales del asistente: logística, ventas, compras, RRHH y seguridad.</summary>
    public static partial class AsistenteCatalogo
    {
        private static IEnumerable<AccionDef> Extra() => new List<AccionDef>
        {
            // ───────── Logística ─────────
            new() { Id = "rutas_activas", Modulo = "Logística", Titulo = "Estado de rutas", Descripcion = "Rutas despachadas o en reparto ahora mismo, con su avance",
                Ejemplo = "estado de las rutas", PermModulo = "Logistica", PermSub = "Rutas",
                Frases = new[] { "estado ruta*", "ruta* activa*", "ruta* en curso", "ruta* en reparto", "ruta* abierta*", "entrega* calle", "envio* calle", "envio* camino", "entrega* camino", "entrega* curso", "como van entrega*", "como van envio*", "como van ruta*", "reparto* hoy" }, Ejecutar = RutasActivas },
            new() { Id = "envios_entregados", Modulo = "Logística", Titulo = "Envíos entregados", Descripcion = "Envíos entregados en el período",
                Ejemplo = "envíos entregados esta semana", UsaFechas = true, PermModulo = "Logistica", PermSub = "Envios",
                Frases = new[] { "envio* entregado*", "entrega* realizada*", "entrega* hecha*" }, Ejecutar = EnviosEntregados },
            new() { Id = "envios_fallidos", Modulo = "Logística", Titulo = "Envíos fallidos", Descripcion = "Envíos fallidos o reagendados con su motivo",
                Ejemplo = "envíos fallidos", PermModulo = "Logistica", PermSub = "Envios",
                Frases = new[] { "envio* fallido*", "entrega* fallida*", "envio* reagendado*", "envio* problema*", "no se entreg*", "entrega* no realizada*" }, Ejecutar = EnviosFallidos },
            new() { Id = "estado_envio", Modulo = "Logística", Titulo = "Estado de un envío", Descripcion = "Dónde va un envío (por número o por cliente)",
                Ejemplo = "estado del envío ENV-0001", PermModulo = "Logistica", PermSub = "Envios",
                Frases = new[] { "estado envio*", "rastrear*", "rastreo", "donde envio*", "seguimiento envio*" }, Ejecutar = EstadoEnvio },
            new() { Id = "repartidores", Modulo = "Logística", Titulo = "Repartidores", Descripcion = "Repartidores activos y su vehículo",
                Ejemplo = "repartidores activos", PermModulo = "Logistica", PermSub = "Repartidores",
                Frases = new[] { "repartidor* activo*", "repartidor*", "conductor*", "motorista*", "choferes" }, Ejecutar = Repartidores },

            // ───────── Ventas ─────────
            new() { Id = "mejores_clientes", Modulo = "Ventas", Titulo = "Mejores clientes", Descripcion = "Clientes que más compraron en el período",
                Ejemplo = "mejores clientes del mes", UsaFechas = true, PermModulo = "Ventas", PermSub = "Clientes",
                Frases = new[] { "mejores client*", "top client*", "client* que mas compra*", "client* frecuente*" }, Ejecutar = MejoresClientes },
            new() { Id = "cotizaciones_por_vencer", Modulo = "Ventas", Titulo = "Cotizaciones por vencer", Descripcion = "Cotizaciones abiertas que vencen en los próximos 7 días o ya vencieron",
                Ejemplo = "cotizaciones por vencer", PermModulo = "Ventas", PermSub = "Cotizaciones",
                Frases = new[] { "cotizacion* por vencer", "cotizacion* vencida*", "cotizacion* vence*" }, Ejecutar = CotizacionesPorVencer },
            new() { Id = "devoluciones_periodo", Modulo = "Ventas", Titulo = "Devoluciones", Descripcion = "Devoluciones de clientes del período",
                Ejemplo = "devoluciones de la semana", UsaFechas = true, PermModulo = "Ventas", PermSub = "Devoluciones",
                Frases = new[] { "devolucion* cliente*", "devolucion* periodo", "devolucion*" }, Ejecutar = DevolucionesPeriodo },

            new() { Id = "informe_metodos", Modulo = "Finanzas", Titulo = "Cobros por método de pago", Descripcion = "Cómo se mueve el negocio: efectivo, tarjeta, transferencia, crédito u otro método (todo o filtrado)",
                Ejemplo = "ventas por método de pago de hoy", UsaFechas = true, PermModulo = "Finanzas", PermSub = "Cierres",
                Frases = new[] { "metodo* pago", "forma* pago", "por metodo*", "venta* efectivo", "venta* tarjeta*", "venta* transferencia*", "venta* credito", "venta* deposito*",
                                 "cobro* efectivo", "cobro* tarjeta*", "cobro* transferencia*", "cobro* credito", "cobro*", "efectivo", "tarjeta*", "transferencia*", "deposito*", "cheque*" },
                Ejecutar = InformeMetodos },

            // ───────── Inventario / Compras ─────────
            new() { Id = "bajas_periodo", Modulo = "Inventario", Titulo = "Bajas de inventario", Descripcion = "Bajas (mermas, daños, vencidos) del período y su valor",
                Ejemplo = "bajas de inventario del mes", UsaFechas = true, PermModulo = "Inventario", PermSub = "Bajas",
                Frases = new[] { "baja* inventario", "baja*", "merma*", "producto* danado*", "producto* vencido*" }, Ejecutar = BajasPeriodo },
            new() { Id = "ordenes_compra_pendientes", Modulo = "Compras", Titulo = "Órdenes de compra abiertas", Descripcion = "Órdenes de compra por recibir o con recepción parcial",
                Ejemplo = "órdenes de compra pendientes", PermModulo = "Compras", PermSub = "Ordenes",
                Frases = new[] { "orden* compra* pendiente*", "orden* compra* abierta*", "compra* por recibir", "mercaderia* por llegar", "pedido* proveedor*" }, Ejecutar = OrdenesCompraPendientes },

            // ───────── RRHH ─────────
            new() { Id = "permisos_pendientes", Modulo = "RRHH", Titulo = "Permisos por aprobar", Descripcion = "Solicitudes de permiso de empleados sin resolver",
                Ejemplo = "permisos pendientes", PermModulo = "RRHH", PermSub = "Permisos",
                Frases = new[] { "permiso*", "permiso* pendiente*", "permiso* solicitado*", "permiso* por aprobar", "solicitud* permiso*" }, Ejecutar = PermisosPendientes },

            // ───────── Seguridad ─────────
            new() { Id = "usuarios_bloqueados", Modulo = "Seguridad", Titulo = "Usuarios bloqueados", Descripcion = "Usuarios bloqueados por intentos fallidos o desactivados",
                Ejemplo = "usuarios bloqueados", PermModulo = "Usuarios", PermSub = "Index",
                Frases = new[] { "usuario* bloqueado*", "usuario* inactivo*", "usuario* desactivado*", "cuenta* bloqueada*" }, Ejecutar = UsuariosBloqueados },

            // ───────── Panorama ─────────
            new() { Id = "pendientes", Modulo = "General", Titulo = "Qué tengo pendiente", Descripcion = "Todo lo que requiere atención, de todos los módulos a los que tienes acceso",
                Ejemplo = "qué tengo pendiente", 
                Frases = new[] { "pendiente* general*", "que tengo pendiente", "alerta*", "requiere* atencion", "por atender", "tareas pendientes" }, Ejecutar = PendientesGenerales },
        };

        // ════════════════════════ LOGÍSTICA ════════════════════════
        private static RespuestaAsistente RutasActivas(ContextoAccion c)
        {
            var estados = new List<string> { "Despachada", "EnReparto" };
            var rutas = c.Db.Rutas.AsNoTracking().Where(r => estados.Contains(r.Estado)).OrderBy(r => r.Fecha).ToList();
            var filas = rutas.Take(Limite).Select(r => Fila(r.Numero, F(r.Fecha), r.RepartidorNombre, r.Estado,
                $"{r.ParadasEntregadas}/{r.TotalParadas}", r.ParadasFallidas.ToString())).ToList();
            var msg = rutas.Count == 0
                ? "No hay rutas despachadas ni en reparto en este momento."
                : $"{rutas.Count} ruta(s) en curso: {rutas.Sum(r => r.ParadasEntregadas)} de {rutas.Sum(r => r.TotalParadas)} paradas entregadas, {rutas.Sum(r => r.ParadasFallidas)} fallida(s){Nota(rutas.Count)}.";
            return RespuestaAsistente.Tabla("Estado de rutas", msg,
                new[] { "Ruta", "Fecha", "Repartidor", "Estado", "Entregadas", "Fallidas" }, filas);
        }

        private static RespuestaAsistente EnviosEntregados(ContextoAccion c)
        {
            var d = c.Desde.Date; var h = c.Hasta.Date.AddDays(1);
            var env = c.Db.Envios.AsNoTracking()
                .Where(e => e.Estado == "Entregado" && e.FechaEntregaReal != null && e.FechaEntregaReal >= d && e.FechaEntregaReal < h)
                .OrderBy(e => e.FechaEntregaReal).ToList();
            var filas = env.Take(Limite).Select(e => Fila(e.Numero, e.ClienteNombre, e.Ciudad ?? e.ZonaNombre ?? "—",
                e.RepartidorNombre ?? "—", e.FechaEntregaReal!.Value.ToString("dd/MM HH:mm", Inv), e.NombreRecibio ?? "—")).ToList();
            return RespuestaAsistente.Tabla($"Envíos entregados {c.RangoTexto}",
                env.Count == 0 ? $"No hay envíos entregados {c.RangoTexto}." : $"{env.Count} envío(s) entregado(s) {c.RangoTexto}{Nota(env.Count)}.",
                new[] { "Envío", "Cliente", "Destino", "Repartidor", "Entregado", "Recibió" }, filas);
        }

        private static RespuestaAsistente EnviosFallidos(ContextoAccion c)
        {
            var estados = new List<string> { "Fallido", "Reagendado" };
            var env = c.Db.Envios.AsNoTracking().Where(e => estados.Contains(e.Estado)).OrderByDescending(e => e.FechaCreacion).ToList();
            var filas = env.Take(Limite).Select(e => Fila(e.Numero, e.ClienteNombre, e.Ciudad ?? e.ZonaNombre ?? "—", e.Estado, e.MotivoFallo ?? "—")).ToList();
            return RespuestaAsistente.Tabla("Envíos fallidos o reagendados",
                env.Count == 0 ? "No hay envíos fallidos ni reagendados." : $"{env.Count} envío(s) con problemas{Nota(env.Count)}.",
                new[] { "Envío", "Cliente", "Destino", "Estado", "Motivo" }, filas);
        }

        private static RespuestaAsistente EstadoEnvio(ContextoAccion c)
        {
            if (string.IsNullOrWhiteSpace(c.Texto))
                return RespuestaAsistente.Faltante("¿De qué envío? Escribe su número o el nombre del cliente.", "estado_envio", c.P, "texto");

            var t = c.Texto.Trim();
            var env = c.Db.Envios.AsNoTracking()
                .Where(e => e.Numero.Contains(t) || e.ClienteNombre.Contains(t))
                .OrderByDescending(e => e.FechaCreacion).Take(10).ToList();
            if (env.Count == 0) return RespuestaAsistente.Texto($"No encontré envíos que coincidan con “{t}”.");

            var rutas = env.Where(e => e.RutaId.HasValue).Select(e => e.RutaId!.Value).Distinct().ToList();
            var numRuta = c.Db.Rutas.AsNoTracking().Where(r => rutas.Contains(r.Id)).ToDictionary(r => r.Id, r => r.Numero);
            var filas = env.Select(e => Fila(e.Numero, e.ClienteNombre, e.Estado,
                e.RutaId.HasValue ? numRuta.GetValueOrDefault(e.RutaId.Value, "—") : "Sin ruta",
                e.RepartidorNombre ?? "—",
                e.FechaEntregaReal.HasValue ? "Entregado " + e.FechaEntregaReal.Value.ToString("dd/MM HH:mm", Inv)
                    : e.FechaEntregaEstimada.HasValue ? "Est. " + F(e.FechaEntregaEstimada.Value) : "—")).ToList();
            return RespuestaAsistente.Tabla("Estado del envío", $"{env.Count} envío(s) para “{t}”.",
                new[] { "Envío", "Cliente", "Estado", "Ruta", "Repartidor", "Fecha" }, filas);
        }

        private static RespuestaAsistente Repartidores(ContextoAccion c)
        {
            var reps = c.Db.Repartidores.AsNoTracking().Where(r => r.Activo).OrderBy(r => r.Nombre).ToList();
            var abiertas = c.Db.Rutas.AsNoTracking().Where(r => r.Estado == "Despachada" || r.Estado == "EnReparto" || r.Estado == "Borrador")
                .Where(r => r.RepartidorId != null).Select(r => r.RepartidorId!.Value).ToList();
            var filas = reps.Select(r => Fila(r.Codigo, r.Nombre, r.Telefono ?? "—", r.Vehiculo ?? "—", r.Placa ?? "—",
                abiertas.Count(x => x == r.Id) > 0 ? "En ruta" : "Disponible")).ToList();
            return RespuestaAsistente.Tabla("Repartidores", reps.Count == 0 ? "No hay repartidores activos." : $"{reps.Count} repartidor(es) activo(s).",
                new[] { "Código", "Nombre", "Teléfono", "Vehículo", "Placa", "Situación" }, filas);
        }

        // ════════════════════════ VENTAS ════════════════════════
        private static RespuestaAsistente MejoresClientes(ContextoAccion c)
        {
            var d = c.Desde.Date; var h = c.Hasta.Date.AddDays(1);
            var top = c.Db.Ventas.AsNoTracking()
                .Where(v => v.Fecha >= d && v.Fecha < h && v.Estado != "Anulada" && v.ClienteId != null)
                .GroupBy(v => v.ClienteId!.Value)
                .Select(g => new { Id = g.Key, Total = g.Sum(x => x.Total), Docs = g.Count() })
                .OrderByDescending(x => x.Total).Take(10).ToList();
            var ids = top.Select(x => x.Id).ToList();
            var nombres = c.Db.Clientes.AsNoTracking().Where(x => ids.Contains(x.Id)).ToDictionary(x => x.Id, x => x.Nombre);
            var filas = top.Select((x, i) => Fila((i + 1).ToString(), nombres.GetValueOrDefault(x.Id, "Cliente " + x.Id), x.Docs.ToString(), L(x.Total))).ToList();
            return RespuestaAsistente.Tabla($"Mejores clientes {c.RangoTexto}",
                top.Count == 0 ? $"No hay ventas a clientes {c.RangoTexto}." : $"Top {top.Count} clientes {c.RangoTexto}.",
                new[] { "#", "Cliente", "Compras", "Total" }, filas,
                top.Count > 0 ? Fila("TOTAL", "", top.Sum(x => x.Docs).ToString(), L(top.Sum(x => x.Total))) : null, new[] { 2, 3 });
        }

        private static RespuestaAsistente CotizacionesPorVencer(ContextoAccion c)
        {
            var limite = c.Hoy.Date.AddDays(7);
            var cot = c.Db.Cotizaciones.AsNoTracking()
                .Where(x => (x.Estado == "Borrador" || x.Estado == "Enviada") && x.FechaVencimiento != null && x.FechaVencimiento <= limite)
                .OrderBy(x => x.FechaVencimiento).ToList();
            var ids = cot.Select(x => x.ClienteId).Distinct().ToList();
            var cli = c.Db.Clientes.AsNoTracking().Where(x => ids.Contains(x.Id)).ToDictionary(x => x.Id, x => x.Nombre);
            var filas = cot.Take(Limite).Select(x => Fila(x.Numero, cli.GetValueOrDefault(x.ClienteId, "—"), F(x.FechaVencimiento!.Value),
                x.FechaVencimiento!.Value.Date < c.Hoy.Date ? "Vencida" : "Por vencer", L(x.Total))).ToList();
            return RespuestaAsistente.Tabla("Cotizaciones por vencer",
                cot.Count == 0 ? "No hay cotizaciones vencidas ni por vencer esta semana." : $"{cot.Count} cotización(es) por dar seguimiento{Nota(cot.Count)}.",
                new[] { "Cotización", "Cliente", "Vence", "Situación", "Total" }, filas,
                cot.Count > 0 ? Fila("TOTAL", "", "", "", L(cot.Sum(x => x.Total))) : null, new[] { 4 });
        }

        private static RespuestaAsistente DevolucionesPeriodo(ContextoAccion c)
        {
            var d = c.Desde.Date; var h = c.Hasta.Date.AddDays(1);
            var dev = c.Db.Devoluciones.AsNoTracking().Where(x => x.Fecha >= d && x.Fecha < h).OrderBy(x => x.Fecha).ToList();
            var ids = dev.Select(x => x.ClienteId).Distinct().ToList();
            var cli = c.Db.Clientes.AsNoTracking().Where(x => ids.Contains(x.Id)).ToDictionary(x => x.Id, x => x.Nombre);
            var filas = dev.Take(Limite).Select(x => Fila(x.Numero, F(x.Fecha), cli.GetValueOrDefault(x.ClienteId, "—"), x.Motivo ?? "—", x.Estado, L(x.Total))).ToList();
            return RespuestaAsistente.Tabla($"Devoluciones {c.RangoTexto}",
                dev.Count == 0 ? $"No hay devoluciones {c.RangoTexto}." : $"{dev.Count} devolución(es) {c.RangoTexto} por {L(dev.Sum(x => x.Total))}{Nota(dev.Count)}.",
                new[] { "Devolución", "Fecha", "Cliente", "Motivo", "Estado", "Total" }, filas,
                dev.Count > 0 ? Fila("TOTAL", "", "", "", "", L(dev.Sum(x => x.Total))) : null, new[] { 5 });
        }

        // ════════════════════ MÉTODOS DE PAGO ════════════════════
        /// <summary>Detecta en el mensaje un método de pago (por nombre o tipo configurado) o "crédito".</summary>
        private static string? DetectarMetodo(ContextoAccion c)
        {
            var raw = InformeMetodosPagoHelper.Norm(c.P.GetValueOrDefault("raw"));
            if (string.IsNullOrEmpty(raw)) return null;
            var palabras = System.Text.RegularExpressions.Regex.Matches(raw, @"[a-z0-9]+").Select(m => m.Value).ToList();

            foreach (var m in c.Db.MetodosPago.AsNoTracking().Where(x => x.Activo).ToList())
            {
                foreach (var cand in new[] { InformeMetodosPagoHelper.Norm(m.Nombre), InformeMetodosPagoHelper.Norm(m.Tipo) })
                {
                    if (cand.Length < 3 || cand == "credito") continue;
                    if (raw.Contains(cand)) return cand;
                }
            }
            // tolerancia a errores de escritura y sinónimos comunes
            foreach (var w in palabras)
            {
                if (w.StartsWith("efect") || w.StartsWith("cash") || w == "billete" || w == "billetes") return "efectivo";
                if (w.StartsWith("tarjet") || w == "pos" || w == "visa" || w == "mastercard") return "tarjeta";
                if (w.StartsWith("transf") || w.StartsWith("deposit") || w == "banco") return "transferencia";
            }
            if (palabras.Any(w => w.StartsWith("credit") || w == "fiado" || w == "fiados")) return "credito";
            return null;
        }

        private static RespuestaAsistente InformeMetodos(ContextoAccion c)
        {
            var d = c.Desde.Date; var h = c.Hasta.Date.AddDays(1);
            var filtro = DetectarMetodo(c);
            var inf = InformeMetodosPagoHelper.Generar(c.Db, d, h, filtro);

            RespuestaAsistente r;
            if (inf.Filtro == "todos")
            {
                var filas = inf.Metodos.Where(m => m.Cantidad > 0).Select(m => Fila(m.Nombre, m.Cantidad.ToString(), L(m.Total), m.Porcentaje.ToString("0.#", Inv) + "%")).ToList();
                filas.Add(Fila("Ventas a crédito (por cobrar)", inf.CantidadCredito.ToString(), L(inf.VentasCredito), ""));
                var msg = inf.TotalTransacciones == 0 && inf.CantidadCredito == 0
                    ? $"No hay cobros ni ventas {c.RangoTexto}."
                    : $"Cobrado {c.RangoTexto}: {L(inf.TotalCobrado)} en {inf.TotalTransacciones} cobro(s). Ventas: contado {L(inf.VentasContado)}, crédito {L(inf.VentasCredito)}. Salidas: {L(inf.TotalEgresos)}.";
                r = RespuestaAsistente.Tabla($"Cobros por método {c.RangoTexto}", msg,
                    new[] { "Método", "Cobros", "Total", "%" }, filas,
                    inf.TotalTransacciones > 0 ? Fila("TOTAL COBRADO", inf.TotalTransacciones.ToString(), L(inf.TotalCobrado), "100%") : null, new[] { 1, 2, 3 });
            }
            else
            {
                var filas = inf.Detalle.Take(Limite).Select(x => Fila(x.Documento, x.Fecha.ToString("dd/MM HH:mm", Inv), x.Cliente, x.Metodo, L(x.Monto))).ToList();
                var msg = inf.Detalle.Count == 0
                    ? $"No hay movimientos de “{inf.FiltroEtiqueta}” {c.RangoTexto}."
                    : $"{inf.Detalle.Count} movimiento(s) de “{inf.FiltroEtiqueta}” {c.RangoTexto} por {L(inf.TotalDetalle)}{Nota(inf.Detalle.Count)}.";
                r = RespuestaAsistente.Tabla($"{inf.FiltroEtiqueta} {c.RangoTexto}", msg,
                    new[] { "Documento", "Fecha", "Cliente", "Método", "Monto" }, filas,
                    inf.Detalle.Count > 0 ? Fila("TOTAL", "", "", inf.Detalle.Count + " mov.", L(inf.TotalDetalle)) : null, new[] { 4 });
            }

            var url = $"/Finanzas/Cierres/Informe?desde={d:yyyy-MM-dd}&hasta={c.Hasta.Date:yyyy-MM-dd}" + (inf.Filtro == "todos" ? "" : "&metodo=" + Uri.EscapeDataString(inf.Filtro));
            r.Enlaces = new List<EnlaceAsistente> { new() { Texto = "Abrir informe completo", Url = url } };
            return r;
        }

        // ════════════════════ INVENTARIO / COMPRAS ════════════════════
        private static RespuestaAsistente BajasPeriodo(ContextoAccion c)
        {
            var d = c.Desde.Date; var h = c.Hasta.Date.AddDays(1);
            var bajas = c.Db.BajasInventario.AsNoTracking().Where(b => b.Fecha >= d && b.Fecha < h && !b.Revertida).OrderBy(b => b.Fecha).ToList();
            var ids = bajas.Select(b => b.ProductoId).Distinct().ToList();
            var prod = c.Db.Productos.AsNoTracking().Where(p => ids.Contains(p.Id)).ToDictionary(p => p.Id, p => p.Nombre);
            var filas = bajas.Take(Limite).Select(b => Fila(b.Numero, F(b.Fecha), prod.GetValueOrDefault(b.ProductoId, "—"),
                b.Cantidad.ToString("N2", Inv), b.TipoBaja, b.Estado, L(b.ValorTotal))).ToList();
            return RespuestaAsistente.Tabla($"Bajas de inventario {c.RangoTexto}",
                bajas.Count == 0 ? $"No hay bajas {c.RangoTexto}." : $"{bajas.Count} baja(s) {c.RangoTexto} por {L(bajas.Sum(b => b.ValorTotal))}{Nota(bajas.Count)}.",
                new[] { "Baja", "Fecha", "Producto", "Cantidad", "Tipo", "Estado", "Valor" }, filas,
                bajas.Count > 0 ? Fila("TOTAL", "", "", "", "", "", L(bajas.Sum(b => b.ValorTotal))) : null, new[] { 6 });
        }

        private static RespuestaAsistente OrdenesCompraPendientes(ContextoAccion c)
        {
            var estados = new List<string> { "Enviada", "RecibidaParcial" };
            var ord = c.Db.OrdenesCompra.AsNoTracking().Where(o => estados.Contains(o.Estado)).OrderBy(o => o.FechaEntregaEstimada ?? o.Fecha).ToList();
            var ids = ord.Select(o => o.ProveedorId).Distinct().ToList();
            var prov = c.Db.Proveedores.AsNoTracking().Where(p => ids.Contains(p.Id)).ToDictionary(p => p.Id, p => p.Nombre);
            var filas = ord.Take(Limite).Select(o => Fila(o.Numero, prov.GetValueOrDefault(o.ProveedorId, "—"), F(o.Fecha),
                o.FechaEntregaEstimada.HasValue ? F(o.FechaEntregaEstimada.Value) : "—", o.Estado, L(o.Total))).ToList();
            return RespuestaAsistente.Tabla("Órdenes de compra por recibir",
                ord.Count == 0 ? "No hay órdenes de compra pendientes de recibir." : $"{ord.Count} orden(es) por recibir por {L(ord.Sum(o => o.Total))}{Nota(ord.Count)}.",
                new[] { "Orden", "Proveedor", "Fecha", "Entrega estimada", "Estado", "Total" }, filas,
                ord.Count > 0 ? Fila("TOTAL", "", "", "", "", L(ord.Sum(o => o.Total))) : null, new[] { 5 });
        }

        // ════════════════════════ RRHH / SEGURIDAD ════════════════════════
        private static RespuestaAsistente PermisosPendientes(ContextoAccion c)
        {
            var per = c.Db.PermisosEmpleado.AsNoTracking().Where(p => p.Estado == "Solicitado").OrderBy(p => p.FechaInicio).ToList();
            var ids = per.Select(p => p.EmpleadoId).Distinct().ToList();
            var emp = c.Db.Empleados.AsNoTracking().Where(e => ids.Contains(e.Id)).ToList().ToDictionary(e => e.Id, e => Nombre(e));
            var filas = per.Take(Limite).Select(p => Fila(p.Numero, emp.GetValueOrDefault(p.EmpleadoId, "—"), p.Tipo,
                F(p.FechaInicio), F(p.FechaFin), p.DiasSolicitados.ToString("0.##", Inv))).ToList();
            return RespuestaAsistente.Tabla("Permisos por aprobar",
                per.Count == 0 ? "No hay permisos pendientes de aprobar." : $"{per.Count} solicitud(es) de permiso por resolver{Nota(per.Count)}.",
                new[] { "Permiso", "Empleado", "Tipo", "Desde", "Hasta", "Días" }, filas);
        }

        private static RespuestaAsistente UsuariosBloqueados(ContextoAccion c)
        {
            var ahora = DateTime.Now;
            var us = c.Db.Usuarios.AsNoTracking()
                .Where(u => !u.Activo || (u.BloqueadoHasta != null && u.BloqueadoHasta > ahora))
                .OrderBy(u => u.Username).ToList();
            var filas = us.Select(u => Fila(u.Username, u.NombreCompleto ?? "—", u.Rol,
                !u.Activo ? "Desactivado" : "Bloqueado hasta " + u.BloqueadoHasta!.Value.ToString("dd/MM HH:mm", Inv),
                u.IntentosFallidos.ToString())).ToList();
            return RespuestaAsistente.Tabla("Usuarios bloqueados o inactivos",
                us.Count == 0 ? "No hay usuarios bloqueados ni desactivados." : $"{us.Count} usuario(s).",
                new[] { "Usuario", "Nombre", "Rol", "Situación", "Intentos fallidos" }, filas);
        }

        // ════════════════════════ PANORAMA ════════════════════════
        private static RespuestaAsistente PendientesGenerales(ContextoAccion c)
        {
            var l = new List<string>();
            var hoy = c.Hoy.Date;

            if (Acceso(c, "Ventas", "Facturas"))
            {
                var venc = c.Db.Facturas.AsNoTracking().Count(f => f.Estado != "Anulada" && f.Saldo > 0 && f.FechaVencimiento != null && f.FechaVencimiento < hoy);
                if (venc > 0) l.Add($"💰 {venc} factura(s) vencidas con saldo por cobrar");
            }
            if (Acceso(c, "Ventas", "Cotizaciones"))
            {
                var lim = hoy.AddDays(7);
                var n = c.Db.Cotizaciones.AsNoTracking().Count(x => (x.Estado == "Borrador" || x.Estado == "Enviada") && x.FechaVencimiento != null && x.FechaVencimiento <= lim);
                if (n > 0) l.Add($"📝 {n} cotización(es) vencidas o por vencer esta semana");
            }
            if (Acceso(c, "Logistica", "Envios"))
            {
                var pend = c.Db.Envios.AsNoTracking().Count(e => e.Estado == "Pendiente" || e.Estado == "EnRuta" || e.Estado == "Reagendado");
                var fall = c.Db.Envios.AsNoTracking().Count(e => e.Estado == "Fallido");
                if (pend > 0) l.Add($"🚚 {pend} envío(s) por entregar");
                if (fall > 0) l.Add($"⚠️ {fall} envío(s) fallidos");
            }
            if (Acceso(c, "Logistica", "Rutas"))
            {
                var r = c.Db.Rutas.AsNoTracking().Count(x => x.Estado == "Despachada" || x.Estado == "EnReparto");
                if (r > 0) l.Add($"🗺️ {r} ruta(s) en curso");
            }
            if (Acceso(c, "Compras", "Ordenes"))
            {
                var o = c.Db.OrdenesCompra.AsNoTracking().Count(x => x.Estado == "Enviada" || x.Estado == "RecibidaParcial");
                if (o > 0) l.Add($"📦 {o} orden(es) de compra por recibir");
            }
            if (Acceso(c, "RRHH", "Permisos"))
            {
                var p = c.Db.PermisosEmpleado.AsNoTracking().Count(x => x.Estado == "Solicitado");
                if (p > 0) l.Add($"🙋 {p} permiso(s) por aprobar");
            }
            if (Acceso(c, "Inventario", "Bajas"))
            {
                var b = c.Db.BajasInventario.AsNoTracking().Count(x => x.Estado == "Pendiente");
                if (b > 0) l.Add($"📉 {b} baja(s) de inventario por aprobar");
            }

            if (Acceso(c, "RRHH", "Vacaciones"))
            {
                var vac = c.Db.VacacionesEmpleado.AsNoTracking().Count(x => x.Estado == "Solicitado");
                if (vac > 0) l.Add($"🏖️ {vac} solicitud(es) de vacaciones por aprobar");
            }
            if (Acceso(c, "RRHH", "Vales"))
            {
                var va = c.Db.ValesEmpleado.AsNoTracking().Count(x => x.Estado == "Solicitado" || x.Estado == "AprobadoGerente");
                if (va > 0) l.Add($"💵 {va} vale(s) de empleados por aprobar");
            }
            if (Acceso(c, "Seguridad", "Index"))
            {
                var aviso = RespaldoEstado.Avisar(DateTime.Now);
                if (aviso != null) l.Add("💾 " + aviso.Texto);
            }

            return l.Count == 0
                ? RespuestaAsistente.Texto("Todo en orden: no encontré pendientes en los módulos a los que tienes acceso. 🎉", "Pendientes")
                : RespuestaAsistente.Texto(string.Join("\n", l), "Qué requiere tu atención");
        }
    }
}
