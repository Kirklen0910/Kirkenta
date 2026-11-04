using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace Kirkenta.Services.Asistente
{
    public class IaResultado
    {
        public string? Accion { get; set; }
        public DateTime? Desde { get; set; }
        public DateTime? Hasta { get; set; }
        public string? Texto { get; set; }
        public decimal? Numero { get; set; }
    }

    /// <summary>
    /// Interpretación OPCIONAL con IA (plus de pago o local). Está apagada por defecto: el asistente funciona
    /// completo con el motor de reglas. Se enciende desde Configuración > Asistente con IA (solo Admin), donde se
    /// elige Ollama local u otro proveedor compatible con OpenAI (URL, modelo y clave opcional).
    /// La IA SOLO recibe el texto que escribió el usuario y la lista de acciones disponibles: nunca ve datos del
    /// negocio. Lo que decide ejecutar pasa por los mismos permisos, confirmaciones y contraseñas de siempre.
    /// </summary>
    public class AsistenteIA
    {
        private readonly AsistenteIAConfigStore _store;
        private readonly IHttpClientFactory _http;
        private readonly ILogger<AsistenteIA> _log;

        public AsistenteIA(AsistenteIAConfigStore store, IHttpClientFactory http, ILogger<AsistenteIA> log)
        {
            _store = store; _http = http; _log = log;
        }

        public bool Activa
        {
            get
            {
                var c = _store.Obtener();
                return c.Habilitada && !string.IsNullOrWhiteSpace(c.Url) && !string.IsNullOrWhiteSpace(c.Modelo);
            }
        }

        /// <summary>Prueba la conexión con la configuración dada (sin guardarla). Devuelve (ok, mensaje).</summary>
        public async Task<(bool Ok, string Mensaje)> ProbarAsync(AsistenteIAConfig cfg)
        {
            if (string.IsNullOrWhiteSpace(cfg.Url) || string.IsNullOrWhiteSpace(cfg.Modelo))
                return (false, "Falta la dirección (URL) o el modelo.");
            if (!Uri.TryCreate(cfg.Url.Trim(), UriKind.Absolute, out var uri) || (uri.Scheme != "http" && uri.Scheme != "https"))
                return (false, "La dirección debe empezar con http:// o https://");
            try
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
                var ia = await LlamarAsync(cfg, null, "Responde solo: ok", null, null, 20, false, cts.Token);
                if (!ia.Ok)
                {
                    var pista = ia.Status == 401 || ia.Status == 403
                        ? " La clave fue rechazada: verifica que sea de ESTE proveedor, sin espacios, y que no sea una clave vieja guardada (escribe la nueva en el campo)."
                        : " Revisa la dirección, el nombre del modelo y la clave.";
                    return (false, $"El servidor respondió {ia.Status}. {Recortar(ia.Detalle)}{pista}");
                }
                return (true, "Conexión correcta: la IA respondió.");
            }
            catch (TaskCanceledException) { return (false, "Se agotó el tiempo de espera (30 s). ¿Está encendido el servidor? Si es Ollama, la primera respuesta puede tardar."); }
            catch (HttpRequestException ex) { return (false, "No se pudo conectar: " + ex.Message); }
            catch (Exception ex) { _log.LogWarning(ex, "Prueba de IA fallida"); return (false, "No se pudo probar la conexión."); }
        }

        public async Task<IaResultado?> InterpretarAsync(string mensaje, IReadOnlyCollection<AccionDef> acciones, DateTime hoy)
        {
            if (!Activa || acciones.Count == 0) return null;
            var cfg = _store.Obtener();
            try
            {
                var catalogo = new StringBuilder();
                foreach (var a in acciones) catalogo.AppendLine($"- {a.Id}: {a.Titulo} — {a.Descripcion}. Ej: \"{a.Ejemplo}\"");

                var sistema =
                    "Eres el intérprete de un ERP en español. Del mensaje del usuario elige la acción del catálogo que MÁS se parezca a lo que pide " +
                    "(aunque use otras palabras: \"entregas en la calle\" = rutas/envíos en curso; \"cuánto vendimos\" = ventas; \"quién me debe\" = cuentas por cobrar). " +
                    "Responde ÚNICAMENTE un objeto JSON, sin texto adicional: " +
                    "{\"accion\":\"id exacto del catálogo o null\",\"desde\":\"yyyy-MM-dd o null\",\"hasta\":\"yyyy-MM-dd o null\",\"texto\":\"nombre o código a buscar o null\",\"numero\":número o null}. " +
                    $"Hoy es {hoy:yyyy-MM-dd}. Usa accion null solo si realmente ninguna se parece. No inventes ids.\nCatálogo:\n{catalogo}";

                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(180));
                var ia = await LlamarAsync(cfg, sistema, mensaje, null, null, 0, true, cts.Token);
                if (!ia.Ok)
                {
                    _log.LogWarning("Asistente IA respondió {Codigo}", ia.Status);
                    _store.Diagnostico($"Mensaje “{mensaje}”: el servidor respondió {ia.Status}. {Recortar(ia.Detalle)}");
                    return null;
                }
                var contenido = ia.Texto;
                var ini = contenido.IndexOf('{'); var fin = contenido.LastIndexOf('}');
                if (ini < 0 || fin <= ini)
                {
                    _store.Diagnostico($"Mensaje “{mensaje}”: la IA no devolvió JSON. Respuesta: {Recortar(contenido)}");
                    return null;
                }

                using var json = JsonDocument.Parse(contenido[ini..(fin + 1)]);
                var r = json.RootElement;
                var res = new IaResultado();
                if (r.TryGetProperty("accion", out var ac) && ac.ValueKind == JsonValueKind.String) res.Accion = ac.GetString();
                if (r.TryGetProperty("desde", out var d) && d.ValueKind == JsonValueKind.String && DateTime.TryParse(d.GetString(), out var dd)) res.Desde = dd.Date;
                if (r.TryGetProperty("hasta", out var h) && h.ValueKind == JsonValueKind.String && DateTime.TryParse(h.GetString(), out var hh)) res.Hasta = hh.Date;
                if (r.TryGetProperty("texto", out var t) && t.ValueKind == JsonValueKind.String) res.Texto = t.GetString();
                if (r.TryGetProperty("numero", out var n) && n.ValueKind == JsonValueKind.Number && n.TryGetDecimal(out var nn)) res.Numero = nn;

                // Solo se acepta una acción que exista en el catálogo permitido
                var id = res.Accion?.Trim().Trim('"');
                var def = acciones.FirstOrDefault(a => string.Equals(a.Id, id, StringComparison.OrdinalIgnoreCase));
                if (def == null)
                {
                    _store.Diagnostico($"Mensaje “{mensaje}”: la IA respondió accion={(res.Accion ?? "null")} y no hay una acción así disponible. Respuesta: {Recortar(contenido)}");
                    return null;
                }
                res.Accion = def.Id;
                _store.Diagnostico($"Mensaje “{mensaje}”: la IA eligió “{def.Titulo}”.");
                return res;
            }
            catch (Exception ex)
            {
                _log.LogWarning(ex, "No se pudo consultar la IA del asistente");
                _store.Diagnostico($"Mensaje “{mensaje}”: error al consultar la IA ({(ex is TaskCanceledException ? "tiempo de espera agotado" : ex.Message)}).");
                return null;
            }
        }

        // ================= LECTURA DE FACTURAS DE PROVEEDOR (foto) =================

        public class LineaFacturaLeida
        {
            public string? Codigo { get; set; }
            public string Descripcion { get; set; } = "";
            public decimal Cantidad { get; set; }
            public decimal Precio { get; set; }
            public decimal? Total { get; set; }
        }

        public class FacturaLeida
        {
            public string? Proveedor { get; set; }
            public string? Rtn { get; set; }
            public string? Numero { get; set; }
            public DateTime? Fecha { get; set; }
            public decimal? Subtotal { get; set; }
            public decimal? Isv { get; set; }
            public decimal? Total { get; set; }
            public List<LineaFacturaLeida> Lineas { get; set; } = new();
            public string? Error { get; set; }
        }

        /// <summary>
        /// Lee una foto de factura de proveedor con un modelo de IA que entienda imágenes (visión).
        /// OJO: a diferencia del asistente de texto, aquí SÍ se envía la imagen de la factura al servidor de IA configurado.
        /// El resultado es solo una propuesta: la persona lo revisa y corrige antes de crear la orden.
        /// </summary>
        public async Task<FacturaLeida> LeerFacturaAsync(byte[] imagen, string mime)
        {
            if (!Activa) return new FacturaLeida { Error = "La IA no está activada. Un administrador debe configurarla en Configuración → Asistente con IA (con un modelo que entienda imágenes)." };
            var cfg = _store.Obtener();
            try
            {
                const string sistema =
                    "Eres un lector de facturas de proveedores de Honduras. Lee la imagen y extrae los datos. " +
                    "Responde ÚNICAMENTE un objeto JSON, sin texto adicional, con esta forma: " +
                    "{\"proveedor\":\"nombre del emisor\",\"rtn\":\"RTN del emisor o null\",\"numero\":\"número de factura o null\",\"fecha\":\"yyyy-MM-dd o null\"," +
                    "\"subtotal\":número o null,\"isv\":número o null,\"total\":número o null," +
                    "\"lineas\":[{\"codigo\":\"código del artículo o null\",\"descripcion\":\"texto\",\"cantidad\":número,\"precio_unitario\":número,\"total\":número}]}. " +
                    "Los montos van en números sin símbolo de moneda ni separador de miles. Si no aparece el precio unitario, calcúlalo como total/cantidad. " +
                    "No inventes líneas ni datos que no se vean; usa null cuando no se pueda leer.";

                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(240));
                var ia = await LlamarAsync(cfg, sistema, "Extrae los datos de esta factura de proveedor.", imagen, mime, 3000, false, cts.Token);
                if (!ia.Ok)
                {
                    var detalle = Recortar(ia.Detalle);
                    _log.LogWarning("Lectura de factura con IA respondió {Codigo}: {Detalle}", ia.Status, detalle);
                    _store.Diagnostico($"Lectura de factura: el servidor respondió {ia.Status}. Detalle: {detalle}");
                    // El modelo configurado solo entiende texto (Groq, la mayoría de modelos locales…)
                    if (ia.Status == 400 && (detalle.Contains("must be a string") || detalle.Contains("image", StringComparison.OrdinalIgnoreCase)))
                        return new FacturaLeida { Error = "El modelo de IA configurado solo entiende texto y no puede leer fotos. En Configuración → Asistente con IA elige uno que lea imágenes: Gemini (gratis), Claude o GPT-4o." };
                    if (ia.Status == 401)
                        return new FacturaLeida { Error = "La clave de la IA no es válida o venció. Revísala en Configuración → Asistente con IA." };
                    return new FacturaLeida { Error = $"El servidor de IA respondió {ia.Status}. Detalle: {detalle} — Revisa que el modelo configurado entienda imágenes (claude, llava, qwen2.5-vl, gpt-4o…)." };
                }
                var contenido = ia.Texto;
                var ini = contenido.IndexOf('{'); var fin = contenido.LastIndexOf('}');
                if (ini < 0 || fin <= ini)
                    return new FacturaLeida { Error = "La IA no pudo leer la factura. Prueba con una foto más nítida, de frente y con buena luz." };

                using var json = JsonDocument.Parse(contenido[ini..(fin + 1)]);
                return ConvertirFactura(json.RootElement);
            }
            catch (TaskCanceledException) { return new FacturaLeida { Error = "La IA tardó demasiado (4 minutos). Prueba con una foto más pequeña o un modelo más rápido." }; }
            catch (HttpRequestException ex) { return new FacturaLeida { Error = "No se pudo conectar con el servidor de IA: " + ex.Message }; }
            catch (Exception ex)
            {
                _log.LogWarning(ex, "No se pudo leer la factura con IA");
                return new FacturaLeida { Error = "No se pudo interpretar la respuesta de la IA. Intenta de nuevo con otra foto." };
            }
        }

        public static FacturaLeida ConvertirFactura(JsonElement r)
        {
            static string? Txt(JsonElement e, string n)
            {
                if (!e.TryGetProperty(n, out var v)) return null;
                return v.ValueKind switch { JsonValueKind.String => v.GetString(), JsonValueKind.Number => v.GetRawText(), _ => null };
            }
            static decimal? Dec(JsonElement e, string n) => Kirkenta.Helpers.FacturaProveedorHelper.ParseDecimal(Txt(e, n));

            var f = new FacturaLeida
            {
                Proveedor = Txt(r, "proveedor")?.Trim(),
                Rtn = Txt(r, "rtn")?.Trim(),
                Numero = Txt(r, "numero")?.Trim(),
                Subtotal = Dec(r, "subtotal"),
                Isv = Dec(r, "isv"),
                Total = Dec(r, "total")
            };
            if (DateTime.TryParse(Txt(r, "fecha"), System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out var fecha)) f.Fecha = fecha.Date;
            if (r.TryGetProperty("lineas", out var ls) && ls.ValueKind == JsonValueKind.Array)
            {
                foreach (var l in ls.EnumerateArray().Take(80))
                {
                    if (l.ValueKind != JsonValueKind.Object) continue;
                    var desc = Txt(l, "descripcion")?.Trim() ?? "";
                    var cant = Dec(l, "cantidad") ?? 0;
                    var precio = Dec(l, "precio_unitario") ?? 0;
                    var total = Dec(l, "total");
                    if (precio == 0 && total.HasValue && cant > 0) precio = Math.Round(total.Value / cant, 4);
                    if (desc.Length == 0 && cant == 0) continue;
                    f.Lineas.Add(new LineaFacturaLeida { Codigo = Txt(l, "codigo")?.Trim(), Descripcion = desc, Cantidad = cant, Precio = precio, Total = total });
                }
            }
            if (f.Lineas.Count == 0) f.Error = "No se encontraron productos en la imagen. Prueba con una foto más nítida, de frente y con buena luz.";
            return f;
        }

        // ================= LLAMADA COMÚN (OpenAI-compatible o Claude de Anthropic) =================

        /// <summary>True si la dirección configurada es la API de Anthropic (Claude): se usa su formato nativo.</summary>
        public static bool EsAnthropic(string? url) => !string.IsNullOrWhiteSpace(url) && url.Contains("api.anthropic.com", StringComparison.OrdinalIgnoreCase);

        private record RespuestaIA(bool Ok, int Status, string Texto, string Detalle);

        /// <summary>
        /// Envía un mensaje (con imagen opcional) y devuelve el texto de la respuesta.
        /// maxTokens 0 = sin límite explícito. json = pedir salida JSON (solo servidores compatibles con OpenAI).
        /// </summary>
        private async Task<RespuestaIA> LlamarAsync(AsistenteIAConfig cfg, string? sistema, string usuario, byte[]? imagen, string? mime,
            int maxTokens, bool json, CancellationToken ct)
        {
            var cliente = _http.CreateClient();
            var anthropic = EsAnthropic(cfg.Url);

            async Task<RespuestaIA> Una(bool simple)
            {
                object cuerpo;
                if (anthropic)
                {
                    var partes = new List<object>();
                    if (imagen != null) partes.Add(new { type = "image", source = new { type = "base64", media_type = mime, data = Convert.ToBase64String(imagen) } });
                    partes.Add(new { type = "text", text = usuario });
                    var d = new Dictionary<string, object?>
                    {
                        ["model"] = cfg.Modelo.Trim(),
                        ["max_tokens"] = maxTokens > 0 ? maxTokens : 1024,
                        ["messages"] = new object[] { new { role = "user", content = partes } }
                    };
                    if (!string.IsNullOrWhiteSpace(sistema)) d["system"] = sistema;
                    cuerpo = d;
                }
                else
                {
                    object contenidoUsuario;
                    if (imagen != null)
                        contenidoUsuario = new object[]
                        {
                            new { type = "text", text = simple && sistema != null ? sistema + "\n\n" + usuario : usuario },
                            new { type = "image_url", image_url = new { url = $"data:{mime};base64,{Convert.ToBase64String(imagen)}" } }
                        };
                    else
                        contenidoUsuario = simple && sistema != null ? sistema + "\n\n" + usuario : usuario;

                    var mensajes = new List<object>();
                    if (!simple && !string.IsNullOrWhiteSpace(sistema)) mensajes.Add(new { role = "system", content = sistema });
                    mensajes.Add(new { role = "user", content = contenidoUsuario });

                    var d = new Dictionary<string, object?>
                    {
                        ["model"] = cfg.Modelo.Trim(),
                        ["stream"] = false,
                        ["messages"] = mensajes
                    };
                    if (!simple)
                    {
                        d["temperature"] = 0;
                        if (maxTokens > 0) d["max_tokens"] = maxTokens;
                        if (json && imagen == null) d["response_format"] = new { type = "json_object" };
                    }
                    cuerpo = d;
                }

                using var req = new HttpRequestMessage(HttpMethod.Post, cfg.Url.Trim()) { Content = new StringContent(JsonSerializer.Serialize(cuerpo), Encoding.UTF8, "application/json") };
                if (anthropic)
                {
                    req.Headers.Add("x-api-key", cfg.ApiKey ?? "");
                    req.Headers.Add("anthropic-version", "2023-06-01");
                }
                else if (!string.IsNullOrWhiteSpace(cfg.ApiKey))
                    req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", cfg.ApiKey);

                using var resp = await cliente.SendAsync(req, ct);
                var texto = await resp.Content.ReadAsStringAsync(ct);
                if (!resp.IsSuccessStatusCode) return new RespuestaIA(false, (int)resp.StatusCode, "", texto);

                try
                {
                    using var doc = JsonDocument.Parse(texto);
                    var raiz = doc.RootElement;
                    if (anthropic)
                    {
                        var sb = new StringBuilder();
                        foreach (var bloque in raiz.GetProperty("content").EnumerateArray())
                            if (bloque.TryGetProperty("text", out var t) && t.ValueKind == JsonValueKind.String) sb.Append(t.GetString());
                        return new RespuestaIA(true, (int)resp.StatusCode, sb.ToString(), "");
                    }
                    var c = raiz.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString() ?? "";
                    return new RespuestaIA(true, (int)resp.StatusCode, c, "");
                }
                catch (Exception)
                {
                    return new RespuestaIA(false, (int)resp.StatusCode, "", "Respuesta con formato inesperado: " + texto);
                }
            }

            var r = await Una(false);
            // Algunos servidores estrictos (modelos locales) rechazan rol "system" / max_tokens: un segundo intento con formato simple
            if (!r.Ok && r.Status == 400 && !anthropic) r = await Una(true);
            return r;
        }

        /// <summary>Pide al proveedor la lista de modelos que esta clave puede usar (GET /models). Devuelve (ok, mensaje, ids).</summary>
        public async Task<(bool Ok, string Mensaje, List<string> Modelos)> ListarModelosAsync(AsistenteIAConfig cfg)
        {
            var vacio = new List<string>();
            if (string.IsNullOrWhiteSpace(cfg.Url)) return (false, "Primero elige un proveedor o escribe la dirección.", vacio);
            var url = cfg.Url.Trim();
            var anthropic = EsAnthropic(url);
            string? destino = null;
            foreach (var fin in new[] { "/chat/completions", "/messages" })
                if (url.EndsWith(fin, StringComparison.OrdinalIgnoreCase)) { destino = url[..^fin.Length] + "/models"; break; }
            if (destino == null) return (false, "No pude deducir la dirección de modelos a partir de la URL del chat.", vacio);
            try
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
                using var req = new HttpRequestMessage(HttpMethod.Get, destino);
                if (anthropic) { req.Headers.Add("x-api-key", cfg.ApiKey ?? ""); req.Headers.Add("anthropic-version", "2023-06-01"); }
                else if (!string.IsNullOrWhiteSpace(cfg.ApiKey)) req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", cfg.ApiKey);
                using var resp = await _http.CreateClient().SendAsync(req, cts.Token);
                var texto = await resp.Content.ReadAsStringAsync(cts.Token);
                if (!resp.IsSuccessStatusCode) return (false, $"El servidor respondió {(int)resp.StatusCode}. {Recortar(texto)}", vacio);
                using var doc = JsonDocument.Parse(texto);
                var ids = new List<string>();
                if (doc.RootElement.TryGetProperty("data", out var data) && data.ValueKind == JsonValueKind.Array)
                    foreach (var m in data.EnumerateArray())
                        if (m.TryGetProperty("id", out var id) && id.ValueKind == JsonValueKind.String) ids.Add(id.GetString()!);
                ids.Sort(StringComparer.OrdinalIgnoreCase);
                return ids.Count == 0
                    ? (false, "El servidor respondió, pero no devolvió modelos.", ids)
                    : (true, $"Modelos disponibles con esta clave ({ids.Count}). Pulsa uno para usarlo:", ids);
            }
            catch (TaskCanceledException) { return (false, "Se agotó el tiempo de espera (30 s).", vacio); }
            catch (HttpRequestException ex) { return (false, "No se pudo conectar: " + ex.Message, vacio); }
            catch (JsonException) { return (false, "El servidor devolvió una respuesta que no pude leer.", vacio); }
        }

        private static string Recortar(string s) => s.Length <= 300 ? s : s[..300] + "…";
    }
}
