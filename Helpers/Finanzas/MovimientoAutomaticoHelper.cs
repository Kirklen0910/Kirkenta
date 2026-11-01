using Kirkenta.Data;
using Kirkenta.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Kirkenta.Helpers.Finanzas
{
    public static class MovimientoAutomaticoHelper
    {
        /// <summary>
        /// Registra el ingreso por una venta del POS (contado o tarjeta).
        /// </summary>
        public static (MovimientoFinanciero? mov, string? error) RegistrarIngresoVenta(
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
                CuentaFinanciera? cuenta;
                if (cuentaId.HasValue)
                {
                    cuenta = context.CuentasFinancieras
                        .FirstOrDefault(c => c.Id == cuentaId.Value && c.Activa);
                    if (cuenta == null)
                        return (null, $"La cuenta especificada (ID {cuentaId}) no existe o está inactiva.");
                }
                else
                {
                    cuenta = ResolverCuentaVentas(context);
                    if (cuenta == null)
                        return (null, "No hay una cuenta de caja configurada y activa para ventas del POS.");
                }

                var categoria = ResolverCategoria(context, "Ingreso", "Ventas al contado");
                if (categoria == null)
                    return (null, "No existe la categoría de ingreso 'Ventas al contado'.");

                // Parte pagada con saldo a favor del cliente: es ingreso de la venta pero NO entra dinero
                // a la caja (el dinero ya estaba en la empresa, como deuda con el cliente). Va solo contable.
                var pagoConSaldoFavor = string.Equals(formaPago, Kirkenta.Helpers.SaldoFavorHelper.NombreMetodo, StringComparison.OrdinalIgnoreCase);

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
                    usuarioId: usuarioId,
                    notas: pagoConSaldoFavor ? SaldoHelper.MarcaSoloContable : null
                );
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MovimientoAutomatico] Error al registrar ingreso de venta: {ex.Message}");
                return (null, $"Excepción al registrar ingreso: {ex.Message}");
            }
        }

        /// <summary>
        /// Registra el COBRO de una venta al crédito.
        /// Genera un movimiento de Ingreso REAL en la cuenta de caja/banco.
        /// Se usa cuando se cobra una venta al crédito.
        /// </summary>
        public static (MovimientoFinanciero? mov, string? error) RegistrarIngresoCobro(
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
                CuentaFinanciera? cuenta;
                if (cuentaId.HasValue)
                {
                    cuenta = context.CuentasFinancieras
                        .FirstOrDefault(c => c.Id == cuentaId.Value && c.Activa);
                    if (cuenta == null)
                        return (null, $"La cuenta especificada (ID {cuentaId}) no existe o está inactiva.");
                }
                else
                {
                    cuenta = ResolverCuentaVentas(context);
                    if (cuenta == null)
                        return (null, "No hay una cuenta de caja configurada y activa para cobros.");
                }

                var categoria = ResolverCategoria(context, "Ingreso", "Ventas al crédito");
                if (categoria == null)
                {
                    categoria = new CategoriaFinanciera
                    {
                        Tipo = "Ingreso",
                        Nombre = "Ventas al crédito (cobros)",
                        Descripcion = "Cobros de ventas a crédito",
                        Color = "#06b6d4",
                        EsSistema = true,
                        Activa = true,
                        FechaCreacion = DateTime.Now,
                        EmpresaId = EmpresaActual.Id
                    };
                    context.CategoriasFinancieras.Add(categoria);
                    context.SaveChanges();
                }

                return CrearMovimientoAtomico(
                    context,
                    tipo: "Ingreso",
                    cuentaId: cuenta.Id,
                    categoriaId: categoria.Id,
                    monto: monto,
                    concepto: $"Cobro venta al crédito {numeroVenta}",
                    referencia: numeroVenta,
                    formaPago: formaPago,
                    origen: "Venta",
                    origenId: ventaId,
                    usuarioId: usuarioId
                );
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MovimientoAutomatico] Error al registrar cobro: {ex.Message}");
                return (null, $"Excepción al registrar cobro: {ex.Message}");
            }
        }

        /// <summary>
        /// OPCIÓN B (legacy): Registra una cuenta por cobrar por una venta al crédito.
        /// Crea un movimiento de Ingreso en la cuenta "Cuentas por Cobrar".
        /// NO afecta caja real. Si la usás, al cobrar debés crear un Egreso en esta misma
        /// cuenta para bajar el saldo pendiente.
        /// </summary>
        public static (MovimientoFinanciero? mov, string? error) RegistrarCuentaPorCobrar(
            ApplicationDbContext context,
            int ventaId,
            string numeroVenta,
            decimal monto,
            int usuarioId,
            string nombreCliente)
        {
            try
            {
                var cuentaPorCobrar = context.CuentasFinancieras
                    .FirstOrDefault(c => c.Tipo == "Caja" && c.Subtipo == "CuentasPorCobrar" && c.Activa);

                if (cuentaPorCobrar == null)
                    return (null, "No se encontró una cuenta financiera de tipo 'Cuentas por Cobrar' activa. Ve a Finanzas > Cuentas y crea una.");

                var categoria = ResolverCategoria(context, "Ingreso", "Ventas al crédito");
                if (categoria == null)
                    return (null, "No existe la categoría de ingreso 'Ventas al crédito (cobros)'.");

                return CrearMovimientoAtomico(
                    context,
                    tipo: "Ingreso",
                    cuentaId: cuentaPorCobrar.Id,
                    categoriaId: categoria.Id,
                    monto: monto,
                    concepto: $"Venta a crédito {numeroVenta} - Cliente: {nombreCliente}",
                    referencia: numeroVenta,
                    formaPago: "Crédito",
                    origen: "Venta",
                    origenId: ventaId,
                    usuarioId: usuarioId
                );
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MovimientoAutomatico] Error al registrar cuenta por cobrar: {ex.Message}");
                return (null, $"Excepción al registrar cuenta por cobrar: {ex.Message}");
            }
        }

        /// <summary>
        /// OPCIÓN B (legacy): Registra un Egreso en la cuenta "Cuentas por Cobrar" para
        /// "bajar" el saldo cuando se cobra parcial o totalmente una venta al crédito.
        /// Solo se usa si usaste RegistrarCuentaPorCobrar en la venta.
        /// </summary>
        public static (MovimientoFinanciero? mov, string? error) RegistrarEgresoCxC(
            ApplicationDbContext context,
            int ventaId,
            string numeroVenta,
            decimal monto,
            int usuarioId,
            string nombreCliente)
        {
            try
            {
                var cuentaPorCobrar = context.CuentasFinancieras
                    .FirstOrDefault(c => c.Tipo == "Caja" && c.Subtipo == "CuentasPorCobrar" && c.Activa);

                if (cuentaPorCobrar == null)
                    return (null, "No se encontró una cuenta financiera de tipo 'Cuentas por Cobrar' activa.");

                var categoria = ResolverCategoria(context, "Egreso", "Ventas al crédito");
                if (categoria == null)
                {
                    categoria = new CategoriaFinanciera
                    {
                        Tipo = "Egreso",
                        Nombre = "Ventas al crédito (cobros)",
                        Descripcion = "Egreso de CxC por cobro",
                        Color = "#06b6d4",
                        EsSistema = true,
                        Activa = true,
                        FechaCreacion = DateTime.Now,
                        EmpresaId = EmpresaActual.Id
                    };
                    context.CategoriasFinancieras.Add(categoria);
                    context.SaveChanges();
                }

                return CrearMovimientoAtomico(
                    context,
                    tipo: "Egreso",
                    cuentaId: cuentaPorCobrar.Id,
                    categoriaId: categoria.Id,
                    monto: monto,
                    concepto: $"Cobro venta al crédito {numeroVenta} - Cliente: {nombreCliente}",
                    referencia: numeroVenta,
                    formaPago: "Crédito",
                    origen: "Venta",
                    origenId: ventaId,
                    usuarioId: usuarioId
                );
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MovimientoAutomatico] Error al registrar egreso CxC: {ex.Message}");
                return (null, $"Excepción al registrar egreso CxC: {ex.Message}");
            }
        }

        public static (MovimientoFinanciero? mov, string? error) RegistrarEgresoCambio(
            ApplicationDbContext context,
            int documentoId,
            string numeroDocumento,
            decimal montoCambio,
            int usuarioId,
            string? formaPago = null,
            int? cuentaId = null,
            string origen = "Venta")
        {
            try
            {
                if (montoCambio <= 0)
                    return (null, "El monto del cambio debe ser mayor a cero.");

                CuentaFinanciera? cuenta;
                if (cuentaId.HasValue)
                {
                    cuenta = context.CuentasFinancieras
                        .FirstOrDefault(c => c.Id == cuentaId.Value && c.Activa);
                    if (cuenta == null)
                        return (null, $"La cuenta especificada (ID {cuentaId}) no existe o está inactiva.");
                }
                else
                {
                    cuenta = ResolverCuentaVentas(context);
                    if (cuenta == null)
                        return (null, "No hay una cuenta de caja configurada y activa para el cambio de venta.");
                }

                // Enlazada a 4.1.01: el cambio reduce las ventas (el ingreso entra por lo recibido)
                var categoria = AsegurarCategoria(context, "Egreso", "Cambio entregado", "4.1.01", "#f59e0b");

                return CrearMovimientoAtomico(
                    context,
                    tipo: "Egreso",
                    cuentaId: cuenta.Id,
                    categoriaId: categoria.Id,
                    monto: montoCambio,
                    concepto: $"Cambio entregado — {origen} {numeroDocumento}",
                    referencia: numeroDocumento,
                    formaPago: formaPago ?? "Efectivo",
                    origen: origen,
                    origenId: documentoId,
                    usuarioId: usuarioId
                );
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MovimientoAutomatico] Error al registrar egreso de cambio: {ex.Message}");
                return (null, $"Excepción al registrar cambio: {ex.Message}");
            }
        }

        public static (MovimientoFinanciero? mov, string? error) RegistrarEgresoPagoProveedor(
            ApplicationDbContext context,
            int pagoProveedorId,
            string numeroPago,
            decimal monto,
            int usuarioId,
            string nombreProveedor = "—",
            string? formaPago = null,
            string? tipoMetodo = null)
        {
            try
            {
                // Pago en efectivo: sale de la caja; transferencia/cheque/tarjeta: sale del banco
                var pagoEfectivo = !string.IsNullOrWhiteSpace(tipoMetodo)
                                   && tipoMetodo.Contains("efectivo", StringComparison.OrdinalIgnoreCase);
                CuentaFinanciera? cuenta = null;
                if (pagoEfectivo)
                {
                    var cajaId = ResolverCuentaCajaActiva(context);
                    if (cajaId.HasValue)
                        cuenta = context.CuentasFinancieras.FirstOrDefault(c => c.Id == cajaId.Value && c.Activa);
                }
                cuenta ??= ResolverCuentaPagosProveedor(context);
                if (cuenta == null)
                    return (null, "No hay una cuenta bancaria ni caja general activa para pagos a proveedores.");

                var categoria = ResolverCategoria(context, "Egreso", "Pago a proveedores");
                if (categoria == null)
                    return (null, "No existe la categoría de egreso 'Pago a proveedores'.");

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
                return (null, $"Excepción al registrar pago a proveedor: {ex.Message}");
            }
        }

        public static (MovimientoFinanciero? mov, string? error) RegistrarEgresoNomina(
            ApplicationDbContext context,
            int nominaId,
            string numeroNomina,
            decimal monto,
            int usuarioId)
        {
            try
            {
                var cuenta = ResolverCuentaDefault(context);
                if (cuenta == null)
                    return (null, "No hay ninguna cuenta activa configurada para el pago de nómina.");

                var categoria = ResolverCategoria(context, "Egreso", "Nómina");
                if (categoria == null)
                    return (null, "No existe la categoría de egreso 'Nómina'.");

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
                return (null, $"Excepción al registrar nómina: {ex.Message}");
            }
        }

        // ============================================================
        // COSTO DE VENTAS (COGS)
        // ============================================================

        /// <summary>
        /// Registra el egreso por Costo de Mercadería Vendida (COGS).
        ///
        /// Este movimiento es CONTABLE, no de caja. Su propósito es que el
        /// Estado de Resultados reste el costo real del producto vendido y
        /// calcule la Utilidad Bruta correctamente.
        ///
        /// FIX 23/10/2026: ya NO modifica SaldoActual de la cuenta (SaldoHelper
        /// ignora los movimientos de la cuenta 5.1.01). Antes cada venta
        /// descontaba el costo del efectivo real de la caja.
        /// Es idempotente: si la venta ya tiene su costo, devuelve el existente.
        ///
        /// Va asociado a la categoría "Costo de mercadería vendida", que
        /// debe estar enlazada al plan de cuentas 5.1.01.
        ///
        /// El movimiento se aplica contra la misma cuenta de caja/banco que
        /// recibió el ingreso, porque en el modelo actual no se maneja una
        /// cuenta puente de Inventario: la "contrapartida" es la misma cuenta
        /// financiera. Lo importante para el Estado de Resultados es que el
        /// egreso quede con Tipo = "Egreso" y con una categoría cuyo plan
        /// sea de tipo Costo.
        /// </summary>
        public static (MovimientoFinanciero? mov, string? error) RegistrarEgresoCostoVenta(
            ApplicationDbContext context,
            int ventaId,
            string numeroVenta,
            decimal montoCosto,
            int usuarioId,
            int? cuentaId = null)
        {
            try
            {
                if (montoCosto <= 0)
                    return (null, "El costo debe ser mayor a cero");

                // Idempotencia: no duplicar el costo de una misma venta
                var existente = context.MovimientosFinancieros.FirstOrDefault(m =>
                    m.Estado == "Activo"
                    && m.Origen == "Venta"
                    && m.OrigenId == ventaId
                    && m.Concepto.StartsWith("Costo de mercadería vendida"));
                if (existente != null) return (existente, null);

                CuentaFinanciera? cuenta;
                if (cuentaId.HasValue)
                {
                    cuenta = context.CuentasFinancieras
                        .FirstOrDefault(c => c.Id == cuentaId.Value && c.Activa);
                    if (cuenta == null)
                        return (null, $"La cuenta especificada (ID {cuentaId}) no existe o está inactiva.");
                }
                else
                {
                    cuenta = ResolverCuentaVentas(context);
                    if (cuenta == null)
                        return (null, "No hay una cuenta de caja configurada y activa para el costo de venta.");
                }

                // Buscar la categoría de COGS explícita
                var categoria = context.CategoriasFinancieras
                    .FirstOrDefault(c => c.Tipo == "Egreso"
                                      && c.Nombre == "Costo de mercadería vendida"
                                      && c.Activa);

                if (categoria == null)
                {
                    // Fallback: cualquier categoría de egreso con PlanCuentaId a 5.1.01
                    categoria = context.CategoriasFinancieras
                        .FirstOrDefault(c => c.Tipo == "Egreso"
                                          && c.PlanCuentaId != null
                                          && c.Activa
                                          && context.PlanCuentas
                                             .Any(p => p.Id == c.PlanCuentaId && p.Codigo == "5.1.01"));
                }

                if (categoria == null)
                    return (null, "No existe la categoría de egreso 'Costo de mercadería vendida'.");

                return CrearMovimientoAtomico(
                    context,
                    tipo: "Egreso",
                    cuentaId: cuenta.Id,
                    categoriaId: categoria.Id,
                    monto: montoCosto,
                    concepto: $"Costo de mercadería vendida — Venta {numeroVenta}",
                    referencia: numeroVenta,
                    formaPago: "N/A",
                    origen: "Venta",
                    origenId: ventaId,
                    usuarioId: usuarioId,
                    notas: SaldoHelper.MarcaSoloContable
                );
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MovimientoAutomatico] Error al registrar COGS: {ex.Message}");
                return (null, $"Excepción al registrar costo de venta: {ex.Message}");
            }
        }

        /// <summary>
        /// Crea el movimiento + aplica saldo + guarda todo dentro de una transacción atómica.
        /// </summary>
        private static (MovimientoFinanciero? mov, string? error) CrearMovimientoAtomico(
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
            int usuarioId,
            string? notas = null)
        {
            // Ningún movimiento puede tener monto negativo, salvo la reversa de costo (solo contable)
            if (monto < 0 && (notas == null || !notas.Contains(SaldoHelper.MarcaSoloContable)))
                return (null, "El monto del movimiento no puede ser negativo.");

            var currentTx = context.Database.CurrentTransaction;
            var abrimosNosotros = currentTx == null;

            IDbContextTransaction? transaction = null;
            if (abrimosNosotros)
            {
                transaction = context.Database.BeginTransaction();
            }

            MovimientoFinanciero? movCreado = null;
            try
            {
                var cuenta = context.CuentasFinancieras.FirstOrDefault(c => c.Id == cuentaId);
                if (cuenta == null)
                {
                    if (abrimosNosotros) transaction!.Rollback();
                    return (null, $"La cuenta (ID {cuentaId}) no existe.");
                }

                if (!cuenta.Activa)
                {
                    if (abrimosNosotros) transaction!.Rollback();
                    return (null, $"La cuenta '{cuenta.Nombre}' está inactiva.");
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
                    Notas = notas,
                    Estado = "Activo",
                    FechaCreacion = DateTime.Now,
                    UsuarioCreoId = usuarioId,
                    EmpresaId = EmpresaActual.Id
                };

                movCreado = mov;
                context.MovimientosFinancieros.Add(mov);
                context.SaveChanges();

                SaldoHelper.Aplicar(context, mov);
                context.SaveChanges();

                if (abrimosNosotros)
                {
                    transaction!.Commit();
                }

                return (mov, null);
            }
            catch (Exception ex)
            {
                if (abrimosNosotros)
                {
                    transaction!.Rollback();
                }
                // El movimiento no quedó guardado: se saca del contexto para que un SaveChanges posterior
                // del llamador (que sigue dentro de su propia transacción) no lo intente guardar a medias.
                if (movCreado != null && context.Entry(movCreado).State != EntityState.Detached)
                    context.Entry(movCreado).State = EntityState.Detached;
                Console.WriteLine($"[MovimientoAutomatico] Error en transacción, rollback aplicado: {ex.Message}");
                return (null, $"Error en transacción: {ex.Message}");
            }
            finally
            {
                if (abrimosNosotros && transaction != null)
                {
                    transaction.Dispose();
                }
            }
        }

        // ============================================================
        // DEVOLUCIONES DE CLIENTES
        // ============================================================

        /// <summary>
        /// Costo de mercadería vendida (COGS) de una factura que NO viene de una venta
        /// (factura de cotización o de pedido). Calcula el costo con el precio de compra actual
        /// de cada producto, igual que el kardex. Movimiento CONTABLE (no mueve caja) e idempotente.
        /// Si el costo es cero no registra nada (devuelve mov = null y error = null).
        /// </summary>
        public static (MovimientoFinanciero? mov, string? error) RegistrarCostoFactura(
            ApplicationDbContext context, Factura factura, int usuarioId)
        {
            try
            {
                var existente = context.MovimientosFinancieros.FirstOrDefault(m =>
                    m.Estado == "Activo" && m.Origen == "Factura" && m.OrigenId == factura.Id
                    && m.Concepto.StartsWith("Costo de mercadería vendida"));
                if (existente != null) return (existente, null);

                var lineas = context.DetalleFacturas.Where(d => d.FacturaId == factura.Id).ToList();
                var idsProd = lineas.Select(l => l.ProductoId).Distinct().ToList();
                var costos = context.Productos
                    .Where(p => idsProd.Contains(p.Id))
                    .ToDictionary(p => p.Id, p => p.PrecioCompra);
                var costoTotal = lineas.Sum(l => l.Cantidad * (costos.TryGetValue(l.ProductoId, out var c) ? c : 0m));
                if (costoTotal <= 0) return (null, null);

                var cuentaId = ResolverCuentaCajaActiva(context);
                if (cuentaId == null)
                    return (null, "No hay una cuenta activa para registrar el costo de ventas.");

                var categoria = context.CategoriasFinancieras
                    .FirstOrDefault(c => c.Tipo == "Egreso" && c.Nombre == "Costo de mercadería vendida" && c.Activa)
                    ?? AsegurarCategoria(context, "Egreso", "Costo de mercadería vendida", "5.1.01", "#b91c1c");

                return CrearMovimientoAtomico(
                    context,
                    tipo: "Egreso",
                    cuentaId: cuentaId.Value,
                    categoriaId: categoria.Id,
                    monto: costoTotal,
                    concepto: $"Costo de mercadería vendida — Factura {factura.Numero}",
                    referencia: factura.Numero,
                    formaPago: "N/A",
                    origen: "Factura",
                    origenId: factura.Id,
                    usuarioId: usuarioId,
                    notas: SaldoHelper.MarcaSoloContable
                );
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MovimientoAutomatico] Error al registrar COGS de factura: {ex.Message}");
                return (null, $"Excepción al registrar costo de venta: {ex.Message}");
            }
        }

        /// <summary>
        /// Ingreso REAL por el pago de una factura que NO viene de una venta (ej. factura de cotización
        /// o de pedido). Efectivo entra a la caja activa; tarjeta/transferencia/cheque a la primera
        /// cuenta bancaria activa (si no hay banco, a la caja).
        /// </summary>
        public static (MovimientoFinanciero? mov, string? error) RegistrarIngresoPagoFactura(
            ApplicationDbContext context,
            int facturaId,
            string numeroFactura,
            decimal monto,
            int usuarioId,
            string? nombreMetodo,
            string? tipoMetodo)
        {
            try
            {
                if (monto <= 0) return (null, "El monto del pago debe ser mayor a cero.");

                int? cuentaId = null;
                var esEfectivo = string.IsNullOrWhiteSpace(tipoMetodo)
                                 || tipoMetodo.Equals("Efectivo", StringComparison.OrdinalIgnoreCase);
                if (!esEfectivo)
                {
                    cuentaId = context.CuentasFinancieras
                        .Where(c => c.Tipo == "Banco" && c.Activa)
                        .OrderBy(c => c.Id)
                        .Select(c => (int?)c.Id)
                        .FirstOrDefault();
                }
                cuentaId ??= ResolverCuentaCajaActiva(context);
                if (cuentaId == null)
                    return (null, "No hay una cuenta de caja o banco activa para registrar el ingreso.");

                var categoria = AsegurarCategoria(context, "Ingreso", "Cobros de facturas", "4.1.01", "#0ea5e9");

                return CrearMovimientoAtomico(
                    context,
                    tipo: "Ingreso",
                    cuentaId: cuentaId.Value,
                    categoriaId: categoria.Id,
                    monto: monto,
                    concepto: $"Pago de factura {numeroFactura}",
                    referencia: numeroFactura,
                    formaPago: nombreMetodo,
                    origen: "Factura",
                    origenId: facturaId,
                    usuarioId: usuarioId
                );
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MovimientoAutomatico] Error al registrar pago de factura: {ex.Message}");
                return (null, $"Excepción al registrar el ingreso: {ex.Message}");
            }
        }

        /// <summary>
        /// Cuenta de caja a usar para movimientos de caja: la de la apertura activa,
        /// o la primera caja activa.
        /// </summary>
        public static int? ResolverCuentaCajaActiva(ApplicationDbContext context)
        {
            var apertura = context.AperturasCaja.FirstOrDefault(a => a.Activa);
            if (apertura != null) return apertura.CuentaId;
            return ResolverCuentaVentas(context)?.Id;
        }

        /// <summary>
        /// Egreso REAL por el reembolso de una devolución de cliente.
        /// Categoría "Devoluciones en ventas" enlazada al plan 4.1.03 (reduce las ventas netas).
        /// </summary>
        public static (MovimientoFinanciero? mov, string? error) RegistrarEgresoDevolucion(
            ApplicationDbContext context,
            int devolucionId,
            string numeroDevolucion,
            decimal monto,
            int usuarioId,
            string? formaPago = null,
            int? cuentaId = null,
            bool soloContable = false)
        {
            try
            {
                if (monto <= 0) return (null, "El monto del reembolso debe ser mayor a cero.");

                var cuenta = cuentaId.HasValue
                    ? context.CuentasFinancieras.FirstOrDefault(c => c.Id == cuentaId.Value && c.Activa)
                    : ResolverCuentaVentas(context);
                if (cuenta == null)
                    return (null, "No hay una cuenta de caja activa para registrar el reembolso.");

                var categoria = AsegurarCategoria(context, "Egreso", "Devoluciones en ventas", "4.1.03", "#dc2626");

                return CrearMovimientoAtomico(
                    context,
                    tipo: "Egreso",
                    cuentaId: cuenta.Id,
                    categoriaId: categoria.Id,
                    monto: monto,
                    concepto: soloContable
                        ? $"Devolución {numeroDevolucion} acreditada a saldo a favor del cliente"
                        : $"Reembolso devolución {numeroDevolucion}",
                    referencia: numeroDevolucion,
                    formaPago: formaPago,
                    origen: "Devolucion",
                    origenId: devolucionId,
                    usuarioId: usuarioId,
                    // Saldo a favor: no sale dinero de la caja; baja la venta y nace una deuda con el cliente
                    notas: soloContable ? SaldoHelper.MarcaSoloContable : null
                );
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MovimientoAutomatico] Error al registrar reembolso: {ex.Message}");
                return (null, $"Excepción al registrar reembolso: {ex.Message}");
            }
        }

        /// <summary>
        /// Reversa del costo de ventas (COGS) cuando la mercadería devuelta regresa al inventario.
        /// Es un movimiento CONTABLE (no toca caja): egreso de monto NEGATIVO en la categoría
        /// "Costo de mercadería vendida" (5.1.01), de modo que el costo del período baja.
        /// </summary>
        public static (MovimientoFinanciero? mov, string? error) RegistrarReversionCostoVenta(
            ApplicationDbContext context,
            int devolucionId,
            string numeroDevolucion,
            decimal montoCosto,
            int usuarioId,
            int? cuentaId = null)
        {
            try
            {
                if (montoCosto <= 0) return (null, "El costo a revertir debe ser mayor a cero.");

                var cuenta = cuentaId.HasValue
                    ? context.CuentasFinancieras.FirstOrDefault(c => c.Id == cuentaId.Value && c.Activa)
                    : ResolverCuentaVentas(context);
                if (cuenta == null)
                    return (null, "No hay una cuenta activa para registrar la reversa de costo.");

                var categoria = context.CategoriasFinancieras
                    .FirstOrDefault(c => c.Tipo == "Egreso" && c.Nombre == "Costo de mercadería vendida" && c.Activa)
                    ?? AsegurarCategoria(context, "Egreso", "Costo de mercadería vendida", "5.1.01", "#b91c1c");

                return CrearMovimientoAtomico(
                    context,
                    tipo: "Egreso",
                    cuentaId: cuenta.Id,
                    categoriaId: categoria.Id,
                    monto: -montoCosto,
                    concepto: $"Reversión de costo de mercadería vendida — Devolución {numeroDevolucion}",
                    referencia: numeroDevolucion,
                    formaPago: "N/A",
                    origen: "Devolucion",
                    origenId: devolucionId,
                    usuarioId: usuarioId,
                    notas: SaldoHelper.MarcaSoloContable
                );
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MovimientoAutomatico] Error al revertir costo: {ex.Message}");
                return (null, $"Excepción al revertir costo de venta: {ex.Message}");
            }
        }

        /// <summary>Busca la categoría por nombre o la crea enlazada al plan de cuentas indicado.</summary>
        private static CategoriaFinanciera AsegurarCategoria(
            ApplicationDbContext context, string tipo, string nombre, string codigoPlan, string color)
        {
            var cat = context.CategoriasFinancieras.FirstOrDefault(c => c.Tipo == tipo && c.Nombre == nombre);
            var plan = context.PlanCuentas.FirstOrDefault(p => p.Codigo == codigoPlan);

            if (cat == null)
            {
                cat = new CategoriaFinanciera
                {
                    Tipo = tipo,
                    Nombre = nombre,
                    Color = color,
                    EsSistema = true,
                    Activa = true,
                    FechaCreacion = DateTime.Now,
                    EmpresaId = EmpresaActual.Id,
                    PlanCuentaId = plan?.Id,
                    CuentaContable = plan?.Codigo
                };
                context.CategoriasFinancieras.Add(cat);
                context.SaveChanges();
            }
            else if (cat.PlanCuentaId == null && plan != null)
            {
                cat.PlanCuentaId = plan.Id;
                cat.CuentaContable = plan.Codigo;
                context.SaveChanges();
            }

            return cat;
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