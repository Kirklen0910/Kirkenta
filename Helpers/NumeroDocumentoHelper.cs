using Kirkenta.Data;
using Kirkenta.Models;

namespace Kirkenta.Helpers
{
    public static class NumeroDocumentoHelper
    {
        public static string GenerarSiguiente(ApplicationDbContext context, string tipo)
        {
            var serie = context.SeriesDocumentos
                .FirstOrDefault(s => s.Tipo == tipo && s.EsPredeterminada && s.Activa);

            if (serie == null)
            {
                var count = ContarDocumentos(context, tipo);
                var prefijo = tipo.Substring(0, Math.Min(3, tipo.Length)).ToUpper();
                return $"{prefijo}-{(count + 1):D4}";
            }

            SincronizarSerie(context, serie, tipo);

            var numero = GenerarNumero(serie);
            serie.SiguienteNumero++;
            context.SaveChanges();

            return numero;
        }

        public static string PreviewSiguiente(ApplicationDbContext context, string tipo)
        {
            var serie = context.SeriesDocumentos
                .FirstOrDefault(s => s.Tipo == tipo && s.EsPredeterminada && s.Activa);

            if (serie == null)
            {
                var prefijo = tipo.Substring(0, Math.Min(3, tipo.Length)).ToUpper();
                return $"{prefijo}-0001";
            }

            SincronizarSerie(context, serie, tipo);
            return GenerarNumero(serie);
        }

        private static string GenerarNumero(SerieDocumento serie)
        {
            var numero = serie.SiguienteNumero.ToString().PadLeft(serie.LongitudNumero, '0');
            var prefijo = serie.Prefijo ?? "";
            var sufijo = serie.Sufijo ?? "";
            var sep = serie.Separador ?? "-";
            var anio = DateTime.Now.Year.ToString();
            var mes = DateTime.Now.Month.ToString("D2");
            var dia = DateTime.Now.Day.ToString("D2");

            if (!string.IsNullOrWhiteSpace(serie.FormatoPersonalizado))
            {
                return serie.FormatoPersonalizado
                    .Replace("{PREFIX}", prefijo)
                    .Replace("{PREFIJO}", prefijo)
                    .Replace("{SUFFIX}", sufijo)
                    .Replace("{SUFIJO}", sufijo)
                    .Replace("{SEP}", sep)
                    .Replace("{YEAR}", anio)
                    .Replace("{ANIO}", anio)
                    .Replace("{MONTH}", mes)
                    .Replace("{MES}", mes)
                    .Replace("{DAY}", dia)
                    .Replace("{DIA}", dia)
                    .Replace("{NUM}", numero);
            }

            return $"{prefijo}{sep}{numero}";
        }

        private static void SincronizarSerie(ApplicationDbContext context, SerieDocumento serie, string tipo)
        {
            try
            {
                int ultimoNumero = 0;
                var prefijoConSep = (serie.Prefijo ?? "") + (serie.Separador ?? "-");

                switch (tipo)
                {
                    // ===== VENTAS =====
                    case "Cotizacion":
                        ultimoNumero = context.Cotizaciones
                            .Where(c => c.Numero.StartsWith(prefijoConSep))
                            .Select(c => c.Numero)
                            .AsEnumerable()
                            .Select(s => ExtraerNumero(s!, prefijoConSep))
                            .DefaultIfEmpty(0)
                            .Max();
                        break;

                    case "Pedido":
                        ultimoNumero = context.Pedidos
                            .Where(p => p.Numero.StartsWith(prefijoConSep))
                            .Select(p => p.Numero)
                            .AsEnumerable()
                            .Select(s => ExtraerNumero(s!, prefijoConSep))
                            .DefaultIfEmpty(0)
                            .Max();
                        break;

                    case "Venta":
                        ultimoNumero = context.Ventas
                            .Where(v => v.Numero.StartsWith(prefijoConSep))
                            .Select(v => v.Numero)
                            .AsEnumerable()
                            .Select(s => ExtraerNumero(s!, prefijoConSep))
                            .DefaultIfEmpty(0)
                            .Max();
                        break;

                    case "Factura":
                        ultimoNumero = context.Facturas
                            .Where(f => f.Numero.StartsWith(prefijoConSep))
                            .Select(f => f.Numero)
                            .AsEnumerable()
                            .Select(s => ExtraerNumero(s!, prefijoConSep))
                            .DefaultIfEmpty(0)
                            .Max();
                        break;

                    case "Devolucion":
                        ultimoNumero = context.Devoluciones
                            .Where(d => d.Numero.StartsWith(prefijoConSep))
                            .Select(d => d.Numero)
                            .AsEnumerable()
                            .Select(s => ExtraerNumero(s!, prefijoConSep))
                            .DefaultIfEmpty(0)
                            .Max();
                        break;

                    // ===== INVENTARIO =====
                    case "Producto":
                        ultimoNumero = context.Productos
                            .Where(p => p.SKU != null && p.SKU.StartsWith(prefijoConSep))
                            .Select(p => p.SKU)
                            .AsEnumerable()
                            .Select(s => ExtraerNumero(s!, prefijoConSep))
                            .DefaultIfEmpty(0)
                            .Max();
                        break;

                    case "Baja":
                        ultimoNumero = context.BajasInventario
                            .Where(b => b.Numero.StartsWith(prefijoConSep))
                            .Select(b => b.Numero)
                            .AsEnumerable()
                            .Select(s => ExtraerNumero(s!, prefijoConSep))
                            .DefaultIfEmpty(0)
                            .Max();
                        break;

                    // ===== COMPRAS =====
                    case "Proveedor":
                        ultimoNumero = context.Proveedores
                            .Where(p => p.Codigo != null && p.Codigo.StartsWith(prefijoConSep))
                            .Select(p => p.Codigo!)
                            .AsEnumerable()
                            .Select(s => ExtraerNumero(s!, prefijoConSep))
                            .DefaultIfEmpty(0)
                            .Max();
                        break;

                    case "OrdenCompra":
                        ultimoNumero = context.OrdenesCompra
                            .Where(o => o.Numero.StartsWith(prefijoConSep))
                            .Select(o => o.Numero)
                            .AsEnumerable()
                            .Select(s => ExtraerNumero(s!, prefijoConSep))
                            .DefaultIfEmpty(0)
                            .Max();
                        break;

                    case "PagoProveedor":
                        ultimoNumero = context.PagosProveedor
                            .Where(p => p.Numero != null && p.Numero.StartsWith(prefijoConSep))
                            .Select(p => p.Numero!)
                            .AsEnumerable()
                            .Select(s => ExtraerNumero(s!, prefijoConSep))
                            .DefaultIfEmpty(0)
                            .Max();
                        break;

                    case "DevolucionProveedor":
                        ultimoNumero = context.DevolucionesProveedor
                            .Where(d => d.Numero.StartsWith(prefijoConSep))
                            .Select(d => d.Numero)
                            .AsEnumerable()
                            .Select(s => ExtraerNumero(s!, prefijoConSep))
                            .DefaultIfEmpty(0)
                            .Max();
                        break;

                    // ===== FINANZAS =====
                    case "MovimientoFinanciero":
                        ultimoNumero = context.MovimientosFinancieros
                            .Where(m => m.Numero.StartsWith(prefijoConSep))
                            .Select(m => m.Numero)
                            .AsEnumerable()
                            .Select(s => ExtraerNumero(s!, prefijoConSep))
                            .DefaultIfEmpty(0)
                            .Max();
                        break;

                    case "AperturaCaja":
                        ultimoNumero = context.AperturasCaja
                            .Where(a => a.Numero.StartsWith(prefijoConSep))
                            .Select(a => a.Numero)
                            .AsEnumerable()
                            .Select(s => ExtraerNumero(s!, prefijoConSep))
                            .DefaultIfEmpty(0)
                            .Max();
                        break;

                    case "CierreCaja":
                        ultimoNumero = context.CierresCaja
                            .Where(c => c.Numero.StartsWith(prefijoConSep))
                            .Select(c => c.Numero)
                            .AsEnumerable()
                            .Select(s => ExtraerNumero(s!, prefijoConSep))
                            .DefaultIfEmpty(0)
                            .Max();
                        break;

                    case "ConciliacionBancaria":
                        ultimoNumero = context.ConciliacionesBancarias
                            .Where(c => c.Numero.StartsWith(prefijoConSep))
                            .Select(c => c.Numero)
                            .AsEnumerable()
                            .Select(s => ExtraerNumero(s!, prefijoConSep))
                            .DefaultIfEmpty(0)
                            .Max();
                        break;

                    // ===== RRHH =====
                    case "Empleado":
                        ultimoNumero = context.Empleados
                            .Where(e => e.Codigo.StartsWith(prefijoConSep))
                            .Select(e => e.Codigo)
                            .AsEnumerable()
                            .Select(s => ExtraerNumero(s!, prefijoConSep))
                            .DefaultIfEmpty(0)
                            .Max();
                        break;

                    case "ValeEmpleado":
                        ultimoNumero = context.ValesEmpleado
                            .Where(v => v.Numero.StartsWith(prefijoConSep))
                            .Select(v => v.Numero)
                            .AsEnumerable()
                            .Select(s => ExtraerNumero(s!, prefijoConSep))
                            .DefaultIfEmpty(0)
                            .Max();
                        break;

                    case "Vacacion":
                        ultimoNumero = context.VacacionesEmpleado
                            .Where(v => v.Numero.StartsWith(prefijoConSep))
                            .Select(v => v.Numero)
                            .AsEnumerable()
                            .Select(s => ExtraerNumero(s!, prefijoConSep))
                            .DefaultIfEmpty(0)
                            .Max();
                        break;

                    case "PermisoEmpleado":
                        ultimoNumero = context.PermisosEmpleado
                            .Where(p => p.Numero.StartsWith(prefijoConSep))
                            .Select(p => p.Numero)
                            .AsEnumerable()
                            .Select(s => ExtraerNumero(s!, prefijoConSep))
                            .DefaultIfEmpty(0)
                            .Max();
                        break;

                    case "Nomina":
                        ultimoNumero = context.Nominas
                            .Where(n => n.Numero.StartsWith(prefijoConSep))
                            .Select(n => n.Numero)
                            .AsEnumerable()
                            .Select(s => ExtraerNumero(s!, prefijoConSep))
                            .DefaultIfEmpty(0)
                            .Max();
                        break;

                    case "PagoNomina":
                        ultimoNumero = context.PagosNominaEmpleado
                            .Where(p => p.Numero.StartsWith(prefijoConSep))
                            .Select(p => p.Numero)
                            .AsEnumerable()
                            .Select(s => ExtraerNumero(s!, prefijoConSep))
                            .DefaultIfEmpty(0)
                            .Max();
                        break;

                    // ===== LOGÍSTICA =====
                    case "Envio":
                        ultimoNumero = context.Envios
                            .Where(e => e.Numero.StartsWith(prefijoConSep))
                            .Select(e => e.Numero)
                            .AsEnumerable()
                            .Select(s => ExtraerNumero(s!, prefijoConSep))
                            .DefaultIfEmpty(0)
                            .Max();
                        break;

                    case "Ruta":
                        ultimoNumero = context.Rutas
                            .Where(r => r.Numero.StartsWith(prefijoConSep))
                            .Select(r => r.Numero)
                            .AsEnumerable()
                            .Select(s => ExtraerNumero(s!, prefijoConSep))
                            .DefaultIfEmpty(0)
                            .Max();
                        break;

                    case "Repartidor":
                        ultimoNumero = context.Repartidores
                            .Where(r => r.Codigo.StartsWith(prefijoConSep))
                            .Select(r => r.Codigo)
                            .AsEnumerable()
                            .Select(s => ExtraerNumero(s!, prefijoConSep))
                            .DefaultIfEmpty(0)
                            .Max();
                        break;
                }

                if (ultimoNumero >= serie.SiguienteNumero)
                {
                    serie.SiguienteNumero = ultimoNumero + 1;
                    context.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Aviso al sincronizar serie {tipo}: {ex.Message}");
            }
        }

        private static int ExtraerNumero(string texto, string prefijoConSep)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(texto)) return 0;

                var sinPrefijo = texto;
                if (!string.IsNullOrEmpty(prefijoConSep) && texto.StartsWith(prefijoConSep))
                {
                    sinPrefijo = texto.Substring(prefijoConSep.Length);
                }

                var match = System.Text.RegularExpressions.Regex.Match(sinPrefijo, @"(\d+)(?!.*\d)");
                if (match.Success && int.TryParse(match.Value, out var num))
                    return num;

                return 0;
            }
            catch
            {
                return 0;
            }
        }

        private static int ContarDocumentos(ApplicationDbContext context, string tipo)
        {
            return tipo switch
            {
                // Ventas
                "Cotizacion" => context.Cotizaciones.Count(),
                "Pedido" => context.Pedidos.Count(),
                "Venta" => context.Ventas.Count(),
                "Factura" => context.Facturas.Count(),
                "Devolucion" => context.Devoluciones.Count(),

                // Inventario
                "Producto" => context.Productos.Count(),
                "Baja" => context.BajasInventario.Count(),

                // Compras
                "Proveedor" => context.Proveedores.Count(),
                "OrdenCompra" => context.OrdenesCompra.Count(),
                "PagoProveedor" => context.PagosProveedor.Count(),
                "DevolucionProveedor" => context.DevolucionesProveedor.Count(),

                // Finanzas
                "MovimientoFinanciero" => context.MovimientosFinancieros.Count(),
                "AperturaCaja" => context.AperturasCaja.Count(),
                "CierreCaja" => context.CierresCaja.Count(),
                "ConciliacionBancaria" => context.ConciliacionesBancarias.Count(),

                // RRHH
                "Empleado" => context.Empleados.Count(),
                "ValeEmpleado" => context.ValesEmpleado.Count(),
                "Vacacion" => context.VacacionesEmpleado.Count(),
                "PermisoEmpleado" => context.PermisosEmpleado.Count(),
                "Nomina" => context.Nominas.Count(),
                "PagoNomina" => context.PagosNominaEmpleado.Count(),

                // Logística
                "Envio" => context.Envios.Count(),
                "Ruta" => context.Rutas.Count(),
                "Repartidor" => context.Repartidores.Count(),

                _ => 0
            };
        }
    }
}