using Kirkenta.Data;
using Kirkenta.Models;
using Microsoft.EntityFrameworkCore;

namespace Kirkenta.Helpers.Logistica
{
    /// <summary>
    /// Lógica de negocio para envíos individuales (una parada / un destino).
    /// </summary>
    public static class EnvioHelper
    {
        /// <summary>
        /// Crea un Envio asociado a una Venta.
        /// El envío nace en estado "Pendiente", sin ruta asignada.
        /// </summary>
        public static Envio CrearParaVenta(
            ApplicationDbContext context,
            Venta venta,
            string direccionEntrega,
            string? referencia,
            string contactoNombre,
            string contactoTelefono,
            string? ciudad,
            int? zonaId,
            decimal monto,
            int? usuarioId)
        {
            var numero = NumeroDocumentoHelper.GenerarSiguiente(context, "Envio");

            string? zonaNombre = null;
            if (zonaId.HasValue)
            {
                zonaNombre = context.ZonasEnvio
                    .FirstOrDefault(z => z.Id == zonaId.Value)?.Nombre;
            }

            var cliente = context.Clientes.FirstOrDefault(c => c.Id == venta.ClienteId);

            var envio = new Envio
            {
                Numero = numero,
                VentaId = venta.Id,
                ClienteId = venta.ClienteId ?? 0,
                ClienteNombre = cliente?.Nombre ?? "Consumidor final",
                DireccionEntrega = direccionEntrega,
                Referencia = referencia,
                ContactoNombre = contactoNombre,
                ContactoTelefono = contactoTelefono,
                Ciudad = ciudad,
                ZonaId = zonaId,
                ZonaNombre = zonaNombre,
                Monto = monto,
                EsGratis = monto <= 0,
                Estado = "Pendiente",
                FechaEntregaEstimada = DateTime.Today.AddDays(1),
                FechaCreacion = DateTime.Now,
                UsuarioCreoId = usuarioId,
                EmpresaId = 1
            };

            context.Envios.Add(envio);
            context.SaveChanges();

            return envio;
        }

        /// <summary>
        /// Crea un Envio asociado a una Cotización.
        /// </summary>
        public static Envio CrearParaCotizacion(
            ApplicationDbContext context,
            Cotizacion cotizacion,
            string direccionEntrega,
            string? referencia,
            string contactoNombre,
            string contactoTelefono,
            string? ciudad,
            int? zonaId,
            decimal monto,
            int? usuarioId)
        {
            var numero = NumeroDocumentoHelper.GenerarSiguiente(context, "Envio");

            string? zonaNombre = null;
            if (zonaId.HasValue)
            {
                zonaNombre = context.ZonasEnvio
                    .FirstOrDefault(z => z.Id == zonaId.Value)?.Nombre;
            }

            var cliente = context.Clientes.FirstOrDefault(c => c.Id == cotizacion.ClienteId);

            var envio = new Envio
            {
                Numero = numero,
                CotizacionId = cotizacion.Id,
                ClienteId = cotizacion.ClienteId,
                ClienteNombre = cliente?.Nombre ?? "—",
                DireccionEntrega = direccionEntrega,
                Referencia = referencia,
                ContactoNombre = contactoNombre,
                ContactoTelefono = contactoTelefono,
                Ciudad = ciudad,
                ZonaId = zonaId,
                ZonaNombre = zonaNombre,
                Monto = monto,
                EsGratis = monto <= 0,
                Estado = "Pendiente",
                FechaEntregaEstimada = DateTime.Today.AddDays(1),
                FechaCreacion = DateTime.Now,
                UsuarioCreoId = usuarioId,
                EmpresaId = 1
            };

            context.Envios.Add(envio);
            context.SaveChanges();

            return envio;
        }

        /// <summary>
        /// Crea un Envio asociado a un Pedido.
        /// </summary>
        public static Envio CrearParaPedido(
            ApplicationDbContext context,
            Pedido pedido,
            string direccionEntrega,
            string? referencia,
            string contactoNombre,
            string contactoTelefono,
            string? ciudad,
            int? zonaId,
            decimal monto,
            int? usuarioId)
        {
            var numero = NumeroDocumentoHelper.GenerarSiguiente(context, "Envio");

            string? zonaNombre = null;
            if (zonaId.HasValue)
            {
                zonaNombre = context.ZonasEnvio
                    .FirstOrDefault(z => z.Id == zonaId.Value)?.Nombre;
            }

            var cliente = context.Clientes.FirstOrDefault(c => c.Id == pedido.ClienteId);

            var envio = new Envio
            {
                Numero = numero,
                PedidoId = pedido.Id,
                ClienteId = pedido.ClienteId,
                ClienteNombre = cliente?.Nombre ?? "—",
                DireccionEntrega = direccionEntrega,
                Referencia = referencia,
                ContactoNombre = contactoNombre,
                ContactoTelefono = contactoTelefono,
                Ciudad = ciudad,
                ZonaId = zonaId,
                ZonaNombre = zonaNombre,
                Monto = monto,
                EsGratis = monto <= 0,
                Estado = "Pendiente",
                FechaEntregaEstimada = pedido.FechaEntrega ?? DateTime.Today.AddDays(1),
                FechaCreacion = DateTime.Now,
                UsuarioCreoId = usuarioId,
                EmpresaId = 1
            };

            context.Envios.Add(envio);
            context.SaveChanges();

            return envio;
        }

        /// <summary>
        /// Crea un Envio asociado a una Factura.
        /// </summary>
        public static Envio CrearParaFactura(
            ApplicationDbContext context,
            Factura factura,
            string direccionEntrega,
            string? referencia,
            string contactoNombre,
            string contactoTelefono,
            string? ciudad,
            int? zonaId,
            decimal monto,
            int? usuarioId)
        {
            var numero = NumeroDocumentoHelper.GenerarSiguiente(context, "Envio");

            string? zonaNombre = null;
            if (zonaId.HasValue)
            {
                zonaNombre = context.ZonasEnvio
                    .FirstOrDefault(z => z.Id == zonaId.Value)?.Nombre;
            }

            var cliente = context.Clientes.FirstOrDefault(c => c.Id == factura.ClienteId);

            var envio = new Envio
            {
                Numero = numero,
                FacturaId = factura.Id,
                ClienteId = factura.ClienteId,
                ClienteNombre = cliente?.Nombre ?? "—",
                DireccionEntrega = direccionEntrega,
                Referencia = referencia,
                ContactoNombre = contactoNombre,
                ContactoTelefono = contactoTelefono,
                Ciudad = ciudad,
                ZonaId = zonaId,
                ZonaNombre = zonaNombre,
                Monto = monto,
                EsGratis = monto <= 0,
                Estado = "Pendiente",
                FechaEntregaEstimada = DateTime.Today.AddDays(1),
                FechaCreacion = DateTime.Now,
                UsuarioCreoId = usuarioId,
                EmpresaId = 1
            };

            context.Envios.Add(envio);
            context.SaveChanges();

            return envio;
        }

        /// <summary>
        /// Marca un envío como entregado.
        /// </summary>
        public static (bool ok, string? error) MarcarEntregado(
            ApplicationDbContext context,
            int envioId,
            string nombreRecibio,
            string? firmaImagen,
            string? fotoEntrega,
            string? notas,
            int? usuarioId)
        {
            try
            {
                var envio = context.Envios.FirstOrDefault(e => e.Id == envioId);
                if (envio == null) return (false, "Envío no encontrado");

                if (envio.Estado == "Entregado")
                    return (false, "Este envío ya fue marcado como entregado");

                var estadoAnterior = envio.Estado;

                envio.Estado = "Entregado";
                envio.FechaEntregaReal = DateTime.Now;
                envio.NombreRecibio = nombreRecibio;
                envio.FirmaImagen = firmaImagen;
                envio.FotoEntrega = fotoEntrega;

                if (!string.IsNullOrWhiteSpace(notas))
                {
                    envio.Notas = (string.IsNullOrWhiteSpace(envio.Notas) ? "" : envio.Notas + "\n") + notas;
                }

                // Registrar en el historial de la ruta
                if (envio.RutaId.HasValue)
                {
                    var usuario = usuarioId.HasValue
                        ? context.Usuarios.FirstOrDefault(u => u.Id == usuarioId.Value)
                        : null;

                    context.RutasHistorial.Add(new RutaHistorial
                    {
                        RutaId = envio.RutaId.Value,
                        EnvioId = envio.Id,
                        Evento = "Entregado",
                        Detalle = $"Parada {envio.OrdenParada} entregada a {nombreRecibio}",
                        EstadoAnterior = estadoAnterior,
                        EstadoNuevo = "Entregado",
                        Fecha = DateTime.Now,
                        UsuarioId = usuarioId,
                        UsuarioNombre = usuario?.Username
                    });

                    // Actualizar contadores de la ruta
                    var ruta = context.Rutas.FirstOrDefault(r => r.Id == envio.RutaId.Value);
                    if (ruta != null)
                    {
                        var todosEnvios = context.Envios
                            .Where(e => e.RutaId == ruta.Id)
                            .ToList();

                        ruta.ParadasEntregadas = todosEnvios.Count(e => e.Estado == "Entregado");
                        ruta.ParadasFallidas = todosEnvios.Count(e => e.Estado == "Fallido");

                        // Si todas están resueltas, cerrar la ruta automáticamente
                        var totalResueltas = ruta.ParadasEntregadas + ruta.ParadasFallidas;
                        if (totalResueltas >= ruta.TotalParadas && ruta.Estado == "EnReparto")
                        {
                            ruta.Estado = "Completada";
                            ruta.FechaRegreso = DateTime.Now;
                        }
                    }
                }

                context.SaveChanges();
                return (true, null);
            }
            catch (Exception ex)
            {
                return (false, ex.Message);
            }
        }

        /// <summary>
        /// Marca un envío como fallido (no se pudo entregar).
        /// </summary>
        public static (bool ok, string? error) MarcarFallido(
            ApplicationDbContext context,
            int envioId,
            string motivoFallo,
            int? usuarioId)
        {
            try
            {
                var envio = context.Envios.FirstOrDefault(e => e.Id == envioId);
                if (envio == null) return (false, "Envío no encontrado");

                if (envio.Estado == "Entregado" || envio.Estado == "Fallido")
                    return (false, "Este envío ya fue procesado");

                var estadoAnterior = envio.Estado;

                envio.Estado = "Fallido";
                envio.FechaEntregaReal = DateTime.Now;
                envio.MotivoFallo = motivoFallo;

                if (envio.RutaId.HasValue)
                {
                    var usuario = usuarioId.HasValue
                        ? context.Usuarios.FirstOrDefault(u => u.Id == usuarioId.Value)
                        : null;

                    context.RutasHistorial.Add(new RutaHistorial
                    {
                        RutaId = envio.RutaId.Value,
                        EnvioId = envio.Id,
                        Evento = "Fallido",
                        Detalle = motivoFallo,
                        EstadoAnterior = estadoAnterior,
                        EstadoNuevo = "Fallido",
                        Fecha = DateTime.Now,
                        UsuarioId = usuarioId,
                        UsuarioNombre = usuario?.Username
                    });

                    var ruta = context.Rutas.FirstOrDefault(r => r.Id == envio.RutaId.Value);
                    if (ruta != null)
                    {
                        var todosEnvios = context.Envios
                            .Where(e => e.RutaId == ruta.Id)
                            .ToList();

                        ruta.ParadasEntregadas = todosEnvios.Count(e => e.Estado == "Entregado");
                        ruta.ParadasFallidas = todosEnvios.Count(e => e.Estado == "Fallido");

                        var totalResueltas = ruta.ParadasEntregadas + ruta.ParadasFallidas;
                        if (totalResueltas >= ruta.TotalParadas && ruta.Estado == "EnReparto")
                        {
                            ruta.Estado = "Completada";
                            ruta.FechaRegreso = DateTime.Now;
                        }
                    }
                }

                context.SaveChanges();
                return (true, null);
            }
            catch (Exception ex)
            {
                return (false, ex.Message);
            }
        }

        /// <summary>
        /// Reagenda un envío fallido (vuelve a estado Pendiente para nueva ruta).
        /// </summary>
        public static (bool ok, string? error) Reagendar(
            ApplicationDbContext context,
            int envioId,
            string? notas,
            int? usuarioId)
        {
            try
            {
                var envio = context.Envios.FirstOrDefault(e => e.Id == envioId);
                if (envio == null) return (false, "Envío no encontrado");

                if (envio.Estado != "Fallido")
                    return (false, "Solo se pueden reagendar envíos fallidos");

                var estadoAnterior = envio.Estado;
                var rutaAnteriorId = envio.RutaId;

                envio.Estado = "Pendiente";
                envio.RutaId = null;
                envio.OrdenParada = null;
                envio.RepartidorId = null;
                envio.RepartidorNombre = null;
                envio.Vehiculo = null;
                envio.FechaSalida = null;
                envio.FechaEntregaReal = null;
                envio.MotivoFallo = null;

                if (!string.IsNullOrWhiteSpace(notas))
                {
                    envio.Notas = (string.IsNullOrWhiteSpace(envio.Notas) ? "" : envio.Notas + "\n") +
                                  $"[Reagendado {DateTime.Now:dd/MM/yyyy HH:mm}] {notas}";
                }

                if (rutaAnteriorId.HasValue)
                {
                    var usuario = usuarioId.HasValue
                        ? context.Usuarios.FirstOrDefault(u => u.Id == usuarioId.Value)
                        : null;

                    context.RutasHistorial.Add(new RutaHistorial
                    {
                        RutaId = rutaAnteriorId.Value,
                        EnvioId = envio.Id,
                        Evento = "Reagendado",
                        Detalle = notas,
                        EstadoAnterior = estadoAnterior,
                        EstadoNuevo = "Pendiente",
                        Fecha = DateTime.Now,
                        UsuarioId = usuarioId,
                        UsuarioNombre = usuario?.Username
                    });
                }

                context.SaveChanges();
                return (true, null);
            }
            catch (Exception ex)
            {
                return (false, ex.Message);
            }
        }

        /// <summary>
        /// Obtiene los envíos pendientes (sin ruta asignada) listos para despachar.
        /// </summary>
        public static List<Envio> ObtenerPendientesSinRuta(ApplicationDbContext context)
        {
            return context.Envios
                .AsNoTracking()
                .Where(e => e.Estado == "Pendiente" && e.RutaId == null)
                .OrderBy(e => e.FechaEntregaEstimada)
                .ThenBy(e => e.Id)
                .ToList();
        }

        /// <summary>
        /// Genera un TrackingCode único tipo KRT-2026-AB12X9.
        /// </summary>
        public static string GenerarTrackingCode()
        {
            const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
            var random = new Random();
            var sufijo = new string(Enumerable.Repeat(chars, 6)
                .Select(s => s[random.Next(s.Length)]).ToArray());

            return $"KRT-{DateTime.Now.Year}-{sufijo}";
        }
    }
}