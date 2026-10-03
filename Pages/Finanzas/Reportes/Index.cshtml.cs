using Kirkenta.Data;
using Kirkenta.Helpers;
using Kirkenta.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Kirkenta.Pages.Finanzas.Reportes
{
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public IndexModel(ApplicationDbContext context)
        {
            _context = context;
        }

        // Filtros
        public DateTime Desde { get; set; } = new DateTime(DateTime.Today.Year, 1, 1);
        public DateTime Hasta { get; set; } = DateTime.Today;
        public int? FiltroCuentaId { get; set; }
        public int? FiltroCategoriaId { get; set; }
        public string TabActiva { get; set; } = "flujo";

        // Listas para filtros
        public List<CuentaFinanciera> Cuentas { get; set; } = new();
        public List<CategoriaFinanciera> Categorias { get; set; } = new();

        // KPIs generales
        public decimal TotalIngresos { get; set; }
        public decimal TotalEgresos { get; set; }
        public decimal Balance { get; set; }
        public int CantidadMovimientos { get; set; }

        // Flujo de caja por mes
        public List<FlujoMensual> FlujoCaja { get; set; } = new();
        public string FlujoCajaJson { get; set; } = "{}";

        // Estado de resultados (por categoría)
        public List<ResultadoCategoria> IngresosPorCategoria { get; set; } = new();
        public List<ResultadoCategoria> EgresosPorCategoria { get; set; } = new();

        // Estado de cuenta
        public List<MovimientoDetalle> Movimientos { get; set; } = new();
        public decimal SaldoInicialCuenta { get; set; }
        public decimal SaldoFinalCuenta { get; set; }

        public class FlujoMensual
        {
            public string Mes { get; set; } = "";
            public int Anio { get; set; }
            public int NumeroMes { get; set; }
            public decimal Ingresos { get; set; }
            public decimal Egresos { get; set; }
            public decimal Balance => Ingresos - Egresos;
        }

        public class ResultadoCategoria
        {
            public int? CategoriaId { get; set; }
            public string Nombre { get; set; } = "";
            public string? Color { get; set; }
            public int Cantidad { get; set; }
            public decimal Total { get; set; }
            public decimal Porcentaje { get; set; }
        }

        public class MovimientoDetalle
        {
            public int Id { get; set; }
            public string Numero { get; set; } = "";
            public DateTime Fecha { get; set; }
            public string Tipo { get; set; } = "";
            public string Concepto { get; set; } = "";
            public string CuentaNombre { get; set; } = "";
            public string? CategoriaNombre { get; set; }
            public string? CategoriaColor { get; set; }
            public string? Referencia { get; set; }
            public decimal Monto { get; set; }
            public decimal MontoFirmado { get; set; }
            public decimal SaldoAcumulado { get; set; }
        }

        public IActionResult OnGet(DateTime? desde, DateTime? hasta, int? cuentaId, int? categoriaId, string tab = "flujo")
        {
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.Username == User.Identity!.Name);
            var currentRol = currentUser?.Rol ?? "Pendiente";

            if (currentRol != "Admin" &&
                !PermisoHelper.TienePermiso(_context, currentRol, "Finanzas", "Reportes", "ver"))
            {
                TempData["Error"] = "No tienes permiso";
                return RedirectToPage("/Finanzas/Index");
            }

            // Filtros
            if (desde.HasValue) Desde = desde.Value;
            if (hasta.HasValue) Hasta = hasta.Value;
            FiltroCuentaId = cuentaId;
            FiltroCategoriaId = categoriaId;
            TabActiva = tab ?? "flujo";

            Cuentas = _context.CuentasFinancieras
                .AsNoTracking()
                .Where(c => c.Activa)
                .OrderBy(c => c.Nombre)
                .ToList();

            Categorias = _context.CategoriasFinancieras
                .AsNoTracking()
                .Where(c => c.Activa)
                .OrderBy(c => c.Tipo).ThenBy(c => c.Nombre)
                .ToList();

            var desdeFull = Desde.Date;
            var hastaFull = Hasta.Date.AddDays(1).AddSeconds(-1);

            // Cargar movimientos activos en el rango
            var query = _context.MovimientosFinancieros
                .AsNoTracking()
                .Where(m => m.Estado == "Activo" && m.Fecha >= desdeFull && m.Fecha <= hastaFull);

            if (FiltroCuentaId.HasValue)
            {
                query = query.Where(m => m.CuentaId == FiltroCuentaId.Value || m.CuentaDestinoId == FiltroCuentaId.Value);
            }
            if (FiltroCategoriaId.HasValue)
            {
                query = query.Where(m => m.CategoriaId == FiltroCategoriaId.Value);
            }

            var movimientos = query.OrderBy(m => m.Fecha).ThenBy(m => m.Id).ToList();

            // Diccionarios
            var cuentasDict = Cuentas.ToDictionary(c => c.Id, c => c.Nombre);
            var categoriasDict = Categorias.ToDictionary(c => c.Id, c => new { c.Nombre, c.Color });

            // KPIs
            TotalIngresos = movimientos.Where(m => m.Tipo == "Ingreso").Sum(m => m.Monto);
            TotalEgresos = movimientos.Where(m => m.Tipo == "Egreso").Sum(m => m.Monto);
            Balance = TotalIngresos - TotalEgresos;
            CantidadMovimientos = movimientos.Count;

            // === FLUJO DE CAJA POR MES ===
            var flujo = new List<FlujoMensual>();
            var cursor = new DateTime(Desde.Year, Desde.Month, 1);
            var finRango = new DateTime(Hasta.Year, Hasta.Month, 1);

            while (cursor <= finRango)
            {
                var inicio = cursor;
                var fin = cursor.AddMonths(1);

                var ingMes = movimientos
                    .Where(m => m.Fecha >= inicio && m.Fecha < fin && m.Tipo == "Ingreso")
                    .Sum(m => m.Monto);
                var egrMes = movimientos
                    .Where(m => m.Fecha >= inicio && m.Fecha < fin && m.Tipo == "Egreso")
                    .Sum(m => m.Monto);

                flujo.Add(new FlujoMensual
                {
                    Mes = inicio.ToString("MMM yyyy"),
                    Anio = inicio.Year,
                    NumeroMes = inicio.Month,
                    Ingresos = ingMes,
                    Egresos = egrMes
                });

                cursor = cursor.AddMonths(1);
            }

            FlujoCaja = flujo;
            FlujoCajaJson = JsonSerializer.Serialize(new
            {
                labels = flujo.Select(f => f.Mes).ToArray(),
                ingresos = flujo.Select(f => f.Ingresos).ToArray(),
                egresos = flujo.Select(f => f.Egresos).ToArray(),
                balance = flujo.Select(f => f.Balance).ToArray()
            });

            // === ESTADO DE RESULTADOS ===
            // Ingresos por categoría
            var totalIngresos = TotalIngresos > 0 ? TotalIngresos : 1;
            IngresosPorCategoria = movimientos
                .Where(m => m.Tipo == "Ingreso")
                .GroupBy(m => m.CategoriaId)
                .Select(g => new ResultadoCategoria
                {
                    CategoriaId = g.Key,
                    Nombre = g.Key.HasValue ? categoriasDict.GetValueOrDefault(g.Key.Value)?.Nombre ?? "Sin categoría" : "Sin categoría",
                    Color = g.Key.HasValue ? categoriasDict.GetValueOrDefault(g.Key.Value)?.Color : "#6b7280",
                    Cantidad = g.Count(),
                    Total = g.Sum(m => m.Monto),
                    Porcentaje = Math.Round(g.Sum(m => m.Monto) / totalIngresos * 100, 2)
                })
                .OrderByDescending(x => x.Total)
                .ToList();

            // Egresos por categoría
            var totalEgresos = TotalEgresos > 0 ? TotalEgresos : 1;
            EgresosPorCategoria = movimientos
                .Where(m => m.Tipo == "Egreso")
                .GroupBy(m => m.CategoriaId)
                .Select(g => new ResultadoCategoria
                {
                    CategoriaId = g.Key,
                    Nombre = g.Key.HasValue ? categoriasDict.GetValueOrDefault(g.Key.Value)?.Nombre ?? "Sin categoría" : "Sin categoría",
                    Color = g.Key.HasValue ? categoriasDict.GetValueOrDefault(g.Key.Value)?.Color : "#6b7280",
                    Cantidad = g.Count(),
                    Total = g.Sum(m => m.Monto),
                    Porcentaje = Math.Round(g.Sum(m => m.Monto) / totalEgresos * 100, 2)
                })
                .OrderByDescending(x => x.Total)
                .ToList();

            // === ESTADO DE CUENTA (con saldo acumulado) ===
            if (FiltroCuentaId.HasValue)
            {
                // Saldo inicial = saldo actual menos los movimientos en el rango (para esta cuenta)
                var cuenta = Cuentas.FirstOrDefault(c => c.Id == FiltroCuentaId.Value);
                var saldoActual = cuenta?.SaldoActual ?? 0;

                // Efecto neto de los movimientos del rango sobre esta cuenta
                decimal efectoRango = 0;
                foreach (var m in movimientos)
                {
                    if (m.CuentaId == FiltroCuentaId.Value)
                    {
                        if (m.Tipo == "Ingreso") efectoRango += m.Monto;
                        else if (m.Tipo == "Egreso") efectoRango -= m.Monto;
                        else if (m.Tipo == "Transferencia") efectoRango -= m.Monto;
                    }
                    if (m.CuentaDestinoId == FiltroCuentaId.Value && m.Tipo == "Transferencia")
                    {
                        efectoRango += m.Monto;
                    }
                }

                // Saldo inicial aproximado = saldo actual - efecto de movimientos posteriores
                // Simplificamos: saldoInicial = saldoActual - efecto total de movimientos en rango
                // (Nota: no considera movimientos fuera del rango, es una aproximación)
                SaldoInicialCuenta = saldoActual - efectoRango;
                SaldoFinalCuenta = saldoActual;

                decimal saldoAcum = SaldoInicialCuenta;
                foreach (var m in movimientos)
                {
                    decimal montoFirmado = 0;
                    if (m.CuentaId == FiltroCuentaId.Value)
                    {
                        if (m.Tipo == "Ingreso") montoFirmado = m.Monto;
                        else if (m.Tipo == "Egreso") montoFirmado = -m.Monto;
                        else if (m.Tipo == "Transferencia") montoFirmado = -m.Monto;
                    }
                    if (m.CuentaDestinoId == FiltroCuentaId.Value && m.Tipo == "Transferencia")
                    {
                        montoFirmado = m.Monto;
                    }

                    saldoAcum += montoFirmado;

                    Movimientos.Add(new MovimientoDetalle
                    {
                        Id = m.Id,
                        Numero = m.Numero,
                        Fecha = m.Fecha,
                        Tipo = m.Tipo,
                        Concepto = m.Concepto,
                        CuentaNombre = cuentasDict.GetValueOrDefault(m.CuentaId, "—"),
                        CategoriaNombre = m.CategoriaId.HasValue ? categoriasDict.GetValueOrDefault(m.CategoriaId.Value)?.Nombre : null,
                        CategoriaColor = m.CategoriaId.HasValue ? categoriasDict.GetValueOrDefault(m.CategoriaId.Value)?.Color : null,
                        Referencia = m.Referencia,
                        Monto = m.Monto,
                        MontoFirmado = montoFirmado,
                        SaldoAcumulado = saldoAcum
                    });
                }
            }

            return Page();
        }
    }
}