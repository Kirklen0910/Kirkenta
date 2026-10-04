using Kirkenta.Data;
using Kirkenta.Models;
using Microsoft.EntityFrameworkCore;

namespace Kirkenta.Helpers.Finanzas
{
    public static class MovimientoAutomaticoHelper
    {
        /// <summary>
        /// Registra el ingreso por una venta del POS.
        /// Permite especificar la cuenta (usualmente la de la apertura activa).
        /// Todo se ejecuta dentro de una transacción atómica.
        /// </summary>
        public static MovimientoFinanciero? RegistrarIngresoVenta(
            ApplicationDbContext context,
            int ventaId,
            string numeroVenta,
            decimal monto,
            int usuarioId,
            string? formaPago = null,
            int? cuentaId = null)
        {
            try
            {
                var cuenta = cuentaId.HasValue
                    ? context.CuentasFinancieras.FirstOrDefault(c => c.Id == cuentaId.Value && c.Activa)
                    : ResolverCuentaVentas(context);

                if (cuenta == null)
                {
                    Console.WriteLine("[MovimientoAutomatico] No hay cuenta configurada para ventas POS");
                    return null;
                }

                var categoria = ResolverCategoria(context, "Ingreso", "Ventas al contado");
                if (categoria == null)
                {
                    Console.WriteLine("[MovimientoAutomatico] No hay categoría 'Ventas al contado'");
                    return null;
                }

                return CrearMovimientoAtomico(
                    context,
                    tipo: "Ingreso",
                    cuentaId: cuenta.Id,
                    categoriaId: categoria.Id,
                    monto: monto,
                    concepto: $"Venta POS {numeroVenta}",
                    referencia: numeroVenta,
                    formaPago: formaPago,
                    origen: "Venta",
                    origenId: ventaId,
                    usuarioId: usuarioId
                );
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MovimientoAutomatico] Error al registrar ingreso de venta: {ex.Message}");
                return null;
            }
        }

        public static MovimientoFinanciero? RegistrarEgresoPagoProveedor(
            ApplicationDbContext context,
            int pagoProveedorId,
            string numeroPago,
            decimal monto,
            int usuarioId,
            string nombreProveedor = "—",
            string? formaPago = null)
        {
            try
            {
                var cuenta = ResolverCuentaPagosProveedor(context);
                if (cuenta == null)
                {
                    Console.WriteLine("[MovimientoAutomatico] No hay cuenta configurada para pagos a proveedores");
                    return null;
                }

                var categoria = ResolverCategoria(context, "Egreso", "Pago a proveedores");
                if (categoria == null)
                {
                    Console.WriteLine("[MovimientoAutomatico] No hay categoría 'Pago a proveedores'");
                    return null;
                }

                return CrearMovimientoAtomico(
                    context,
                    tipo: "Egreso",
                    cuentaId: cuenta.Id,
                    categoriaId: categoria.Id,
                    monto: monto,
                    concepto: $"Pago a proveedor {nombreProveedor}",
                    referencia: numeroPago,
                    formaPago: formaPago,
                    origen: "PagoProveedor",
                    origenId: pagoProveedorId,
                    usuarioId: usuarioId
                );
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MovimientoAutomatico] Error al registrar egreso de pago: {ex.Message}");
                return null;
            }
        }

        public static MovimientoFinanciero? RegistrarEgresoNomina(
            ApplicationDbContext context,
            int nominaId,
            string numeroNomina,
            decimal monto,
            int usuarioId)
        {
            try
            {
                var cuenta = ResolverCuentaDefault(context);
                if (cuenta == null) return null;

                var categoria = ResolverCategoria(context, "Egreso", "Nómina");
                if (categoria == null) return null;

                return CrearMovimientoAtomico(
                    context,
                    tipo: "Egreso",
                    cuentaId: cuenta.Id,
                    categoriaId: categoria.Id,
                    monto: monto,
                    concepto: $"Pago de nómina {numeroNomina}",
                    referencia: numeroNomina,
                    formaPago: "Transferencia",
                    origen: "Nomina",
                    origenId: nominaId,
                    usuarioId: usuarioId
                );
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MovimientoAutomatico] Error al registrar nómina: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Crea el movimiento + aplica saldo + guarda todo dentro de una transacción atómica.
        /// Si algo falla en el medio, se revierte TODO.
        /// </summary>
        private static MovimientoFinanciero? CrearMovimientoAtomico(
            ApplicationDbContext context,
            string tipo,
            int cuentaId,
            int? categoriaId,
            decimal monto,
            string concepto,
            string? referencia,
            string? formaPago,
            string origen,
            int? origenId,
            int usuarioId)
        {
            using var transaction = context.Database.BeginTransaction();
            try
            {
                // Validar que la cuenta exista y esté activa
                var cuenta = context.CuentasFinancieras.FirstOrDefault(c => c.Id == cuentaId);
                if (cuenta == null)
                {
                    transaction.Rollback();
                    Console.WriteLine($"[MovimientoAutomatico] Cuenta {cuentaId} no encontrada");
                    return null;
                }

                if (!cuenta.Activa)
                {
                    transaction.Rollback();
                    Console.WriteLine($"[MovimientoAutomatico] Cuenta {cuentaId} inactiva");
                    return null;
                }

                var numero = NumeroDocumentoHelper.GenerarSiguiente(context, "MovimientoFinanciero");
                var monedaDefecto = context.Monedas.FirstOrDefault(m => m.EsPredeterminada)?.Id ?? 1;

                var mov = new MovimientoFinanciero
                {
                    Numero = numero,
                    Tipo = tipo,
                    Fecha = DateTime.Now,
                    CuentaId = cuentaId,
                    CuentaDestinoId = null,
                    CategoriaId = categoriaId,
                    Monto = monto,
                    MonedaId = monedaDefecto,
                    TipoCambio = 1,
                    Concepto = concepto,
                    Referencia = referencia,
                    FormaPago = formaPago,
                    Origen = origen,
                    OrigenId = origenId,
                    EsAutomatico = true,
                    Estado = "Activo",
                    FechaCreacion = DateTime.Now,
                    UsuarioCreoId = usuarioId,
                    EmpresaId = 1
                };

                context.MovimientosFinancieros.Add(mov);
                context.SaveChanges();

                // Aplicar saldo
                SaldoHelper.Aplicar(context, mov);
                context.SaveChanges();

                transaction.Commit();
                return mov;
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                Console.WriteLine($"[MovimientoAutomatico] Error en transacción, rollback aplicado: {ex.Message}");
                return null;
            }
        }

        private static CuentaFinanciera? ResolverCuentaVentas(ApplicationDbContext context)
        {
            var cuentaPOS = context.CuentasFinancieras
                .FirstOrDefault(c => c.Tipo == "Caja" && c.Subtipo == "POS" && c.Activa);

            if (cuentaPOS != null) return cuentaPOS;

            return context.CuentasFinancieras
                .Where(c => c.Tipo == "Caja" && c.Activa)
                .OrderBy(c => c.Id)
                .FirstOrDefault();
        }

        private static CuentaFinanciera? ResolverCuentaPagosProveedor(ApplicationDbContext context)
        {
            var banco = context.CuentasFinancieras
                .Where(c => c.Tipo == "Banco" && c.Activa)
                .OrderBy(c => c.Id)
                .FirstOrDefault();

            if (banco != null) return banco;

            var cajaGeneral = context.CuentasFinancieras
                .FirstOrDefault(c => c.Tipo == "Caja" && c.Subtipo == "General" && c.Activa);

            if (cajaGeneral != null) return cajaGeneral;

            return context.CuentasFinancieras
                .Where(c => c.Tipo == "Caja" && c.Activa)
                .OrderBy(c => c.Id)
                .FirstOrDefault();
        }

        private static CuentaFinanciera? ResolverCuentaDefault(ApplicationDbContext context)
        {
            return context.CuentasFinancieras
                .Where(c => c.Activa)
                .OrderBy(c => c.Id)
                .FirstOrDefault();
        }

        private static CategoriaFinanciera? ResolverCategoria(ApplicationDbContext context, string tipo, string nombre)
        {
            var cat = context.CategoriasFinancieras
                .FirstOrDefault(c => c.Tipo == tipo && c.Nombre == nombre && c.Activa);

            if (cat != null) return cat;

            return context.CategoriasFinancieras
                .FirstOrDefault(c => c.Tipo == tipo && c.Nombre.Contains(nombre) && c.Activa);
        }
    }
}