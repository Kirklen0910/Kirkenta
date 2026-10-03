using Kirkenta.Data;
using Kirkenta.Models;

namespace Kirkenta.Helpers.Finanzas
{
    /// <summary>
    /// Maneja las distribuciones del efectivo contado en un cierre de caja
    /// y genera los movimientos financieros correspondientes.
    /// </summary>
    public static class DistribucionHelper
    {
        /// <summary>
        /// Registra las distribuciones de un cierre y genera los movimientos financieros.
        /// Valida que la suma coincida con el efectivo contado.
        /// </summary>
        public static (bool ok, string? error) RegistrarDistribuciones(
            ApplicationDbContext context,
            int cierreId,
            int cuentaOrigenId,
            decimal efectivoContado,
            List<DistribucionInput> distribuciones,
            int usuarioId)
        {
            try
            {
                // Validar que la suma coincida
                var totalDistribuido = distribuciones.Sum(d => d.Monto);

                if (Math.Abs(totalDistribuido - efectivoContado) > 0.01m)
                {
                    return (false, $"La suma de las distribuciones (L. {totalDistribuido:N2}) no coincide con el efectivo contado (L. {efectivoContado:N2})");
                }

                var monedaDefecto = context.Monedas.FirstOrDefault(m => m.EsPredeterminada)?.Id ?? 1;

                foreach (var dist in distribuciones)
                {
                    if (dist.Monto <= 0) continue;

                    var distribucion = new DistribucionCierre
                    {
                        CierreCajaId = cierreId,
                        Tipo = dist.Tipo,
                        Monto = dist.Monto,
                        CuentaDestinoId = dist.CuentaDestinoId,
                        DestinoDescripcion = dist.DestinoDescripcion,
                        Referencia = dist.Referencia,
                        UsuarioRecibeId = dist.UsuarioRecibeId,
                        NombreRecibe = dist.NombreRecibe,
                        Notas = dist.Notas,
                        Fecha = DateTime.Now
                    };

                    context.DistribucionesCierre.Add(distribucion);
                    context.SaveChanges();

                    // Generar movimiento según tipo
                    MovimientoFinanciero? mov = null;

                    switch (dist.Tipo)
                    {
                        case "RetiroBanco":
                            if (!dist.CuentaDestinoId.HasValue)
                                return (false, "El retiro a banco requiere una cuenta destino");

                            mov = GenerarTransferencia(
                                context, cuentaOrigenId, dist.CuentaDestinoId.Value,
                                dist.Monto, $"Retiro a banco al cierre #{cierreId}",
                                dist.Referencia, cierreId, usuarioId, monedaDefecto);
                            break;

                        case "EntregaAdmin":
                            mov = GenerarEgreso(
                                context, cuentaOrigenId, dist.Monto,
                                $"Entrega a administración al cierre #{cierreId}",
                                dist.Referencia, cierreId, usuarioId, monedaDefecto,
                                categoriaNombre: "Entrega a administración");
                            break;

                        case "PagoDirecto":
                            mov = GenerarEgreso(
                                context, cuentaOrigenId, dist.Monto,
                                $"Pago directo al cierre #{cierreId}",
                                dist.Referencia, cierreId, usuarioId, monedaDefecto,
                                categoriaNombre: "Pago directo");
                            break;

                        case "FondoCaja":
                            // El fondo NO genera movimiento. Solo queda registrado.
                            // La caja queda con ese saldo para la próxima apertura.
                            break;

                        case "Otro":
                            // Según el monto: si sale de la caja, se registra egreso
                            mov = GenerarEgreso(
                                context, cuentaOrigenId, dist.Monto,
                                $"Salida de efectivo al cierre #{cierreId}",
                                dist.Referencia, cierreId, usuarioId, monedaDefecto,
                                categoriaNombre: "Otros");
                            break;
                    }

                    if (mov != null)
                    {
                        distribucion.MovimientoId = mov.Id;
                        context.SaveChanges();
                    }
                }

                // Actualizar el total distribuido en el cierre
                var cierre = context.CierresCaja.FirstOrDefault(c => c.Id == cierreId);
                if (cierre != null)
                {
                    cierre.TotalDistribuido = totalDistribuido;
                    context.SaveChanges();
                }

                return (true, null);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DistribucionHelper] Error: {ex.Message}");
                return (false, ex.Message);
            }
        }

        private static MovimientoFinanciero GenerarTransferencia(
            ApplicationDbContext context,
            int cuentaOrigenId,
            int cuentaDestinoId,
            decimal monto,
            string concepto,
            string? referencia,
            int cierreId,
            int usuarioId,
            int monedaId)
        {
            var numero = NumeroDocumentoHelper.GenerarSiguiente(context, "MovimientoFinanciero");

            var mov = new MovimientoFinanciero
            {
                Numero = numero,
                Tipo = "Transferencia",
                Fecha = DateTime.Now,
                CuentaId = cuentaOrigenId,
                CuentaDestinoId = cuentaDestinoId,
                Monto = monto,
                MonedaId = monedaId,
                TipoCambio = 1,
                Concepto = concepto,
                Referencia = referencia,
                FormaPago = "Transferencia",
                Origen = "CierreCaja",
                OrigenId = cierreId,
                EsAutomatico = true,
                Estado = "Activo",
                FechaCreacion = DateTime.Now,
                UsuarioCreoId = usuarioId,
                EmpresaId = 1
            };

            context.MovimientosFinancieros.Add(mov);
            context.SaveChanges();

            SaldoHelper.Aplicar(context, mov);
            context.SaveChanges();

            return mov;
        }

        private static MovimientoFinanciero GenerarEgreso(
            ApplicationDbContext context,
            int cuentaOrigenId,
            decimal monto,
            string concepto,
            string? referencia,
            int cierreId,
            int usuarioId,
            int monedaId,
            string categoriaNombre)
        {
            var categoria = context.CategoriasFinancieras
                .FirstOrDefault(c => c.Tipo == "Egreso" && c.Nombre.Contains(categoriaNombre));

            if (categoria == null)
            {
                categoria = new CategoriaFinanciera
                {
                    Tipo = "Egreso",
                    Nombre = categoriaNombre,
                    Color = "#f59e0b",
                    EsSistema = true,
                    Activa = true,
                    FechaCreacion = DateTime.Now,
                    EmpresaId = 1
                };
                context.CategoriasFinancieras.Add(categoria);
                context.SaveChanges();
            }

            var numero = NumeroDocumentoHelper.GenerarSiguiente(context, "MovimientoFinanciero");

            var mov = new MovimientoFinanciero
            {
                Numero = numero,
                Tipo = "Egreso",
                Fecha = DateTime.Now,
                CuentaId = cuentaOrigenId,
                CategoriaId = categoria.Id,
                Monto = monto,
                MonedaId = monedaId,
                TipoCambio = 1,
                Concepto = concepto,
                Referencia = referencia,
                FormaPago = "Efectivo",
                Origen = "CierreCaja",
                OrigenId = cierreId,
                EsAutomatico = true,
                Estado = "Activo",
                FechaCreacion = DateTime.Now,
                UsuarioCreoId = usuarioId,
                EmpresaId = 1
            };

            context.MovimientosFinancieros.Add(mov);
            context.SaveChanges();

            SaldoHelper.Aplicar(context, mov);
            context.SaveChanges();

            return mov;
        }

        /// <summary>
        /// Obtiene las distribuciones de un cierre con información expandida.
        /// </summary>
        public static List<DistribucionView> ObtenerDistribuciones(ApplicationDbContext context, int cierreId)
        {
            var cuentasDict = context.CuentasFinancieras
                .ToDictionary(c => c.Id, c => c.Nombre);

            var usuariosDict = context.Usuarios
                .ToDictionary(u => u.Id, u => u.Username);

            return context.DistribucionesCierre
                .Where(d => d.CierreCajaId == cierreId)
                .OrderBy(d => d.Id)
                .ToList()
                .Select(d => new DistribucionView
                {
                    Id = d.Id,
                    Tipo = d.Tipo,
                    Monto = d.Monto,
                    CuentaDestinoNombre = d.CuentaDestinoId.HasValue
                        ? cuentasDict.GetValueOrDefault(d.CuentaDestinoId.Value)
                        : null,
                    DestinoDescripcion = d.DestinoDescripcion,
                    Referencia = d.Referencia,
                    NombreRecibe = d.NombreRecibe ?? (d.UsuarioRecibeId.HasValue
                        ? usuariosDict.GetValueOrDefault(d.UsuarioRecibeId.Value)
                        : null),
                    Notas = d.Notas,
                    MovimientoId = d.MovimientoId
                })
                .ToList();
        }
    }

    /// <summary>
    /// DTO para pasar distribuciones desde la UI al helper.
    /// </summary>
    public class DistribucionInput
    {
        public string Tipo { get; set; } = "FondoCaja";
        public decimal Monto { get; set; }
        public int? CuentaDestinoId { get; set; }
        public string? DestinoDescripcion { get; set; }
        public string? Referencia { get; set; }
        public int? UsuarioRecibeId { get; set; }
        public string? NombreRecibe { get; set; }
        public string? Notas { get; set; }
    }

    public class DistribucionView
    {
        public int Id { get; set; }
        public string Tipo { get; set; } = "";
        public decimal Monto { get; set; }
        public string? CuentaDestinoNombre { get; set; }
        public string? DestinoDescripcion { get; set; }
        public string? Referencia { get; set; }
        public string? NombreRecibe { get; set; }
        public string? Notas { get; set; }
        public int? MovimientoId { get; set; }
    }
}