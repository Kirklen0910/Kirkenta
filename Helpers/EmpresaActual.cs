using System.Security.Claims;

namespace Kirkenta.Helpers
{
    /// <summary>
    /// Punto UNICO donde se decide a que empresa pertenece lo que se esta registrando.
    /// Antes habia "EmpresaId = 1" fijo en 21 sitios; ahora todos leen EmpresaActual.Id.
    /// Orden: claim "EmpresaId" del usuario autenticado -> variable de entorno KIRKENTA_EMPRESA_ID -> 1.
    /// El middleware de Program.cs llama a Establecer() en cada peticion.
    /// </summary>
    public static class EmpresaActual
    {
        private static readonly AsyncLocal<int?> _actual = new();
        private static readonly int _porDefecto = LeerPorDefecto();

        public static int Id => _actual.Value ?? _porDefecto;

        public static void Establecer(ClaimsPrincipal? usuario)
        {
            var claim = usuario?.FindFirst("EmpresaId")?.Value;
            _actual.Value = int.TryParse(claim, out var id) && id > 0 ? id : null;
        }

        private static int LeerPorDefecto()
        {
            var env = Environment.GetEnvironmentVariable("KIRKENTA_EMPRESA_ID");
            return int.TryParse(env, out var id) && id > 0 ? id : 1;
        }
    }
}
