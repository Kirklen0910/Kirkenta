using Kirkenta.Data;
using Kirkenta.Models;

namespace Kirkenta.Helpers.Finanzas
{
    /// <summary>
    /// Registra el saldo inicial de las cuentas financieras (Caja/Banco) como
    /// movimientos contables de tipo "Ingreso" asociados a una categoría de Activo.
    ///
    /// Esto es necesario para que el Balance General refleje el Activo real.
    ///
    /// Regla: solo se genera un movimiento de apertura si la cuenta tiene
    /// SaldoInicial > 0 y NO existe ya un movimiento de apertura para esa cuenta.
    /// Es idempotente.
    /// </summary>
    public static class AperturaCuentaHelper
    {
        /// <summary>
        /// Nombre de la categoría de ingreso que se usa como contrapartida
        /// del saldo inicial de una cuenta de Caja/Banco.
        /// </summary>
        public const string CategoriaAperturaNombre = "Apertura de cuenta";

        /// <summary>
        /// Alias legacy. Registrar apertura para UNA cuenta específica.
        /// Se llama al crear una nueva cuenta financiera con SaldoInicial > 0.
        /// Firma exacta que espera Pages/Finanzas/Cuentas/Create.cshtml.cs.
        /// </summary>
        public static (MovimientoFinanciero? mov, string? error) RegistrarApertura(
            ApplicationDbContext context,
            int cuentaId,
            int? usuarioId = null)
        {
            return RegistrarAperturaCuenta(context, cuentaId, usuarioId);
        }

        /// <summary>
        /// Crea el movimiento de apertura para UNA cuenta específica.
        /// </summary>
        public static (MovimientoFinanciero? mov, string? error) RegistrarAperturaCuenta(
            ApplicationDbContext context,
            int cuentaId,
            int? usuarioId = null)
        {
            try
            {
                var cuenta = context.CuentasFinancieras.FirstOrDefault(c => c.Id == cuentaId);
                if (cuenta == null) return (null, "Cuenta no encontrada");

                if (cuenta.SaldoInicial <= 0) return (null, "La cuenta no tiene saldo inicial");

                bool yaExiste = context.MovimientosFinancieros
                    .Any(m => m.Origen == "Apertura" && m.OrigenId == cuentaId);

                if (yaExiste) return (null, "Ya existe un movimiento de apertura para esta cuenta");

                var categoria = ObtenerOCrearCategoriaApertura(context);
                var monedaDefecto = context.Monedas
                    .FirstOrDefault(m => m.EsPredeterminada)?.Id ?? 1;

                var numero = NumeroDocumentoHelper.GenerarSiguiente(context, "MovimientoFinanciero");

                var movimiento = new MovimientoFinanciero
                {
                    Numero = numero,
                    Tipo = "Ingreso",
                    Fecha = cuenta.FechaCreacion,
                    CuentaId = cuenta.Id,
                    CategoriaId = categoria.Id,
                    Monto = cuenta.SaldoInicial,
                    MonedaId = monedaDefecto,
                    TipoCambio = 1,
                    Concepto = $"Saldo inicial de la cuenta '{cuenta.Nombre}'",
                    Referencia = cuenta.Codigo,
                    FormaPago = "Efectivo",
                    Origen = "Apertura",
                    OrigenId = cuenta.Id,
                    EsAutomatico = true,
                    Notas = "Movimiento contable automático por saldo inicial de la cuenta",
                    Estado = "Activo",
                    FechaCreacion = DateTime.Now,
                    UsuarioCreoId = usuarioId,
                    EmpresaId = EmpresaActual.Id
                };

                context.MovimientosFinancieros.Add(movimiento);
                context.SaveChanges();

                // ⚠️ NO aplicar SaldoHelper.Aplicar porque el SaldoActual
                //     ya contempla el SaldoInicial al crear la cuenta.
                //     Este movimiento es SOLO para el reporte contable.

                return (movimiento, null);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AperturaCuentaHelper] Error: {ex.Message}");
                return (null, ex.Message);
            }
        }

        /// <summary>
        /// Procesa TODAS las cuentas activas con saldo inicial > 0 que no
        /// tengan ya su movimiento de apertura.
        /// Se llama al arrancar la app desde Program.cs.
        /// Devuelve cuántos movimientos se crearon.
        /// </summary>
        public static int ProcesarAperturasPendientes(ApplicationDbContext context, int? usuarioId = null)
        {
            try
            {
                var cuentas = context.CuentasFinancieras
                    .Where(c => c.Activa && c.SaldoInicial > 0)
                    .Where(c => c.Subtipo == null || c.Subtipo != "CuentasPorCobrar")
                    .ToList();

                if (cuentas.Count == 0) return 0;

                int creados = 0;

                foreach (var cuenta in cuentas)
                {
                    var (mov, error) = RegistrarAperturaCuenta(context, cuenta.Id, usuarioId);
                    if (mov != null) creados++;
                }

                return creados;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AperturaCuentaHelper] Error en ProcesarAperturasPendientes: {ex.Message}");
                return 0;
            }
        }

        /// <summary>
        /// Obtiene (o crea) la categoría de ingreso "Apertura de cuenta",
        /// que sirve como contrapartida del saldo inicial.
        /// Se asocia al plan de cuentas de Activo (1.1.01.001 - Caja General).
        /// </summary>
        private static CategoriaFinanciera ObtenerOCrearCategoriaApertura(ApplicationDbContext context)
        {
            var categoria = context.CategoriasFinancieras
                .FirstOrDefault(c => c.Tipo == "Ingreso" && c.Nombre == CategoriaAperturaNombre);

            if (categoria != null) return categoria;

            // Buscar la cuenta de plan "Caja General" (o cualquier 1.1.01.x)
            int? planCuentaId = null;
            var planCuenta = context.PlanCuentas
                .FirstOrDefault(p => p.Codigo == "1.1.01.001")
                ?? context.PlanCuentas.FirstOrDefault(p => p.Codigo.StartsWith("1.1.01"));

            if (planCuenta != null) planCuentaId = planCuenta.Id;

            categoria = new CategoriaFinanciera
            {
                Tipo = "Ingreso",
                Nombre = CategoriaAperturaNombre,
                Descripcion = "Saldo inicial de cuentas financieras (contrapartida contable de Activo)",
                CuentaContable = planCuenta?.Codigo,
                PlanCuentaId = planCuentaId,
                Color = "#14b8a6",
                EsSistema = true,
                Activa = true,
                FechaCreacion = DateTime.Now,
                EmpresaId = EmpresaActual.Id
            };

            context.CategoriasFinancieras.Add(categoria);
            context.SaveChanges();

            return categoria;
        }
    }
}