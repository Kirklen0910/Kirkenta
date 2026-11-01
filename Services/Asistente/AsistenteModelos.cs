using Kirkenta.Data;
using Kirkenta.Models;

namespace Kirkenta.Services.Asistente
{
    public enum TipoAccion { Consulta, Navegar, Operacion }

    /// <summary>Estado que el navegador devuelve en cada mensaje (el servidor no guarda conversaciones).</summary>
    public class ContextoCliente
    {
        public string? Accion { get; set; }
        public Dictionary<string, string>? Params { get; set; }
        /// <summary>Dato que el asistente está esperando que el usuario escriba (ej. "contado").</summary>
        public string? Slot { get; set; }
        /// <summary>Última consulta respondida (para entender "¿y ayer?" o "¿y solo en efectivo?").</summary>
        public string? UltimaAccion { get; set; }
        public Dictionary<string, string>? UltimaParams { get; set; }
    }

    public class OpcionAsistente
    {
        public string Texto { get; set; } = "";
        /// <summary>Texto que se enviará al asistente si el usuario elige esta opción.</summary>
        public string Envio { get; set; } = "";
    }

    public class EnlaceAsistente
    {
        public string Texto { get; set; } = "";
        public string Url { get; set; } = "";
        public bool Externo { get; set; }
    }

    /// <summary>Todo lo que el asistente le devuelve al navegador.</summary>
    public class RespuestaAsistente
    {
        /// <summary>texto | tabla | confirmar | faltante | opciones | error</summary>
        public string Tipo { get; set; } = "texto";
        public string? Titulo { get; set; }
        public string Mensaje { get; set; } = "";
        public string? Empresa { get; set; }
        /// <summary>"ia" si la interpretó la IA; "ia-sin-respuesta" si la IA estaba activa pero tampoco entendió. Vacío = reglas.</summary>
        public string? Origen { get; set; }

        public List<string>? Columnas { get; set; }
        public List<List<string>>? Filas { get; set; }
        public List<string>? Totales { get; set; }
        /// <summary>Columnas alineadas a la derecha (montos).</summary>
        public List<int>? ColumnasNumericas { get; set; }

        public List<string>? Resumen { get; set; }
        public string? Token { get; set; }
        public bool RequierePassword { get; set; }

        public List<OpcionAsistente>? Opciones { get; set; }
        public List<EnlaceAsistente>? Enlaces { get; set; }
        public ContextoCliente? Contexto { get; set; }
        /// <summary>El navegador lo guarda y lo manda de vuelta para poder continuar la conversación.</summary>
        public ContextoCliente? Seguimiento { get; set; }

        public static RespuestaAsistente Texto(string mensaje, string? titulo = null)
            => new() { Tipo = "texto", Mensaje = mensaje, Titulo = titulo };

        public static RespuestaAsistente Error(string mensaje)
            => new() { Tipo = "error", Mensaje = mensaje };

        public static RespuestaAsistente Tabla(string titulo, string mensaje, string[] columnas,
            List<List<string>> filas, List<string>? totales = null, int[]? numericas = null)
            => new()
            {
                Tipo = "tabla",
                Titulo = titulo,
                Mensaje = mensaje,
                Columnas = columnas.ToList(),
                Filas = filas,
                Totales = totales,
                ColumnasNumericas = numericas?.ToList()
            };

        public static RespuestaAsistente Faltante(string pregunta, string accionId, Dictionary<string, string> p, string slot)
            => new()
            {
                Tipo = "faltante",
                Mensaje = pregunta,
                Contexto = new ContextoCliente { Accion = accionId, Params = new Dictionary<string, string>(p), Slot = slot }
            };

        public static RespuestaAsistente Elegir(string mensaje, IEnumerable<OpcionAsistente> opciones)
            => new() { Tipo = "opciones", Mensaje = mensaje, Opciones = opciones.ToList() };
    }

    /// <summary>Datos con los que se ejecuta una acción.</summary>
    public class ContextoAccion
    {
        public ApplicationDbContext Db { get; init; } = null!;
        public Usuario Usuario { get; init; } = null!;
        public Dictionary<string, string> P { get; init; } = new();
        public DateTime Hoy { get; init; } = DateTime.Today;
        /// <summary>Inicio del rango (inclusive).</summary>
        public DateTime Desde { get; set; } = DateTime.Today;
        /// <summary>Fin del rango (inclusive, solo fecha).</summary>
        public DateTime Hasta { get; set; } = DateTime.Today;
        public string RangoTexto { get; set; } = "hoy";
        public string? Texto => P.TryGetValue("texto", out var t) && !string.IsNullOrWhiteSpace(t) ? t : null;
        public string? Ip { get; init; }
        /// <summary>Usuario que autorizó con su contraseña (puede ser el mismo).</summary>
        public string? Autorizo { get; set; }
        public int? AutorizoId { get; set; }

        public decimal? Numero(string clave)
            => P.TryGetValue(clave, out var v) && decimal.TryParse(v, System.Globalization.NumberStyles.Number,
                   System.Globalization.CultureInfo.InvariantCulture, out var d) ? d : null;
    }

    /// <summary>Definición de una cosa que el asistente sabe hacer.</summary>
    public class AccionDef
    {
        public string Id { get; init; } = "";
        public string Modulo { get; init; } = "General";
        public string Titulo { get; init; } = "";
        public string Descripcion { get; init; } = "";
        public string Ejemplo { get; init; } = "";
        public TipoAccion Tipo { get; init; } = TipoAccion.Consulta;

        /// <summary>
        /// Frases que activan la acción. Cada frase es un conjunto de palabras (todas deben aparecer en el
        /// mensaje, en cualquier orden). "vend*" significa "empieza con vend".
        /// </summary>
        public string[] Frases { get; init; } = Array.Empty<string>();

        /// <summary>Permiso requerido (módulo / submódulo / acción). Null = cualquier usuario activo.</summary>
        public string? PermModulo { get; init; }
        public string? PermSub { get; init; }
        public string PermAccion { get; init; } = "ver";

        /// <summary>Pide la contraseña (propia o de un usuario autorizado) antes de ejecutar.</summary>
        public bool RequierePassword { get; init; }

        /// <summary>La acción usa rango de fechas (hoy, ayer, este mes...).</summary>
        public bool UsaFechas { get; init; }

        /// <summary>Destino cuando la acción es de navegación.</summary>
        public string? Url { get; init; }

        /// <summary>Para operaciones: valida, pide datos faltantes y devuelve el resumen a confirmar.</summary>
        public Func<ContextoAccion, RespuestaAsistente>? Preparar { get; init; }
        public Func<ContextoAccion, RespuestaAsistente>? Ejecutar { get; init; }
    }
}
