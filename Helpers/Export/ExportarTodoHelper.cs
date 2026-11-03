using System.Collections;
using System.Globalization;
using System.IO.Compression;
using System.Text;
using Kirkenta.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Kirkenta.Helpers.Export
{
    /// <summary>
    /// «Llévate todos tus datos»: un .zip con un CSV por tabla (UTF-8, se abre en Excel) y un LEEME.
    /// Los datos son del negocio: se pueden llevar a donde quieran, sin pedir permiso a nadie.
    /// Nunca incluye contraseñas, tokens ni llaves.
    /// </summary>
    public static class ExportarTodoHelper
    {
        // columnas que jamás salen del sistema
        private static readonly string[] Secretos = { "password", "contrasena", "clave", "hash", "salt", "token", "secret", "apikey", "llave", "firma" };

        public record Tabla(string Archivo, string Titulo, int Filas);

        internal static bool EsSecreto(string propiedad)
        {
            var n = propiedad.ToLowerInvariant();
            return Secretos.Any(n.Contains);
        }

        public static List<Tabla> Generar(ApplicationDbContext ctx, Stream destino)
        {
            var tablas = new List<Tabla>();
            var anterior = ctx.ChangeTracker.QueryTrackingBehavior;
            ctx.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
            try
            {
                using var zip = new ZipArchive(destino, ZipArchiveMode.Create, leaveOpen: true);
                foreach (var tipo in ctx.Model.GetEntityTypes()
                             .Where(t => !t.IsOwned() && t.FindPrimaryKey() != null && t.GetTableName() != null && t.ClrType != typeof(Dictionary<string, object>)
                                 && !t.ClrType.Name.Contains("Licencia"))       // la licencia es del equipo, no del negocio
                             .OrderBy(t => t.GetTableName()))
                {
                    var props = tipo.GetProperties().Where(p => p.PropertyInfo != null && !EsSecreto(p.Name)).ToList();
                    if (props.Count == 0) continue;
                    var archivo = tipo.GetTableName()! + ".csv";
                    var entrada = zip.CreateEntry(archivo, CompressionLevel.Optimal);
                    int filas = 0;
                    using (var w = new StreamWriter(entrada.Open(), new UTF8Encoding(true)))
                    {
                        w.WriteLine(string.Join(",", props.Select(p => Celda(p.Name))));
                        var set = typeof(DbContext).GetMethod(nameof(DbContext.Set), Type.EmptyTypes)!.MakeGenericMethod(tipo.ClrType).Invoke(ctx, null);
                        foreach (var fila in (IEnumerable)set!)
                        {
                            w.WriteLine(string.Join(",", props.Select(p => Celda(Texto(p.PropertyInfo!.GetValue(fila))))));
                            filas++;
                        }
                    }
                    tablas.Add(new Tabla(archivo, tipo.ClrType.Name, filas));
                }

                var leeme = zip.CreateEntry("LEEME.txt");
                using (var w = new StreamWriter(leeme.Open(), new UTF8Encoding(true)))
                {
                    w.WriteLine("Datos completos del negocio exportados desde Kirkenta");
                    w.WriteLine($"Fecha: {DateTime.Now:dd/MM/yyyy HH:mm}");
                    w.WriteLine();
                    w.WriteLine("Cada archivo .csv es una tabla y se abre con Excel. La primera fila trae los nombres de las columnas.");
                    w.WriteLine("Fechas en formato año-mes-día (2026-11-02 14:30:00) y números con punto decimal (1234.50).");
                    w.WriteLine("Las columnas que terminan en «Id» unen una tabla con otra (por ejemplo, VentaId en las líneas de venta).");
                    w.WriteLine("Por seguridad no se incluyen contraseñas ni llaves de acceso.");
                    w.WriteLine();
                    foreach (var t in tablas) w.WriteLine($"{t.Archivo,-40} {t.Filas,10:N0} filas");
                }
            }
            finally { ctx.ChangeTracker.QueryTrackingBehavior = anterior; }
            return tablas;
        }

        private static string Texto(object? v) => v switch
        {
            null => "",
            DateTime d => d.TimeOfDay == TimeSpan.Zero ? d.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) : d.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
            DateOnly d => d.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            bool b => b ? "Sí" : "No",
            IFormattable f => f.ToString(null, CultureInfo.InvariantCulture),
            byte[] => "",
            _ => v.ToString() ?? ""
        };

        private static string Celda(string s) =>
            s.IndexOfAny(new[] { ',', '"', '\n', '\r' }) >= 0 ? "\"" + s.Replace("\"", "\"\"") + "\"" : s;
    }
}
