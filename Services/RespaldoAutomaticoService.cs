using System.Net;
using System.Net.Mail;
using Kirkenta.Helpers;

namespace Kirkenta.Services
{
    /// <summary>
    /// Respaldo completo automático de los datos del cliente. Por defecto corre cada noche a las 02:00 y conserva los últimos 14.
    /// Se configura desde Configuración → Respaldos del sistema (hora, cuántos conservar y carpeta de copia externa:
    /// USB, otro disco o unidad de red). Como respaldo se leen las claves Respaldo:* del appsettings.
    /// </summary>
    public class RespaldoAutomaticoService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopes;
        private readonly IConfiguration _cfg;
        private readonly ILogger<RespaldoAutomaticoService> _log;
        private readonly RespaldoConfigStore _store;
        private DateTime _ultimoIntento = DateTime.MinValue;

        public RespaldoAutomaticoService(IServiceScopeFactory scopes, IConfiguration cfg, ILogger<RespaldoAutomaticoService> log, RespaldoConfigStore store)
        {
            _scopes = scopes; _cfg = cfg; _log = log; _store = store;
        }

        /// <summary>true si toca respaldar: ya pasó la hora de hoy y no se ha hecho un intento hoy.</summary>
        public static bool Toca(DateTime ahora, TimeSpan hora, DateTime ultimoIntento) =>
            ahora.TimeOfDay >= hora && ultimoIntento.Date < ahora.Date;

        /// <summary>Si el correo está configurado (Correo:*), avisa por correo que el respaldo falló. Mejor esfuerzo.</summary>
        private async Task NotificarFalloAsync(string detalle, CancellationToken ct, bool soloCopiaExterna = false)
        {
            try
            {
                if (!ResumenDiarioService.Configurado(_cfg)) return;
                var host = _cfg["Correo:Host"]!;
                var puerto = int.TryParse(_cfg["Correo:Puerto"], out var p) ? p : 587;
                var ssl = !string.Equals(_cfg["Correo:Ssl"], "false", StringComparison.OrdinalIgnoreCase);
                var usuario = _cfg["Correo:Usuario"];
                var desde = _cfg["Correo:Desde"] ?? usuario ?? "kirkenta@localhost";
                using var msg = new MailMessage
                {
                    From = new MailAddress(desde, "Kirkenta"),
                    Subject = soloCopiaExterna ? "⚠️ Kirkenta no pudo copiar el respaldo a la carpeta externa" : "⚠️ Falló el respaldo automático de Kirkenta",
                    Body = $"<p>{(soloCopiaExterna ? "El respaldo se creó, pero no se pudo copiar a la carpeta externa (¿USB desconectado o disco lleno?)." : "El respaldo automático de la noche no se pudo crear.")}</p><p><strong>Detalle:</strong> {WebUtility.HtmlEncode(detalle)}</p><p>Entra a Configuración → Respaldos del sistema para crear uno manual y revisar el problema (espacio en disco, permisos de la carpeta de respaldos, conexión a la base).</p>",
                    IsBodyHtml = true
                };
                foreach (var d in (_cfg["Correo:Destinatarios"] ?? "").Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                    msg.To.Add(d);
                using var smtp = new SmtpClient(host, puerto) { EnableSsl = ssl, Timeout = 30000 };
                if (!string.IsNullOrWhiteSpace(usuario)) smtp.Credentials = new NetworkCredential(usuario, _cfg["Correo:Clave"]);
                await smtp.SendMailAsync(msg, ct);
            }
            catch (Exception ex) { _log.LogWarning(ex, "No se pudo enviar el aviso de fallo del respaldo"); }
        }

        protected override async Task ExecuteAsync(CancellationToken ct)
        {
            try { await Task.Delay(TimeSpan.FromMinutes(2), ct); } catch (OperationCanceledException) { return; }

            while (!ct.IsCancellationRequested)
            {
                try
                {
                    // Se lee la configuración en cada vuelta: los cambios hechos en pantalla aplican sin reiniciar.
                    var cfg = _store.Obtener();
                    if (cfg.Automatico && TimeSpan.TryParse(cfg.Hora, out var hora))
                    {
                        var ahora = DateTime.Now;
                        if (Toca(ahora, hora, _ultimoIntento))
                        {
                            _ultimoIntento = ahora;
                            using var scope = _scopes.CreateScope();
                            var svc = scope.ServiceProvider.GetRequiredService<RespaldoSistemaService>();

                            // Si el sistema se reinició y ya hay un respaldo de hoy hecho después de la hora programada, no se repite.
                            var ultimo = svc.Listar().FirstOrDefault(f => !f.Name.Contains("previo"));
                            if (ultimo != null && ultimo.LastWriteTime.Date == ahora.Date && ultimo.LastWriteTime.TimeOfDay >= hora)
                            {
                                await CopiarExternaAsync(ultimo.FullName, cfg, ct);
                            }
                            else
                            {
                                var r = await svc.CrearAsync();
                                if (r.Ok)
                                {
                                    _log.LogInformation("Respaldo automático: {m}", r.Mensaje);
                                    svc.AplicarRetencion(cfg.Conservar);
                                    if (r.Ruta != null) await CopiarExternaAsync(r.Ruta, cfg, ct);
                                }
                                else
                                {
                                    _log.LogError("Respaldo automático falló: {m}", r.Mensaje);
                                    await NotificarFalloAsync(r.Mensaje, ct);
                                }
                            }
                        }
                    }
                }
                catch (Exception ex) { _log.LogError(ex, "Error en el respaldo automático"); }

                try { await Task.Delay(TimeSpan.FromMinutes(10), ct); } catch (OperationCanceledException) { break; }
            }
        }

        private async Task CopiarExternaAsync(string rutaZip, RespaldoConfig cfg, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(cfg.CopiaExterna)) return;
            var (ok, mensaje) = RespaldoConfigStore.CopiarAExterna(rutaZip, cfg.CopiaExterna!, cfg.Conservar);
            _store.RegistrarCopia(ok, mensaje);
            if (ok) _log.LogInformation("Respaldo automático: {m}", mensaje);
            else
            {
                _log.LogWarning("Respaldo automático: {m}", mensaje);
                await NotificarFalloAsync(mensaje, ct, soloCopiaExterna: true);
            }
        }
    }
}
