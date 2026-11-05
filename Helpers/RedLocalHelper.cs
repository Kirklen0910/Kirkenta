using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace Kirkenta.Helpers
{
    /// <summary>
    /// Direcciones con las que los celulares y otros equipos de la misma red entran al sistema.
    /// </summary>
    public static class RedLocalHelper
    {
        public record Candidata(string Ip, string Adaptador, bool TienePuertaEnlace, bool EsWifi);
        public record Direccion(string Url, string Ip, string Adaptador);

        public record Estado(List<Direccion> Direcciones, bool SoloEstaComputadora, bool EsHttps, int Puerto);

        private static readonly string[] Virtuales = { "vmware", "virtualbox", "hyper-v", "vethernet", "wsl", "loopback", "bluetooth", "tap-", "tailscale", "zerotier", "docker", "npcap" };

        /// <summary>Las IPv4 de los adaptadores conectados de esta computadora.</summary>
        public static List<Candidata> CandidatasDelEquipo()
        {
            var r = new List<Candidata>();
            try
            {
                foreach (var ni in NetworkInterface.GetAllNetworkInterfaces())
                {
                    if (ni.OperationalStatus != OperationalStatus.Up || ni.NetworkInterfaceType == NetworkInterfaceType.Loopback) continue;
                    var props = ni.GetIPProperties();
                    var gw = props.GatewayAddresses.Any(g => g.Address.AddressFamily == AddressFamily.InterNetwork && !g.Address.Equals(IPAddress.Any));
                    foreach (var u in props.UnicastAddresses)
                        if (u.Address.AddressFamily == AddressFamily.InterNetwork)
                            r.Add(new Candidata(u.Address.ToString(), ni.Name + " " + ni.Description, gw, ni.NetworkInterfaceType == NetworkInterfaceType.Wireless80211));
                }
            }
            catch { /* sin acceso a los adaptadores: no se muestra nada */ }
            return r;
        }

        /// <summary>
        /// Deja solo las direcciones de red privada de adaptadores reales: fuera las virtuales (VMware, Hyper-V…),
        /// las 127.x y 169.254.x. Primero las que tienen salida (puerta de enlace) y el Wi-Fi.
        /// </summary>
        public static List<Candidata> Elegir(IEnumerable<Candidata> candidatas)
        {
            var reales = candidatas
                .Where(c => IPAddress.TryParse(c.Ip, out var ip) && EsPrivada(ip))
                .Where(c => !Virtuales.Any(v => c.Adaptador.Contains(v, StringComparison.OrdinalIgnoreCase)))
                .GroupBy(c => c.Ip).Select(g => g.First())
                .ToList();
            // si alguna tiene puerta de enlace, esas son la red del negocio; las otras suelen ser cables sueltos o virtuales
            if (reales.Any(c => c.TienePuertaEnlace)) reales = reales.Where(c => c.TienePuertaEnlace).ToList();
            return reales.OrderByDescending(c => c.EsWifi).ThenBy(c => c.Ip).ToList();
        }

        public static bool EsPrivada(IPAddress ip)
        {
            var b = ip.GetAddressBytes();
            if (b.Length != 4) return false;
            return b[0] == 10
                || (b[0] == 172 && b[1] >= 16 && b[1] <= 31)
                || (b[0] == 192 && b[1] == 168);
        }

        /// <summary>
        /// Arma el estado a partir de las direcciones en las que escucha el sistema (por ejemplo
        /// «http://0.0.0.0:5114» o «https://0.0.0.0:5115»). Si existe una de red se usa esa; si todas son
        /// de esta computadora (localhost/127.0.0.1) los celulares no pueden entrar.
        /// </summary>
        public static Estado Calcular(IEnumerable<string> escucha, IEnumerable<Candidata> candidatas)
        {
            var urls = escucha.Select(a => Uri.TryCreate(a.Replace("://*", "://0.0.0.0").Replace("://+", "://0.0.0.0").Replace("://[::]", "://0.0.0.0"), UriKind.Absolute, out var u) ? u : null)
                .Where(u => u != null).Cast<Uri>().ToList();
            bool Local(Uri u) => u.Host is "localhost" or "127.0.0.1" or "[::1]" or "::1";
            var red = urls.Where(u => !Local(u)).OrderByDescending(u => u.Scheme == "https").FirstOrDefault();
            if (red == null)
            {
                var l = urls.FirstOrDefault();
                return new Estado(new(), true, l?.Scheme == "https", l?.Port ?? 5114);
            }
            List<Candidata> elegidas;
            if (red.Host != "0.0.0.0" && IPAddress.TryParse(red.Host, out _))
                elegidas = new() { new Candidata(red.Host, "", true, false) };   // escucha en una IP concreta
            else
                elegidas = Elegir(candidatas);
            var dirs = elegidas.Select(c => new Direccion($"{red.Scheme}://{c.Ip}:{red.Port}", c.Ip, c.Adaptador)).ToList();
            return new Estado(dirs, false, red.Scheme == "https", red.Port);
        }
    }
}
