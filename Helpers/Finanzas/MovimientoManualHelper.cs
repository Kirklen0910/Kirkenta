using Kirkenta.Data;
using Kirkenta.Models;

namespace Kirkenta.Helpers.Finanzas
{
    /// <summary>
    /// Crea movimientos financieros desde módulos nuevos (Activos, Caja chica, Mantenimiento).
    /// No abre transacción ni es atómico por sí solo: el llamador debe envolverlo en
    /// context.Database.BeginTransaction() para que todo se guarde o todo se revierta.
    /// </summary>
    public static class MovimientoManualHelper
    {
        /// <summary>Busca (o crea) una categoría financiera enlazada a una cuenta del plan.</summary>
        public static CategoriaFinanciera ObtenerOCrearCategoria(
            ApplicationDbContext context, string tipo, string nombre, string? codigoPlan, string color = "#6b7280")
        {
            var cat = context.CategoriasFinancieras.FirstOrDefault(c => c.Tipo == tipo && c.Nombre == nombre);
            if (cat != null) return cat;

            int? planId = null;
            if (!string.IsNullOrEmpty(codigoPlan))
                planId = context.PlanCuentas.Where(p => p.Codigo == codigoPlan).Select(p => (int?)p.Id).FirstOrDefault();

            cat = new CategoriaFinanciera
            {
                Tipo = tipo,
                Nombre = nombre,
                Color = color,
                PlanCuentaId = planId,
                CuentaContable = codigoPlan,
                EsSistema = true,
                Activa = true
            };
            context.CategoriasFinancieras.Add(cat);
            context.SaveChanges();
            return cat;
        }

        public static (MovimientoFinanciero? mov, string? error) Crear(
            ApplicationDbContext context,
            string tipo,
            DateTime fecha,
            int cuentaId,
            int? cuentaDestinoId,
            int? categoriaId,
            decimal monto,
            string concepto,
            string? referencia,
            string origen,
            int? origenId,
            int? usuarioId,
            bool soloContable = false,
            string? notas = null)
        {
            if (monto <= 0) return (null, "El monto debe ser mayor a 0.");

            var (periodoOk, periodoError) = CierreContableHelper.ValidarFecha(context, fecha);
            if (!periodoOk) return (null, periodoError);

            var cuenta = context.CuentasFinancieras.FirstOrDefault(c => c.Id == cuentaId);
            if (cuenta == null) return (null, "La cuenta financiera no existe.");
            if (!cuenta.Activa) return (null, $"La cuenta '{cuenta.Nombre}' está inactiva.");

            if (tipo == "Transferencia")
            {
                if (!cuentaDestinoId.HasValue || cuentaDestinoId == cuentaId)
                    return (null, "La cuenta destino debe ser distinta a la de origen.");
                var destino = context.CuentasFinancieras.FirstOrDefault(c => c.Id == cuentaDestinoId.Value);
                if (destino == null || !destino.Activa) return (null, "La cuenta destino no existe o está inactiva.");
            }
            else if (!categoriaId.HasValue)
            {
                return (null, "Falta la categoría del movimiento.");
            }

            var notasFinal = notas;
            if (soloContable)
                notasFinal = string.IsNullOrWhiteSpace(notas) ? SaldoHelper.MarcaSoloContable : $"{notas} {SaldoHelper.MarcaSoloContable}";

            var mov = new MovimientoFinanciero
            {
                Numero = NumeroDocumentoHelper.GenerarSiguiente(context, "MovimientoFinanciero"),
                Tipo = tipo,
                Fecha = fecha,
                CuentaId = cuentaId,
                CuentaDestinoId = tipo == "Transferencia" ? cuentaDestinoId : null,
                CategoriaId = tipo == "Transferencia" ? null : categoriaId,
                Monto = monto,
                MonedaId = context.Monedas.FirstOrDefault(m => m.EsPredeterminada)?.Id ?? 1,
                TipoCambio = 1,
                Concepto = concepto,
                Referencia = referencia,
                FormaPago = cuenta.Tipo == "Caja" ? "Efectivo" : "Transferencia",
                Origen = origen,
                OrigenId = origenId,
                EsAutomatico = true,
                Notas = notasFinal,
                Estado = "Activo",
                FechaCreacion = DateTime.Now,
                UsuarioCreoId = usuarioId,
                EmpresaId = EmpresaActual.Id
            };

            context.MovimientosFinancieros.Add(mov);
            context.SaveChanges();

            SaldoHelper.Aplicar(context, mov);
            context.SaveChanges();
            return (mov, null);
        }
    }
}
