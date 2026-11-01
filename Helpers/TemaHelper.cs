using System.Security.Claims;

namespace Kirkenta.Helpers
{
    /// <summary>
    /// Tema visual del usuario (claro | oscuro | sistema). Se guarda en la cookie kk_tema_u{idUsuario}
    /// y la última elegida en kk_tema_last, para que Login y las páginas públicas respeten la preferencia.
    /// </summary>
    public static class TemaHelper
    {
        public static string Uid(HttpContext ctx) =>
            ctx.User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0";

        public static string Preferencia(HttpContext ctx)
        {
            var p = ctx.Request.Cookies["kk_tema_u" + Uid(ctx)] ?? ctx.Request.Cookies["kk_tema_last"] ?? "claro";
            return p is "claro" or "oscuro" or "sistema" ? p : "claro";
        }

        /// <summary>Valor inicial de data-theme: "dark" solo si eligió oscuro (en "sistema" lo resuelve el script del navegador).</summary>
        public static string Resuelto(string preferencia) => preferencia == "oscuro" ? "dark" : "light";
    }
}
