using System.Security.Cryptography;
using System.Text;
using Kirkenta.Services.Licencia;

// ============================================================
// Licenciador de Kirkenta  -  herramienta SOLO para el emisor
//   Sin argumentos        -> menú interactivo
//   llaves                -> crea el par de llaves (una sola vez)
//   emitir [opciones]     -> crea una licencia
//   ver <licencia|.lic>   -> muestra qué trae una licencia
// La llave PRIVADA (llave-privada.pem) es lo más importante: quien la tenga puede fabricar licencias.
// Guárdala fuera del proyecto, con copia de seguridad, y NUNCA la entregues a un cliente.
// ============================================================

var carpeta = AppContext.BaseDirectory;
var rutaLlave = Environment.GetEnvironmentVariable("KIRKENTA_LLAVE") ?? Path.Combine(Directory.GetCurrentDirectory(), "llave-privada.pem");

if (args.Length == 0) return Menu();
return args[0].ToLowerInvariant() switch
{
    "llaves" => Llaves(),
    "emitir" => Emitir(Opciones(args.Skip(1).ToArray())),
    "ver" => Ver(args.Length > 1 ? args[1] : ""),
    _ => Ayuda()
};

static int Ayuda()
{
    Console.WriteLine(@"Uso:
  Licenciador                      menú interactivo
  Licenciador llaves               crea llave-privada.pem y muestra la clave pública para el sistema
  Licenciador emitir --plan demo7 --cliente ""Ferretería X"" --maquina ABCD-1234-ABCD-1234
       planes: min30, hora1, horas3, horas12, demo7, demo15, demo30, 6m, 1a, 3a, 5a, perpetua
       otras opciones: --dias N  --minutos N  --tipo demo|normal  --inicio 2026-12-01  --activar-dentro N  --libre (sirve en cualquier PC)
  Licenciador ver <texto de la licencia o archivo .lic>");
    return 0;
}

static Dictionary<string, string> Opciones(string[] a)
{
    var d = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    for (int i = 0; i < a.Length; i++)
    {
        if (!a[i].StartsWith("--")) continue;
        var k = a[i][2..];
        d[k] = (i + 1 < a.Length && !a[i + 1].StartsWith("--")) ? a[++i] : "true";
    }
    return d;
}

int Menu()
{
    while (true)
    {
        Console.WriteLine();
        Console.WriteLine("=== Licenciador de Kirkenta ===");
        Console.WriteLine("1) Crear llaves (solo la primera vez)");
        Console.WriteLine("2) Emitir una licencia");
        Console.WriteLine("3) Ver qué trae una licencia");
        Console.WriteLine("0) Salir");
        Console.Write("> ");
        switch (Console.ReadLine()?.Trim())
        {
            case "1": Llaves(); break;
            case "2": EmitirInteractivo(); break;
            case "3": Console.Write("Pega la licencia o la ruta del archivo .lic: "); Ver(Console.ReadLine() ?? ""); break;
            case "0": return 0;
        }
    }
}

void EmitirInteractivo()
{
    Console.WriteLine("Plan:");
    Console.WriteLine("   1) 30 minutos   2) 1 hora   3) 3 horas   4) 12 horas");
    Console.WriteLine("   5) Demo 7 días  6) Demo 15 días  7) Demo 30 días");
    Console.WriteLine("   8) 6 meses  9) 1 año  10) 3 años  11) 5 años  12) Sin vencimiento");
    Console.Write("> ");
    var plan = (Console.ReadLine()?.Trim()) switch
    {
        "1" => "min30", "2" => "hora1", "3" => "horas3", "4" => "horas12",
        "5" => "demo7", "6" => "demo15", "7" => "demo30",
        "8" => "6m", "9" => "1a", "10" => "3a", "11" => "5a", "12" => "perpetua",
        _ => ""
    };
    if (plan == "") { Console.WriteLine("Plan no válido."); return; }
    Console.Write("Nombre del cliente: ");
    var cliente = Console.ReadLine()?.Trim() ?? "";
    Console.Write("Código de la computadora (XXXX-XXXX-XXXX-XXXX) o Enter para licencia libre: ");
    var maq = Console.ReadLine()?.Trim() ?? "";
    var o = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { ["plan"] = plan, ["cliente"] = cliente };
    if (maq == "") o["libre"] = "true"; else o["maquina"] = maq;
    Emitir(o);
}

int Llaves()
{
    if (File.Exists(rutaLlave))
    {
        Console.WriteLine($"Ya existe {rutaLlave}. No se sobrescribe (cambiarla invalidaría todas las licencias ya emitidas).");
        return 1;
    }
    using var ec = ECDsa.Create(ECCurve.NamedCurves.nistP256);
    File.WriteAllText(rutaLlave, ec.ExportPkcs8PrivateKeyPem());
    var pub = Convert.ToBase64String(ec.ExportSubjectPublicKeyInfo());
    Console.WriteLine($"Llave privada guardada en: {rutaLlave}");
    Console.WriteLine("  -> Haz copia de seguridad y guárdala FUERA del proyecto. No la entregues a nadie.");
    Console.WriteLine();
    Console.WriteLine("Pega esto como contenido completo de  Services\\Licencia\\LicenciaClavePublica.cs  en el sistema:");
    Console.WriteLine("------------------------------------------------------------");
    Console.WriteLine($@"namespace Kirkenta.Services.Licencia
{{
    public static class LicenciaClavePublica
    {{
        public const string Valor = ""{pub}"";
    }}
}}");
    Console.WriteLine("------------------------------------------------------------");
    return 0;
}

int Emitir(Dictionary<string, string> o)
{
    if (!File.Exists(rutaLlave)) { Console.WriteLine($"No encuentro la llave privada ({rutaLlave}). Ejecuta primero:  Licenciador llaves"); return 1; }

    var plan = o.GetValueOrDefault("plan", "").ToLowerInvariant();
    int dias; string tipo; int minutos = 0;
    switch (plan)
    {
        // Las licencias cortas llevan Dias = 1 por seguridad: una versión vieja del sistema las tomaría como de 1 día, nunca como ilimitadas
        case "min30": dias = 1; minutos = 30; tipo = "demo"; break;
        case "hora1": dias = 1; minutos = 60; tipo = "demo"; break;
        case "horas3": dias = 1; minutos = 180; tipo = "demo"; break;
        case "horas12": dias = 1; minutos = 720; tipo = "demo"; break;
        case "demo7": dias = 7; tipo = "demo"; break;
        case "demo15": dias = 15; tipo = "demo"; break;
        case "demo30": dias = 30; tipo = "demo"; break;
        case "6m": dias = 183; tipo = "normal"; break;
        case "1a": dias = 365; tipo = "normal"; break;
        case "3a": dias = 1095; tipo = "normal"; break;
        case "5a": dias = 1825; tipo = "normal"; break;
        case "perpetua": dias = 0; tipo = "normal"; break;
        default:
            if (int.TryParse(o.GetValueOrDefault("minutos", ""), out minutos) && minutos > 0)
            {
                dias = 1; tipo = o.GetValueOrDefault("tipo", "demo");
                break;
            }
            minutos = 0;
            if (!int.TryParse(o.GetValueOrDefault("dias", ""), out dias) || dias < 0) { Console.WriteLine("Indica --plan, --dias o --minutos."); return 1; }
            tipo = o.GetValueOrDefault("tipo", "normal");
            break;
    }
    if (o.TryGetValue("tipo", out var t2) && (t2 == "demo" || t2 == "normal")) tipo = t2;

    var libre = o.ContainsKey("libre");
    var maq = libre ? "*" : o.GetValueOrDefault("maquina", "").Trim().ToUpperInvariant();
    if (!libre && !System.Text.RegularExpressions.Regex.IsMatch(maq, @"^[0-9A-F]{4}(-[0-9A-F]{4}){3}$"))
    { Console.WriteLine("El código de la computadora debe verse así: ABCD-1234-ABCD-1234 (o usa --libre)."); return 1; }

    var ahora = DateTime.UtcNow;
    var ventana = int.TryParse(o.GetValueOrDefault("activar-dentro", ""), out var w) ? w : (tipo == "demo" ? 3 : 30);
    DateTime? inicio = null;
    if (o.TryGetValue("inicio", out var si))
    {
        if (!DateTime.TryParse(si, out var di)) { Console.WriteLine("--inicio no es una fecha válida (usa 2026-12-01)."); return 1; }
        inicio = DateTime.SpecifyKind(di.Date, DateTimeKind.Utc);
    }

    var d = new LicenciaDatos
    {
        Id = Guid.NewGuid().ToString("N"),
        Cliente = o.GetValueOrDefault("cliente", "").Trim(),
        Tipo = tipo,
        Dias = dias,
        Minutos = minutos,
        Maquina = maq,
        Emitida = ahora,
        ActivarAntes = ahora.AddDays(ventana),
        Inicio = inicio
    };

    using var ec = ECDsa.Create();
    ec.ImportFromPem(File.ReadAllText(rutaLlave));
    var texto = LicenciaFormato.Firmar(d, ec);

    var dir = Path.Combine(Directory.GetCurrentDirectory(), "licencias-emitidas");
    Directory.CreateDirectory(dir);
    var nombre = new string((string.IsNullOrWhiteSpace(d.Cliente) ? "cliente" : d.Cliente).Where(c => char.IsLetterOrDigit(c)).ToArray());
    var duracion = minutos > 0 ? $"{minutos}min" : $"{dias}d";
    var archivo = Path.Combine(dir, $"{nombre}-{tipo}-{duracion}-{d.Id[..6]}.lic");
    File.WriteAllText(archivo, texto);
    File.AppendAllText(Path.Combine(dir, "registro.csv"),
        $"{ahora:yyyy-MM-dd HH:mm},{d.Id},\"{d.Cliente}\",{tipo},{(minutos > 0 ? minutos + " min" : dias + " d")},{maq},{(inicio?.ToString("yyyy-MM-dd") ?? "")}\r\n", Encoding.UTF8);

    Console.WriteLine();
    Console.WriteLine($"Licencia {(tipo == "demo" ? "DEMO" : "normal")}: {(minutos > 0 ? Duracion(minutos) : dias == 0 ? "sin vencimiento" : dias + " días")}  |  cliente: {d.Cliente}  |  PC: {maq}");
    Console.WriteLine($"Debe activarse antes del {d.ActivarAntes:dd/MM/yyyy}" + (inicio != null ? $"  |  la vigencia cuenta desde {inicio:dd/MM/yyyy}" : "  |  la vigencia cuenta desde que se active"));
    Console.WriteLine($"Guardada en: {archivo}");
    Console.WriteLine();
    Console.WriteLine("Envía al cliente este texto completo:");
    Console.WriteLine(texto);
    return 0;
}

int Ver(string entrada)
{
    var texto = File.Exists(entrada) ? File.ReadAllText(entrada) : entrada;
    var t = LicenciaFormato.Normalizar(texto);
    if (!t.StartsWith(LicenciaFormato.Prefijo)) { Console.WriteLine("No parece una licencia."); return 1; }
    try
    {
        var p = t[LicenciaFormato.Prefijo.Length..].Split('.')[0].Replace('-', '+').Replace('_', '/');
        p += new string('=', (4 - p.Length % 4) % 4);
        Console.WriteLine(Encoding.UTF8.GetString(Convert.FromBase64String(p)));
        Console.WriteLine("(Solo se muestra el contenido; la firma la comprueba el sistema al activarla.)");
        return 0;
    }
    catch { Console.WriteLine("No se pudo leer."); return 1; }
}

static string Duracion(int minutos)
    => minutos % 60 == 0 && minutos >= 60 ? $"{minutos / 60} hora(s)" : $"{minutos} minutos";
