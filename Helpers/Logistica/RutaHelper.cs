using Kirkenta.Data;
using Kirkenta.Models;
using Microsoft.EntityFrameworkCore;

namespace Kirkenta.Helpers.Logistica
{
    /// <summary>
    /// Lógica de negocio para Rutas de despacho.
    /// </summary>
    public static class RutaHelper
    {
        /// <summary>
        /// Crea una ruta nueva en estado "Borrador" y le asigna los envíos seleccionados.
        /// </summary>
        public static (Ruta? ruta, string? error) Crear(
            ApplicationDbContext context,
            List<int> envioIds,
            int? repartidorId,
            string? repartidorNombre,
            string? vehiculo,
            string? placa,
            int? zonaId,
            string? descripcion,
            DateTime? fechaEntregaEstimada,
            string? notas,
            int? usuarioId)
        {
            try
            {
                if (envioIds == null || envioIds.Count == 0)
                    return (null, "Debes seleccionar al menos un envío");

                // Verificar que todos los envíos existan y estén disponibles
                var envios = context.Envios
                    .Where(e => envioIds.Contains(e.Id))
                    .ToList();

                if (envios.Count != envioIds.Count)
                    return (null, "Algunos envíos no existen");

                var conRuta = envios.Where(e => e.RutaId != null).ToList();
                if (conRuta.Count > 0)
                    return (null, $"Los siguientes envíos ya tienen ruta asignada: {string.Join(", ", conRuta.Select(e => e.Numero))}");

                var noDisponibles = envios.Where(e => e.Estado != "Pendiente").ToList();
                if (noDisponibles.Count > 0)
                    return (null, $"Los siguientes envíos no están en estado Pendiente: {string.Join(", ", noDisponibles.Select(e => e.Numero))}");

                // Si hay repartidor, cargar su info
                Repartidor? repartidor = null;
                if (repartidorId.HasValue)
                {
                    repartidor = context.Repartidores.FirstOrDefault(r => r.Id == repartidorId.Value);
                    if (repartidor != null)
                    {
                        repartidorNombre = repartidor.Nombre;
                        vehiculo ??= repartidor.Vehiculo;
                        placa ??= repartidor.Placa;
                    }
                }

                string? zonaNombre = null;
                if (zonaId.HasValue)
                {
                    zonaNombre = context.ZonasEnvio
                        .FirstOrDefault(z => z.Id == zonaId.Value)?.Nombre;
                }

                var numero = NumeroDocumentoHelper.GenerarSiguiente(context, "Ruta");
                var trackingCode = EnvioHelper.GenerarTrackingCode();

                // Asegurar tracking único
                while (context.Rutas.Any(r => r.TrackingCode == trackingCode))
                {
                    trackingCode = EnvioHelper.GenerarTrackingCode();
                }

                var ruta = new Ruta
                {
                    Numero = numero,
                    Fecha = DateTime.Today,
                    RepartidorId = repartidorId,
                    RepartidorNombre = repartidorNombre ?? "",
                    Vehiculo = vehiculo,
                    Placa = placa,
                    Estado = "Borrador",
                    ZonaId = zonaId,
                    ZonaNombre = zonaNombre,
                    Descripcion = descripcion,
                    TotalParadas = envios.Count,
                    MontoTotalEnvios = envios.Sum(e => e.Monto),
                    TrackingCode = trackingCode,
                    Notas = notas,
                    FechaCreacion = DateTime.Now,
                    UsuarioCreoId = usuarioId,
                    EmpresaId = 1
                };

                context.Rutas.Add(ruta);
                context.SaveChanges();

                // Asignar los envíos a la ruta
                int orden = 1;
                foreach (var envio in envios)
                {
                    envio.RutaId = ruta.Id;
                    envio.OrdenParada = orden;
                    envio.FechaEntregaEstimada = fechaEntregaEstimada ?? envio.FechaEntregaEstimada;

                    orden++;
                }

                context.SaveChanges();

                // Registrar en historial
                var usuario = usuarioId.HasValue
                    ? context.Usuarios.FirstOrDefault(u => u.Id == usuarioId.Value)
                    : null;

                context.RutasHistorial.Add(new RutaHistorial
                {
                    RutaId = ruta.Id,
                    Evento = "RutaCreada",
                    Detalle = $"Ruta creada con {envios.Count} parada(s)",
                    EstadoNuevo = "Borrador",
                    Fecha = DateTime.Now,
                    UsuarioId = usuarioId,
                    UsuarioNombre = usuario?.Username
                });

                context.SaveChanges();

                return (ruta, null);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[RutaHelper] Error al crear: {ex.Message}");
                return (null, ex.Message);
            }
        }

        /// <summary>
        /// Despacha una ruta: cambia estado a "EnReparto" y marca todos sus envíos como "EnRuta".
        /// </summary>
        public static (bool ok, string? error) Despachar(
            ApplicationDbContext context,
            int rutaId,
            int? usuarioId)
        {
            try
            {
                var ruta = context.Rutas.FirstOrDefault(r => r.Id == rutaId);
                if (ruta == null) return (false, "Ruta no encontrada");

                if (ruta.Estado != "Borrador" && ruta.Estado != "Despachada")
                    return (false, $"No se puede despachar una ruta en estado '{ruta.Estado}'");

                if (string.IsNullOrWhiteSpace(ruta.RepartidorNombre))
                    return (false, "Debes asignar un repartidor antes de despachar");

                var envios = context.Envios.Where(e => e.RutaId == rutaId).ToList();
                if (envios.Count == 0)
                    return (false, "Esta ruta no tiene paradas asignadas");

                var estadoAnterior = ruta.Estado;

                ruta.Estado = "EnReparto";
                ruta.FechaSalida = DateTime.Now;

                foreach (var envio in envios)
                {
                    envio.Estado = "EnRuta";
                    envio.FechaSalida = DateTime.Now;
                    envio.RepartidorId = ruta.RepartidorId;
                    envio.RepartidorNombre = ruta.RepartidorNombre;
                    envio.Vehiculo = ruta.Vehiculo;
                }

                var usuario = usuarioId.HasValue
                    ? context.Usuarios.FirstOrDefault(u => u.Id == usuarioId.Value)
                    : null;

                context.RutasHistorial.Add(new RutaHistorial
                {
                    RutaId = ruta.Id,
                    Evento = "Despachada",
                    Detalle = $"Ruta despachada con {envios.Count} parada(s) a cargo de {ruta.RepartidorNombre}",
                    EstadoAnterior = estadoAnterior,
                    EstadoNuevo = "EnReparto",
                    Fecha = DateTime.Now,
                    UsuarioId = usuarioId,
                    UsuarioNombre = usuario?.Username
                });

                context.SaveChanges();
                return (true, null);
            }
            catch (Exception ex)
            {
                return (false, ex.Message);
            }
        }

        /// <summary>
        /// Cierra manualmente una ruta (aunque queden paradas pendientes).
        /// </summary>
        public static (bool ok, string? error) Cerrar(
            ApplicationDbContext context,
            int rutaId,
            string? notas,
            int? usuarioId)
        {
            try
            {
                var ruta = context.Rutas.FirstOrDefault(r => r.Id == rutaId);
                if (ruta == null) return (false, "Ruta no encontrada");

                if (ruta.Estado == "Completada")
                    return (false, "Esta ruta ya está cerrada");

                var estadoAnterior = ruta.Estado;

                var envios = context.Envios.Where(e => e.RutaId == rutaId).ToList();
                ruta.ParadasEntregadas = envios.Count(e => e.Estado == "Entregado");
                ruta.ParadasFallidas = envios.Count(e => e.Estado == "Fallido");
                ruta.Estado = "Completada";
                ruta.FechaRegreso = DateTime.Now;

                if (!string.IsNullOrWhiteSpace(notas))
                {
                    ruta.Notas = (string.IsNullOrWhiteSpace(ruta.Notas) ? "" : ruta.Notas + "\n") +
                                 $"[Cierre {DateTime.Now:dd/MM/yyyy HH:mm}] {notas}";
                }

                var usuario = usuarioId.HasValue
                    ? context.Usuarios.FirstOrDefault(u => u.Id == usuarioId.Value)
                    : null;

                context.RutasHistorial.Add(new RutaHistorial
                {
                    RutaId = ruta.Id,
                    Evento = "Completada",
                    Detalle = $"Ruta cerrada. Entregadas: {ruta.ParadasEntregadas}, Fallidas: {ruta.ParadasFallidas}",
                    EstadoAnterior = estadoAnterior,
                    EstadoNuevo = "Completada",
                    Fecha = DateTime.Now,
                    UsuarioId = usuarioId,
                    UsuarioNombre = usuario?.Username
                });

                context.SaveChanges();
                return (true, null);
            }
            catch (Exception ex)
            {
                return (false, ex.Message);
            }
        }

        /// <summary>
        /// Cancela una ruta (solo si está en Borrador).
        /// </summary>
        public static (bool ok, string? error) Cancelar(
            ApplicationDbContext context,
            int rutaId,
            string motivo,
            int? usuarioId)
        {
            try
            {
                var ruta = context.Rutas.FirstOrDefault(r => r.Id == rutaId);
                if (ruta == null) return (false, "Ruta no encontrada");

                if (ruta.Estado != "Borrador")
                    return (false, "Solo se pueden cancelar rutas en estado Borrador");

                var estadoAnterior = ruta.Estado;
                ruta.Estado = "Cancelada";

                // Liberar los envíos
                var envios = context.Envios.Where(e => e.RutaId == rutaId).ToList();
                foreach (var envio in envios)
                {
                    envio.RutaId = null;
                    envio.OrdenParada = null;
                    envio.Estado = "Pendiente";
                }

                ruta.Notas = (string.IsNullOrWhiteSpace(ruta.Notas) ? "" : ruta.Notas + "\n") +
                             $"[Cancelada {DateTime.Now:dd/MM/yyyy HH:mm}] {motivo}";

                var usuario = usuarioId.HasValue
                    ? context.Usuarios.FirstOrDefault(u => u.Id == usuarioId.Value)
                    : null;

                context.RutasHistorial.Add(new RutaHistorial
                {
                    RutaId = ruta.Id,
                    Evento = "Cancelada",
                    Detalle = motivo,
                    EstadoAnterior = estadoAnterior,
                    EstadoNuevo = "Cancelada",
                    Fecha = DateTime.Now,
                    UsuarioId = usuarioId,
                    UsuarioNombre = usuario?.Username
                });

                context.SaveChanges();
                return (true, null);
            }
            catch (Exception ex)
            {
                return (false, ex.Message);
            }
        }

        /// <summary>
        /// Agrega un envío pendiente a una ruta existente (solo si está en Borrador).
        /// </summary>
        public static (bool ok, string? error) AgregarEnvio(
            ApplicationDbContext context,
            int rutaId,
            int envioId)
        {
            try
            {
                var ruta = context.Rutas.FirstOrDefault(r => r.Id == rutaId);
                if (ruta == null) return (false, "Ruta no encontrada");

                if (ruta.Estado != "Borrador")
                    return (false, "Solo se pueden agregar envíos a rutas en Borrador");

                var envio = context.Envios.FirstOrDefault(e => e.Id == envioId);
                if (envio == null) return (false, "Envío no encontrado");

                if (envio.RutaId != null)
                    return (false, "Este envío ya tiene una ruta asignada");

                if (envio.Estado != "Pendiente")
                    return (false, "Solo se pueden agregar envíos en estado Pendiente");

                var maxOrden = context.Envios
                    .Where(e => e.RutaId == rutaId)
                    .Select(e => e.OrdenParada ?? 0)
                    .DefaultIfEmpty(0)
                    .Max();

                envio.RutaId = rutaId;
                envio.OrdenParada = maxOrden + 1;

                var todos = context.Envios.Where(e => e.RutaId == rutaId).ToList();
                ruta.TotalParadas = todos.Count;
                ruta.MontoTotalEnvios = todos.Sum(e => e.Monto);

                context.SaveChanges();
                return (true, null);
            }
            catch (Exception ex)
            {
                return (false, ex.Message);
            }
        }

        /// <summary>
        /// Quita un envío de una ruta (solo si está en Borrador).
        /// </summary>
        public static (bool ok, string? error) QuitarEnvio(
            ApplicationDbContext context,
            int rutaId,
            int envioId)
        {
            try
            {
                var ruta = context.Rutas.FirstOrDefault(r => r.Id == rutaId);
                if (ruta == null) return (false, "Ruta no encontrada");

                if (ruta.Estado != "Borrador")
                    return (false, "Solo se pueden quitar envíos de rutas en Borrador");

                var envio = context.Envios.FirstOrDefault(e => e.Id == envioId && e.RutaId == rutaId);
                if (envio == null) return (false, "Este envío no pertenece a esta ruta");

                envio.RutaId = null;
                envio.OrdenParada = null;

                var todos = context.Envios.Where(e => e.RutaId == rutaId).ToList();
                ruta.TotalParadas = todos.Count;
                ruta.MontoTotalEnvios = todos.Sum(e => e.Monto);

                // Reordenar
                int orden = 1;
                foreach (var e in todos.OrderBy(x => x.OrdenParada))
                {
                    e.OrdenParada = orden;
                    orden++;
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
        /// Reordena las paradas de una ruta.
        /// </summary>
        public static (bool ok, string? error) Reordenar(
            ApplicationDbContext context,
            int rutaId,
            List<int> envioIdsEnOrden)
        {
            try
            {
                var ruta = context.Rutas.FirstOrDefault(r => r.Id == rutaId);
                if (ruta == null) return (false, "Ruta no encontrada");

                if (ruta.Estado != "Borrador")
                    return (false, "Solo se pueden reordenar rutas en Borrador");

                var envios = context.Envios.Where(e => e.RutaId == rutaId).ToList();
                if (envios.Count != envioIdsEnOrden.Count)
                    return (false, "La lista de orden no coincide con las paradas de la ruta");

                int orden = 1;
                foreach (var envioId in envioIdsEnOrden)
                {
                    var envio = envios.FirstOrDefault(e => e.Id == envioId);
                    if (envio == null) return (false, $"Envío {envioId} no pertenece a esta ruta");

                    envio.OrdenParada = orden;
                    orden++;
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
        /// Obtiene los envíos de una ruta ordenados por parada.
        /// </summary>
        public static List<Envio> ObtenerEnviosDeRuta(ApplicationDbContext context, int rutaId)
        {
            return context.Envios
                .AsNoTracking()
                .Where(e => e.RutaId == rutaId)
                .OrderBy(e => e.OrdenParada)
                .ToList();
        }

        /// <summary>
        /// Recalcula los totales de una ruta.
        /// </summary>
        public static void RecalcularTotales(ApplicationDbContext context, int rutaId)
        {
            var ruta = context.Rutas.FirstOrDefault(r => r.Id == rutaId);
            if (ruta == null) return;

            var envios = context.Envios.Where(e => e.RutaId == rutaId).ToList();

            ruta.TotalParadas = envios.Count;
            ruta.ParadasEntregadas = envios.Count(e => e.Estado == "Entregado");
            ruta.ParadasFallidas = envios.Count(e => e.Estado == "Fallido");
            ruta.MontoTotalEnvios = envios.Sum(e => e.Monto);

            context.SaveChanges();
        }
    }
}