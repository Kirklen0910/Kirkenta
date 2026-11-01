using System.Net;
using System.Net.Mail;
using Kirkenta.Data;
using Kirkenta.Helpers;
using Microsoft.EntityFrameworkCore;

namespace Kirkenta.Services
{
    /// <summary>
    /// Manda cada día, a la hora configurada, un resumen del negocio por correo.
    /// Está APAGADO mientras no se configure el correo. Se configura sin tocar appsettings.json:
    /// con variables de entorno o dotnet user-secrets (clave:valor):
    ///   Correo:Host, Correo:Puerto (587), Correo:Ssl (true), Correo:Usuario, Correo:Clave,
    ///   Correo:Desde, Correo:Destinatarios (separados por coma), Correo:HoraResumen (20:00)
    /// Ej. con variables de entorno:  Correo__Host=smtp.gmail.com  Correo__Destinatarios=dueno@correo.com
    /// </summary>
    public class ResumenDiarioService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopes;
        private readonly IConfiguration _cfg;
        private readonly ILogger<ResumenDiarioService> _log;
        private DateTime _ultimoEnvio = DateTime.MinValue;

        public ResumenDiarioService(IServiceScopeFactory scopes, IConfiguration cfg, ILogger<ResumenDiarioService> log)
        {
            _scopes = scopes; _cfg = cfg; _log = log;
        }

        public static bool Configurado(IConfiguration c)
            => !string.IsNullOrWhiteSpace(c["Correo:Host"]) && !string.IsNullOrWhiteSpace(c["Correo:Destinatarios"]);

        /// <summary>¿Ya pasó la hora de hoy y todavía no se envió hoy?</summary>
        public static bool ToqueEnviar(DateTime ahora, TimeSpan hora, DateTime ultimoEnvio)
            => ahora.TimeOfDay >= hora && ultimoEnvio.Date < ahora.Date;

        protected override async Task ExecuteAsync(CancellationToken ct)
        {
            // Espera inicial para no competir con el arranque de la aplicación
            try { await Task.Delay(TimeSpan.FromSeconds(30), ct); } catch (OperationCanceledException) { return; }

            while (!ct.IsCancellationRequested)
            {
                try
                {
                    if (Configurado(_cfg))
                    {
                        var hora = TimeSpan.TryParse(_cfg["Correo:HoraResumen"], out var h) ? h : new TimeSpan(20, 0, 0);
                        if (ToqueEnviar(DateTime.Now, hora, _ultimoEnvio))
                        {
                            _ultimoEnvio = DateTime.Now; // se marca antes: si falla no se reintenta en bucle cada minuto
                            await EnviarAsync(ct);
                        }
                    }
                }
                catch (OperationCanceledException) { return; }
                catch (Exception ex) { _log.LogError(ex, "No se pudo enviar el resumen diario por correo"); }

                try { await Task.Delay(TimeSpan.FromMinutes(1), ct); } catch (OperationCanceledException) { return; }
            }
        }

        private async Task EnviarAsync(CancellationToken ct)
        {
            using var scope = _scopes.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var empresa = db.ConfiguracionEmpresa.AsNoTracking().Select(e => e.Nombre).FirstOrDefault() ?? "Kirkenta";
            var resumen = ResumenNegocioHelper.Calcular(db, DateTime.Now);

            var host = _cfg["Correo:Host"]!;
            var puerto = int.TryParse(_cfg["Correo:Puerto"], out var p) ? p : 587;
            var ssl = !string.Equals(_cfg["Correo:Ssl"], "false", StringComparison.OrdinalIgnoreCase);
            var usuario = _cfg["Correo:Usuario"];
            var desde = _cfg["Correo:Desde"] ?? usuario ?? "kirkenta@localhost";

            using var mensaje = new MailMessage
            {
                From = new MailAddress(desde, empresa),
                Subject = ResumenNegocioHelper.Asunto(empresa, resumen),
                Body = ResumenNegocioHelper.ToHtml(empresa, resumen),
                IsBodyHtml = true
            };
            foreach (var d in (_cfg["Correo:Destinatarios"] ?? "").Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                mensaje.To.Add(d);

            using var smtp = new SmtpClient(host, puerto) { EnableSsl = ssl, Timeout = 30000 };
            if (!string.IsNullOrWhiteSpace(usuario))
                smtp.Credentials = new NetworkCredential(usuario, _cfg["Correo:Clave"]);

            await smtp.SendMailAsync(mensaje, ct);
            _log.LogInformation("Resumen diario enviado a {Destinos}", string.Join(", ", mensaje.To.Select(a => a.Address)));
        }
    }
}
