using Kirkenta.Data;
using Microsoft.EntityFrameworkCore;

namespace Kirkenta.Helpers.Finanzas
{
    public class HallazgoIntegridad
    {
        public string Documento { get; set; } = "";
        public string Detalle { get; set; } = "";
        public string? Pagina { get; set; }
        public int? Id { get; set; }
        public string? Enlace { get; set; }
    }

    public class RevisionIntegridad
    {
        public string Area { get; set; } = "";
        public string Titulo { get; set; } = "";
        public string Explicacion { get; set; } = "";
        /// <summary>Cuántos registros se revisaron (0 = nada que revisar en el período).</summary>
        public int Revisados { get; set; }
        public int Total { get; set; }
        /// <summary>No depende del período (saldos actuales, stock, etc.).</summary>
        public bool Global { get; set; }
        public List<HallazgoIntegridad> Ejemplos { get; set; } = new();
        public bool Ok => Total == 0;
    }

    /// <summary>
    /// Cruza ventas, cobros, finanzas, compras, inventario y contabilidad para detectar descuadres.
    /// Solo lee: no modifica nada. Cada paso se ejecuta por separado para poder mostrar el avance.
    /// </summary>
    public static class IntegridadHelper
    {
        private const decimal Tol = 0.05m;
        private const int MaxEjemplos = 50;

        public static readonly string[] Modulos = { "Ventas", "Cobros y créditos", "Finanzas", "Compras", "Nómina", "Inventario", "Contabilidad" };

        private record Paso(string Area, string Titulo, Func<ApplicationDbContext, DateTime, DateTime, RevisionIntegridad> Ejecuta);

        private static readonly Paso[] Pasos =
        {
            new("Ventas", "Ventas de contado con ingreso en finanzas", VentasSinMovimiento),
            new("Ventas", "Ventas vigentes con ingreso válido", VentasMovimientoInvalido),
            new("Ventas", "Ventas anuladas sin movimientos activos", VentasAnuladasConMovimientos),
            new("Ventas", "Cobrado igual al total de la venta", VentasCobradoVsTotal),
            new("Ventas", "Factura igual a su venta", FacturaVsVenta),
            new("Ventas", "Costo de ventas registrado", VentasCosto),
            new("Cobros y créditos", "Saldos de facturas", FacturasSaldoInvalido),
            new("Cobros y créditos", "Saldos de ventas a crédito", CreditoSaldo),
            new("Cobros y créditos", "Devoluciones: reparto cuadra con el total", DevolucionesReparto),
            new("Cobros y créditos", "Devoluciones: saldo a favor y reembolso registrados", DevolucionesLedger),
            new("Cobros y créditos", "Saldo a favor de clientes", SaldoFavorNegativo),
            new("Finanzas", "Saldo de cada cuenta contra sus movimientos", SaldosCuentas),
            new("Finanzas", "Ingresos y egresos válidos", MovimientosValidos),
            new("Compras", "Compras recibidas con entrada al kardex", ComprasSinKardex),
            new("Compras", "Saldos por pagar de órdenes", ComprasSaldoInvalido),
            new("Compras", "Pagos a proveedores con movimiento", PagosProveedorMovimiento),
            new("Nómina", "Pagos de nómina con egreso que cuadra", NominaPagosMovimiento),
            new("Nómina", "Neto = bruto − deducciones", NominaDetalleAritmetica),
            new("Inventario", "Stock igual al kardex", StockVsKardex),
            new("Contabilidad", "Asientos con debe = haber", AsientosDescuadrados),
        };

        public static int TotalPasos => Pasos.Length;
        public static (string Area, string Titulo) Info(int i) => (Pasos[i].Area, Pasos[i].Titulo);

        public static RevisionIntegridad EjecutarPaso(ApplicationDbContext db, int i, DateTime desde, DateTime hasta)
        {
            var p = Pasos[i];
            try { return p.Ejecuta(db, desde, hasta); }
            catch (Exception ex)
            {
                var msg = ex.InnerException?.Message ?? ex.Message;
                return new RevisionIntegridad { Area = p.Area, Titulo = p.Titulo, Explicacion = "No se pudo completar esta revisión: " + msg, Total = 1,
                    Ejemplos = { new HallazgoIntegridad { Documento = "Error de revisión", Detalle = msg } } };
            }
        }

        private static RevisionIntegridad Nueva(string area, string titulo, string expl, int revisados, List<HallazgoIntegridad> lista, bool global = false)
            => new() { Area = area, Titulo = titulo, Explicacion = expl, Revisados = revisados, Total = lista.Count, Global = global, Ejemplos = lista.Take(MaxEjemplos).ToList() };

        // ---------- VENTAS ----------
        private static RevisionIntegridad VentasSinMovimiento(ApplicationDbContext db, DateTime d, DateTime h)
        {
            var datos = db.Ventas.AsNoTracking()
                .Where(v => v.Fecha >= d && v.Fecha < h && v.Estado != "Anulada" && v.LoteMigracion == null && !v.EsCredito && v.Total > 0
                            && v.Total - v.SaldoFavorAplicado > 0.005m)
                .OrderByDescending(v => v.Fecha).Select(v => new { v.Id, v.Numero, v.Total, v.Fecha, v.MovimientoRegistrado }).ToList();
            var ids = datos.Select(x => x.Id).ToList();
            var conMov = db.MovimientosFinancieros.AsNoTracking()
                .Where(m => m.Origen == "Venta" && m.OrigenId != null && ids.Contains(m.OrigenId.Value) && m.Estado == "Activo")
                .Select(m => m.OrigenId!.Value).Distinct().ToList().ToHashSet();
            var lista = datos.Where(x => !x.MovimientoRegistrado && !conMov.Contains(x.Id)).Select(x => new HallazgoIntegridad
            {
                Documento = x.Numero, Id = x.Id, Pagina = "/Ventas/Details",
                Detalle = $"{x.Fecha:dd/MM/yyyy} · L. {x.Total:N2} sin ingreso registrado en finanzas"
            }).ToList();
            return Nueva("Ventas", "Ventas de contado con ingreso en finanzas", "Toda venta pagada debe haber generado su ingreso en una cuenta.", datos.Count, lista);
        }

        private static RevisionIntegridad VentasMovimientoInvalido(ApplicationDbContext db, DateTime d, DateTime h)
        {
            var datos = db.Ventas.AsNoTracking()
                .Where(v => v.Fecha >= d && v.Fecha < h && v.Estado != "Anulada" && v.MovimientoIngresoId != null)
                .Select(v => new { v.Id, v.Numero, Mov = v.MovimientoIngresoId!.Value }).ToList();
            var movIds = datos.Select(x => x.Mov).ToList();
            var estados = db.MovimientosFinancieros.AsNoTracking().Where(m => movIds.Contains(m.Id))
                .Select(m => new { m.Id, m.Estado }).ToList().ToDictionary(m => m.Id, m => m.Estado);
            var lista = datos.Where(x => !estados.TryGetValue(x.Mov, out var e) || e != "Activo").Select(x => new HallazgoIntegridad
            {
                Documento = x.Numero, Id = x.Id, Pagina = "/Ventas/Details",
                Detalle = estados.ContainsKey(x.Mov) ? "Su movimiento de ingreso está anulado" : "Su movimiento de ingreso ya no existe"
            }).ToList();
            return Nueva("Ventas", "Ventas vigentes con ingreso válido", "La venta vigente debe tener su dinero registrado y activo.", datos.Count, lista);
        }

        private static RevisionIntegridad VentasAnuladasConMovimientos(ApplicationDbContext db, DateTime d, DateTime h)
        {
            var anuladas = db.Ventas.AsNoTracking().Where(v => v.Fecha >= d && v.Fecha < h && v.Estado == "Anulada")
                .Select(v => new { v.Id, v.Numero }).ToList();
            var ids = anuladas.Select(a => a.Id).ToList();
            var activos = db.MovimientosFinancieros.AsNoTracking()
                .Where(m => m.Origen == "Venta" && m.OrigenId != null && ids.Contains(m.OrigenId.Value) && m.Estado == "Activo")
                .GroupBy(m => m.OrigenId!.Value).Select(g => new { Id = g.Key, N = g.Count() }).ToList();
            var lista = activos.Join(anuladas, a => a.Id, v => v.Id, (a, v) => new HallazgoIntegridad
            {
                Documento = v.Numero, Id = v.Id, Pagina = "/Ventas/Details", Detalle = $"Anulada pero con {a.N} movimiento(s) financiero(s) activo(s)"
            }).ToList();
            return Nueva("Ventas", "Ventas anuladas sin movimientos activos", "Al anular una venta todos sus movimientos deben anularse.", anuladas.Count, lista);
        }

        private static RevisionIntegridad VentasCobradoVsTotal(ApplicationDbContext db, DateTime d, DateTime h)
        {
            var ventas = db.Ventas.AsNoTracking()
                .Where(v => v.Fecha >= d && v.Fecha < h && v.Estado != "Anulada" && v.LoteMigracion == null && !v.EsCredito && v.Total > 0)
                .Select(v => new { v.Id, v.Numero, v.Total, v.SaldoFavorAplicado }).ToList();
            var ids = ventas.Select(v => v.Id).ToList();
            var pagos = db.Pagos.AsNoTracking().Where(p => p.VentaId != null && ids.Contains(p.VentaId.Value))
                .GroupBy(p => p.VentaId!.Value).Select(g => new { Id = g.Key, S = g.Sum(x => x.Monto) }).ToList()
                .ToDictionary(x => x.Id, x => x.S);
            // El pago con saldo a favor ya es un Pago más; en ventas antiguas podía no estarlo, por eso se acepta cualquiera de las dos sumas
            var lista = ventas.Where(v => pagos.ContainsKey(v.Id)
                    && Math.Abs(pagos[v.Id] - v.Total) > Tol && Math.Abs(pagos[v.Id] + v.SaldoFavorAplicado - v.Total) > Tol)
                .Select(v => new HallazgoIntegridad
                {
                    Documento = v.Numero, Id = v.Id, Pagina = "/Ventas/Details",
                    Detalle = $"Total L. {v.Total:N2}; pagos registrados L. {pagos[v.Id]:N2}; saldo a favor L. {v.SaldoFavorAplicado:N2}"
                }).ToList();
            return Nueva("Ventas", "Cobrado igual al total de la venta", "Los pagos (incluido el saldo a favor) deben sumar el total.", ventas.Count, lista);
        }

        private static RevisionIntegridad FacturaVsVenta(ApplicationDbContext db, DateTime d, DateTime h)
        {
            var datos = (from f in db.Facturas.AsNoTracking()
                         join v in db.Ventas.AsNoTracking() on f.VentaId equals v.Id
                         where f.Fecha >= d && f.Fecha < h && f.Estado != "Anulada" && v.Estado != "Anulada"
                         select new { f.Id, f.Numero, FT = f.Total, VT = v.Total }).ToList();
            var lista = datos.Where(x => Math.Abs(x.FT - x.VT) > Tol).Select(x => new HallazgoIntegridad
            {
                Documento = x.Numero, Id = x.Id, Pagina = "/Facturas/Details", Detalle = $"Factura L. {x.FT:N2} vs venta L. {x.VT:N2}"
            }).ToList();
            return Nueva("Ventas", "Factura igual a su venta", "La factura debe reflejar exactamente lo vendido (incluye el descuento).", datos.Count, lista);
        }

        private static RevisionIntegridad VentasCosto(ApplicationDbContext db, DateTime dd, DateTime h)
        {
            var datos = db.Ventas.AsNoTracking()
                .Where(v => v.Fecha >= dd && v.Fecha < h && v.Estado != "Anulada" && v.LoteMigracion == null && v.Total > 0)
                .Select(v => new { v.Id, v.Numero, v.CostoTotalVendido, v.CostoRegistrado }).ToList();
            var ids = datos.Select(x => x.Id).ToList();
            var detalle = db.DetalleVentas.AsNoTracking().Where(x => ids.Contains(x.VentaId))
                .GroupBy(x => x.VentaId).Select(g => new { Id = g.Key, C = g.Sum(x => x.CostoTotalLinea) }).ToList().ToDictionary(x => x.Id, x => x.C);
            var lista = datos.Where(x => detalle.GetValueOrDefault(x.Id) > 0.005m && (!x.CostoRegistrado || Math.Abs(x.CostoTotalVendido - detalle[x.Id]) > Tol))
                .Select(x => new HallazgoIntegridad
                {
                    Documento = x.Numero, Id = x.Id, Pagina = "/Ventas/Details",
                    Detalle = x.CostoRegistrado ? $"Costo de la venta L. {x.CostoTotalVendido:N2} ≠ suma de líneas L. {detalle[x.Id]:N2}" : "El costo de ventas no se registró"
                }).ToList();
            return Nueva("Ventas", "Costo de ventas registrado", "Cada venta con productos con costo debe registrar su costo de ventas.", datos.Count, lista);
        }

        // ---------- COBROS Y CRÉDITOS ----------
        private static RevisionIntegridad FacturasSaldoInvalido(ApplicationDbContext db, DateTime d, DateTime h)
        {
            var q = db.Facturas.AsNoTracking().Where(f => f.Fecha >= d && f.Fecha < h && f.Estado != "Anulada");
            var lista = q.Where(f => f.Saldo < -0.005m || f.Saldo > f.Total + 0.005m)
                .Select(f => new HallazgoIntegridad { Documento = f.Numero, Id = f.Id, Pagina = "/Facturas/Details", Detalle = "Saldo fuera de rango (negativo o mayor al total)" }).ToList();
            lista.AddRange(q.Where(f => f.Estado == "Pagada" && f.Saldo > 0.005m)
                .Select(f => new HallazgoIntegridad { Documento = f.Numero, Id = f.Id, Pagina = "/Facturas/Details", Detalle = "Marcada como pagada pero con saldo pendiente" }).ToList());
            return Nueva("Cobros y créditos", "Saldos de facturas", "El saldo debe estar entre 0 y el total, y una factura pagada no debe tener saldo.", q.Count(), lista);
        }

        private static RevisionIntegridad CreditoSaldo(ApplicationDbContext db, DateTime d, DateTime h)
        {
            var q = db.Ventas.AsNoTracking().Where(v => v.Fecha >= d && v.Fecha < h && v.Estado != "Anulada" && v.EsCredito);
            var lista = q.Where(v => v.SaldoPendiente < -0.005m || v.SaldoPendiente > v.Total + 0.005m)
                .Select(v => new HallazgoIntegridad { Documento = v.Numero, Id = v.Id, Pagina = "/Ventas/Details", Detalle = "Saldo pendiente fuera de rango" }).ToList();
            return Nueva("Cobros y créditos", "Saldos de ventas a crédito", "El saldo pendiente debe estar entre 0 y el total.", q.Count(), lista);
        }

        private static RevisionIntegridad DevolucionesReparto(ApplicationDbContext db, DateTime d, DateTime h)
        {
            var datos = db.Devoluciones.AsNoTracking().Where(x => x.Fecha >= d && x.Fecha < h && x.Estado == "Aplicada")
                .Select(x => new { x.Id, x.Numero, x.Total, x.MontoAplicadoSaldo, x.MontoReembolsado, x.MontoSaldoFavor }).ToList();
            var lista = datos.Where(x => Math.Abs(x.Total - (x.MontoAplicadoSaldo + x.MontoReembolsado + x.MontoSaldoFavor)) > Tol)
                .Select(x => new HallazgoIntegridad
                {
                    Documento = x.Numero, Id = x.Id, Pagina = "/Devoluciones/Details",
                    Detalle = $"Total L. {x.Total:N2} ≠ saldo L. {x.MontoAplicadoSaldo:N2} + reembolso L. {x.MontoReembolsado:N2} + saldo a favor L. {x.MontoSaldoFavor:N2}"
                }).ToList();
            return Nueva("Cobros y créditos", "Devoluciones: reparto cuadra con el total", "Lo devuelto se reparte entre saldo de la factura, reembolso y saldo a favor.", datos.Count, lista);
        }

        private static RevisionIntegridad DevolucionesLedger(ApplicationDbContext db, DateTime d, DateTime h)
        {
            var datos = db.Devoluciones.AsNoTracking().Where(x => x.Fecha >= d && x.Fecha < h && x.Estado == "Aplicada")
                .Select(x => new { x.Id, x.Numero, x.MontoSaldoFavor, x.MontoReembolsado, x.MovimientoReembolsoId }).ToList();
            var ids = datos.Select(x => x.Id).ToList();
            var cred = db.SaldoFavorMovimientos.AsNoTracking().Where(m => m.DevolucionId != null && ids.Contains(m.DevolucionId.Value) && m.Monto > 0)
                .GroupBy(m => m.DevolucionId!.Value).Select(g => new { Id = g.Key, S = g.Sum(x => x.Monto) }).ToList().ToDictionary(x => x.Id, x => x.S);
            var movIds = datos.Where(x => x.MovimientoReembolsoId != null).Select(x => x.MovimientoReembolsoId!.Value).ToList();
            var activos = db.MovimientosFinancieros.AsNoTracking().Where(m => movIds.Contains(m.Id) && m.Estado == "Activo").Select(m => m.Id).ToList().ToHashSet();
            var lista = new List<HallazgoIntegridad>();
            foreach (var x in datos)
            {
                if (Math.Abs(cred.GetValueOrDefault(x.Id) - x.MontoSaldoFavor) > Tol)
                    lista.Add(new HallazgoIntegridad { Documento = x.Numero, Id = x.Id, Pagina = "/Devoluciones/Details",
                        Detalle = $"Saldo a favor de la devolución L. {x.MontoSaldoFavor:N2}; acreditado al cliente L. {cred.GetValueOrDefault(x.Id):N2}" });
                if (x.MontoReembolsado > 0.005m && (x.MovimientoReembolsoId == null || !activos.Contains(x.MovimientoReembolsoId.Value)))
                    lista.Add(new HallazgoIntegridad { Documento = x.Numero, Id = x.Id, Pagina = "/Devoluciones/Details",
                        Detalle = $"Reembolso de L. {x.MontoReembolsado:N2} sin movimiento de egreso activo" });
            }
            return Nueva("Cobros y créditos", "Devoluciones: saldo a favor y reembolso registrados", "El saldo a favor debe estar acreditado al cliente y el reembolso debe tener su egreso.", datos.Count, lista);
        }

        private static RevisionIntegridad SaldoFavorNegativo(ApplicationDbContext db, DateTime d, DateTime h)
        {
            var todos = db.SaldoFavorMovimientos.AsNoTracking().GroupBy(m => m.ClienteId).Select(g => new { g.Key, S = g.Sum(x => x.Monto) }).ToList();
            var neg = todos.Where(x => x.S < -0.005m).ToList();
            var ids = neg.Select(n => n.Key).ToList();
            var nombres = db.Clientes.AsNoTracking().Where(c => ids.Contains(c.Id)).ToDictionary(c => c.Id, c => c.Nombre);
            var lista = neg.Select(n => new HallazgoIntegridad
            {
                Documento = nombres.GetValueOrDefault(n.Key, "Cliente " + n.Key), Id = n.Key, Pagina = "/Clientes/Details", Detalle = $"Saldo a favor negativo: L. {n.S:N2}"
            }).ToList();
            return Nueva("Cobros y créditos", "Saldo a favor de clientes", "Un cliente no puede usar más saldo del que tiene.", todos.Count, lista, true);
        }

        // ---------- FINANZAS ----------
        private static RevisionIntegridad SaldosCuentas(ApplicationDbContext db, DateTime d, DateTime h)
        {
            var cogs = db.CategoriasFinancieras.AsNoTracking()
                .Join(db.PlanCuentas, c => c.PlanCuentaId, p => (int?)p.Id, (c, p) => new { c.Id, p.Codigo })
                .Where(x => x.Codigo == "5.1.01").Select(x => x.Id).ToList().ToHashSet();
            var movs = db.MovimientosFinancieros.AsNoTracking()
                .Where(m => m.Estado == "Activo" && m.Origen != "Apertura")
                .Select(m => new { m.Tipo, m.CuentaId, m.CuentaDestinoId, m.Monto, m.Notas, m.CategoriaId }).ToList();
            var esperado = new Dictionary<int, decimal>();
            void Suma(int? id, decimal v) { if (id.HasValue) esperado[id.Value] = esperado.GetValueOrDefault(id.Value) + v; }
            foreach (var m in movs)
            {
                if (m.Notas != null && m.Notas.Contains(SaldoHelper.MarcaSoloContable)) continue;
                if (m.Tipo == "Egreso" && m.CategoriaId.HasValue && cogs.Contains(m.CategoriaId.Value)) continue;
                switch (m.Tipo)
                {
                    case "Ingreso": Suma(m.CuentaId, m.Monto); break;
                    case "Egreso": Suma(m.CuentaId, -m.Monto); break;
                    case "Transferencia": Suma(m.CuentaId, -m.Monto); Suma(m.CuentaDestinoId, m.Monto); break;
                }
            }
            var lista = new List<HallazgoIntegridad>();
            var cuentas = db.CuentasFinancieras.AsNoTracking().ToList();
            foreach (var c in cuentas)
            {
                var esp = c.SaldoInicial + esperado.GetValueOrDefault(c.Id);
                if (Math.Abs(esp - c.SaldoActual) > 0.01m)
                    lista.Add(new HallazgoIntegridad
                    {
                        Documento = c.Nombre, Enlace = "/Finanzas/Movimientos/Index?CuentaId=" + c.Id,
                        Detalle = $"Saldo en sistema L. {c.SaldoActual:N2}; según movimientos L. {esp:N2} (diferencia L. {c.SaldoActual - esp:N2})"
                    });
            }
            return Nueva("Finanzas", "Saldo de cada cuenta contra sus movimientos", "El saldo de cada caja/banco debe ser igual al saldo inicial más sus movimientos activos.", cuentas.Count, lista, true);
        }

        private static RevisionIntegridad MovimientosValidos(ApplicationDbContext db, DateTime d, DateTime h)
        {
            var q = db.MovimientosFinancieros.AsNoTracking().Where(m => m.Fecha >= d && m.Fecha < h && m.Estado == "Activo");
            var datos = q.Select(m => new { m.Id, m.Numero, m.Tipo, m.Monto, m.CuentaId, m.CuentaDestinoId, m.Concepto, m.Origen, m.Notas }).ToList();
            var cuentas = db.CuentasFinancieras.AsNoTracking().Select(c => c.Id).ToList().ToHashSet();
            var lista = new List<HallazgoIntegridad>();
            foreach (var m in datos)
            {
                string? problema = null;
                // La reversa del costo de una devolución es un egreso NEGATIVO solo contable (no toca caja): es válido
                var reversaCosto = m.Monto < 0 && m.Tipo == "Egreso" && m.Origen == "Devolucion"
                                   && (m.Notas ?? "").Contains(SaldoHelper.MarcaSoloContable);
                if (m.Monto <= 0 && !reversaCosto) problema = "Monto cero o negativo";
                else if (!cuentas.Contains(m.CuentaId)) problema = "La cuenta ya no existe";
                else if (m.Tipo == "Transferencia" && (m.CuentaDestinoId == null || !cuentas.Contains(m.CuentaDestinoId.Value) || m.CuentaDestinoId == m.CuentaId))
                    problema = "Transferencia sin cuenta destino válida";
                if (problema != null)
                    lista.Add(new HallazgoIntegridad { Documento = m.Numero, Enlace = "/Finanzas/Movimientos/Index?Q=" + Uri.EscapeDataString(m.Numero), Detalle = $"{problema} · {m.Concepto}" });
            }
            return Nueva("Finanzas", "Ingresos y egresos válidos", "Cada movimiento debe tener monto positivo y cuentas existentes.", datos.Count, lista);
        }

        // ---------- COMPRAS ----------
        private static RevisionIntegridad ComprasSinKardex(ApplicationDbContext db, DateTime d, DateTime h)
        {
            var ordenes = db.OrdenesCompra.AsNoTracking()
                .Where(o => o.Fecha >= d && o.Fecha < h && (o.Estado == "Recibida" || o.Estado == "RecibidaParcial") && o.LoteMigracion == null)
                .Select(o => new { o.Id, o.Numero }).ToList();
            var ids = ordenes.Select(o => o.Id).ToList();
            var conRecibido = db.DetalleOrdenesCompra.AsNoTracking().Where(x => ids.Contains(x.OrdenCompraId) && x.CantidadRecibida > 0)
                .Select(x => x.OrdenCompraId).Distinct().ToList().ToHashSet();
            var conKardex = db.MovimientosInventario.AsNoTracking()
                .Where(m => m.Origen == "Compra" && m.DocumentoId != null && ids.Contains(m.DocumentoId.Value))
                .Select(m => m.DocumentoId!.Value).Distinct().ToList().ToHashSet();
            var lista = ordenes.Where(o => conRecibido.Contains(o.Id) && !conKardex.Contains(o.Id)).Select(o => new HallazgoIntegridad
            {
                Documento = o.Numero, Id = o.Id, Pagina = "/OrdenesCompra/Details", Detalle = "Marcada como recibida pero sin entrada en el kardex"
            }).ToList();
            return Nueva("Compras", "Compras recibidas con entrada al kardex", "Toda mercadería recibida debe haber entrado al inventario.", ordenes.Count, lista);
        }

        private static RevisionIntegridad ComprasSaldoInvalido(ApplicationDbContext db, DateTime d, DateTime h)
        {
            var q = db.OrdenesCompra.AsNoTracking().Where(o => o.Fecha >= d && o.Fecha < h && o.Estado != "Cancelada" && o.Estado != "Anulada");
            var lista = q.Where(o => o.Saldo < -0.005m || o.Saldo > o.Total + 0.005m)
                .Select(o => new HallazgoIntegridad { Documento = o.Numero, Id = o.Id, Pagina = "/OrdenesCompra/Details", Detalle = "Saldo por pagar fuera de rango" }).ToList();
            return Nueva("Compras", "Saldos por pagar de órdenes", "El saldo debe estar entre 0 y el total.", q.Count(), lista);
        }

        private static RevisionIntegridad PagosProveedorMovimiento(ApplicationDbContext db, DateTime d, DateTime h)
        {
            var pagos = db.PagosProveedor.AsNoTracking().Where(p => p.Fecha >= d && p.Fecha < h).Select(p => new { p.Id, p.Numero, p.Monto }).ToList();
            var ids = pagos.Select(p => p.Id).ToList();
            var conMov = db.MovimientosFinancieros.AsNoTracking()
                .Where(m => m.Origen == "PagoProveedor" && m.OrigenId != null && ids.Contains(m.OrigenId.Value) && m.Estado == "Activo")
                .Select(m => m.OrigenId!.Value).Distinct().ToList().ToHashSet();
            var lista = pagos.Where(p => p.Monto > 0 && !conMov.Contains(p.Id)).Select(p => new HallazgoIntegridad
            {
                Documento = p.Numero ?? ("Pago " + p.Id), Id = p.Id, Pagina = "/PagosProveedor/Index", Detalle = $"Pago de L. {p.Monto:N2} sin egreso activo en finanzas"
            }).ToList();
            return Nueva("Compras", "Pagos a proveedores con movimiento", "Cada pago a proveedor debe haber sacado el dinero de una cuenta.", pagos.Count, lista);
        }

        // ---------- NÓMINA ----------
        private static RevisionIntegridad NominaPagosMovimiento(ApplicationDbContext db, DateTime d, DateTime h)
        {
            var pagos = db.PagosNominaEmpleado.AsNoTracking()
                .Where(p => p.Estado == "Pagado" && p.FechaPago >= d && p.FechaPago < h)
                .Select(p => new { p.Id, p.Numero, p.NominaId, p.Monto, p.MovimientoId }).ToList();
            var movIds = pagos.Where(p => p.MovimientoId != null).Select(p => p.MovimientoId!.Value).Distinct().ToList();
            var movs = db.MovimientosFinancieros.AsNoTracking()
                .Where(m => movIds.Contains(m.Id) && m.Estado == "Activo")
                .Select(m => new { m.Id, m.Numero, m.Monto }).ToList().ToDictionary(m => m.Id);

            var lista = pagos.Where(p => p.Monto > 0 && (p.MovimientoId == null || !movs.ContainsKey(p.MovimientoId.Value)))
                .Select(p => new HallazgoIntegridad
                {
                    Documento = p.Numero, Id = p.NominaId, Pagina = "/RRHH/Nomina/Details", Detalle = $"Pago de L. {p.Monto:N2} sin egreso activo en finanzas"
                }).ToList();

            // Un mismo egreso puede cubrir a varios empleados (pago total): la suma de sus pagos debe ser el monto del egreso
            lista.AddRange(pagos.Where(p => p.MovimientoId != null && movs.ContainsKey(p.MovimientoId.Value))
                .GroupBy(p => p.MovimientoId!.Value)
                .Where(g => Math.Abs(g.Sum(p => p.Monto) - movs[g.Key].Monto) > 0.005m)
                .Select(g => new HallazgoIntegridad
                {
                    Documento = movs[g.Key].Numero, Id = g.First().NominaId, Pagina = "/RRHH/Nomina/Details",
                    Detalle = $"Egreso de L. {movs[g.Key].Monto:N2}; los pagos a empleados suman L. {g.Sum(p => p.Monto):N2}"
                }));
            return Nueva("Nómina", "Pagos de nómina con egreso que cuadra", "Cada pago de nómina debe haber sacado exactamente ese dinero de una cuenta.", pagos.Count, lista);
        }

        private static RevisionIntegridad NominaDetalleAritmetica(ApplicationDbContext db, DateTime d, DateTime h)
        {
            var detalles = (from x in db.DetalleNominas.AsNoTracking()
                            join n in db.Nominas.AsNoTracking() on x.NominaId equals n.Id
                            where n.Estado != "Anulada" && n.FechaInicio >= d && n.FechaInicio < h
                            select new
                            {
                                n.Id, n.Numero, x.EmpleadoId, x.TotalBruto, x.TotalDeducciones, x.SalarioNeto,
                                Suma = x.DeduccionIHSS + x.DeduccionRAP + x.DeduccionISR + x.DeduccionVales + x.OtrasDeducciones
                            }).ToList();
            var lista = detalles.Where(x => Math.Abs(x.Suma - x.TotalDeducciones) > 0.005m
                                         || Math.Abs(Math.Max(0m, x.TotalBruto - x.TotalDeducciones) - x.SalarioNeto) > 0.005m)
                .Select(x => new HallazgoIntegridad
                {
                    Documento = x.Numero, Id = x.Id, Pagina = "/RRHH/Nomina/Details",
                    Detalle = $"Empleado #{x.EmpleadoId}: bruto L. {x.TotalBruto:N2} − deducciones L. {x.TotalDeducciones:N2} (suma real L. {x.Suma:N2}) ≠ neto L. {x.SalarioNeto:N2}"
                }).ToList();
            return Nueva("Nómina", "Neto = bruto − deducciones", "En cada empleado el neto debe ser el bruto menos la suma de sus deducciones.", detalles.Count, lista);
        }

        // ---------- INVENTARIO ----------
        private static RevisionIntegridad StockVsKardex(ApplicationDbContext db, DateTime d, DateTime h)
        {
            var ultimos = db.MovimientosInventario.AsNoTracking().GroupBy(m => m.ProductoId).Select(g => g.Max(x => x.Id)).ToList();
            var movs = new List<(int ProductoId, decimal Post)>();
            foreach (var lote in ultimos.Chunk(1000))
            {
                var ids = lote.ToList();
                movs.AddRange(db.MovimientosInventario.AsNoTracking().Where(m => ids.Contains(m.Id))
                    .Select(m => new { m.ProductoId, m.StockPosterior }).ToList().Select(x => (x.ProductoId, x.StockPosterior)));
            }
            var prods = db.Productos.AsNoTracking().Select(p => new { p.Id, p.Nombre, p.Stock }).ToList().ToDictionary(p => p.Id);
            var lista = movs.Where(m => prods.ContainsKey(m.ProductoId) && Math.Abs(prods[m.ProductoId].Stock - m.Post) > 0.0001m)
                .Select(m => new HallazgoIntegridad
                {
                    Documento = prods[m.ProductoId].Nombre, Id = m.ProductoId, Pagina = "/Productos/Historial",
                    Detalle = $"Stock {prods[m.ProductoId].Stock:0.####}; el kardex termina en {m.Post:0.####}"
                }).ToList();
            return Nueva("Inventario", "Stock igual al kardex", "El stock debe coincidir con el último saldo del kardex.", movs.Count, lista, true);
        }

        // ---------- CONTABILIDAD ----------
        private static RevisionIntegridad AsientosDescuadrados(ApplicationDbContext db, DateTime d, DateTime h)
        {
            var sumas = (from x in db.DetallesAsiento.AsNoTracking()
                         join a in db.AsientosContables.AsNoTracking() on x.AsientoContableId equals a.Id
                         where a.Estado == "Activo" && a.Fecha >= d && a.Fecha < h
                         group x by new { a.Id, a.Numero } into g
                         select new { g.Key.Id, g.Key.Numero, Debe = g.Sum(y => y.Debe), Haber = g.Sum(y => y.Haber) }).ToList();
            var lista = sumas.Where(s => Math.Abs(s.Debe - s.Haber) > 0.01m).Select(s => new HallazgoIntegridad
            {
                Documento = s.Numero, Id = s.Id, Pagina = "/Finanzas/Asientos/Details", Detalle = $"Debe L. {s.Debe:N2} ≠ Haber L. {s.Haber:N2}"
            }).ToList();
            return Nueva("Contabilidad", "Asientos con debe = haber", "En cada asiento el debe debe ser igual al haber.", sumas.Count, lista);
        }
    }
}
