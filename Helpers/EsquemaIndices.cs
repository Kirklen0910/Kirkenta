using Kirkenta.Data;
using Kirkenta.Models;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace Kirkenta.Helpers
{
    /// <summary>
    /// Crea (solo si faltan) índices de rendimiento sobre las columnas que más se filtran
    /// en listados y reportes. Es idempotente: consulta information_schema antes de crear y
    /// nunca modifica ni borra datos. Si un índice falla, lo registra y sigue con el siguiente.
    /// </summary>
    public static class EsquemaIndices
    {
        private static readonly (Type Entidad, string[] Propiedades)[] Indices =
        {
            (typeof(Factura), new[] { "Fecha" }),
            (typeof(Factura), new[] { "ClienteId", "Estado" }),
            (typeof(Factura), new[] { "Estado", "FechaVencimiento" }),
            (typeof(Venta), new[] { "Fecha" }),
            (typeof(Venta), new[] { "ClienteId" }),
            (typeof(DetalleFactura), new[] { "FacturaId" }),
            (typeof(DetalleFactura), new[] { "ProductoId" }),
            (typeof(DetalleVenta), new[] { "VentaId" }),
            (typeof(DetalleVenta), new[] { "ProductoId" }),
            (typeof(MovimientoFinanciero), new[] { "Fecha" }),
            (typeof(MovimientoFinanciero), new[] { "CuentaId", "Fecha" }),
            (typeof(MovimientoFinanciero), new[] { "Origen", "OrigenId" }),
            (typeof(MovimientoInventario), new[] { "Fecha" }),
            (typeof(ActividadUsuario), new[] { "Fecha" }),
            (typeof(ActividadUsuario), new[] { "UsuarioId", "Fecha" }),
        };

        public static int Aplicar(ApplicationDbContext db, Action<string>? log = null)
        {
            var conexion = db.Database.GetDbConnection();
            var abrir = conexion.State != ConnectionState.Open;
            if (abrir) conexion.Open();
            int creados = 0;
            try
            {
                var existentes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                using (var cmd = conexion.CreateCommand())
                {
                    cmd.CommandText = "SELECT DISTINCT TABLE_NAME, INDEX_NAME FROM information_schema.STATISTICS WHERE TABLE_SCHEMA = DATABASE()";
                    using var rd = cmd.ExecuteReader();
                    while (rd.Read()) existentes.Add(rd.GetString(0) + "|" + rd.GetString(1));
                }

                foreach (var (entidad, propiedades) in Indices)
                {
                    try
                    {
                        var tipo = db.Model.FindEntityType(entidad);
                        var tabla = tipo?.GetTableName();
                        if (tipo == null || tabla == null) continue;

                        var columnas = new List<string>();
                        foreach (var nombre in propiedades)
                        {
                            var col = tipo.FindProperty(nombre)?.GetColumnName();
                            if (col == null) { columnas.Clear(); break; }
                            columnas.Add(col);
                        }
                        if (columnas.Count == 0) continue;

                        var nombreIndice = $"IX_perf_{tabla}_{string.Join("_", propiedades)}";
                        if (nombreIndice.Length > 64) nombreIndice = nombreIndice.Substring(0, 64);
                        if (existentes.Contains(tabla + "|" + nombreIndice)) continue;

                        using var cmd = conexion.CreateCommand();
                        cmd.CommandText = $"CREATE INDEX `{nombreIndice}` ON `{tabla}` ({string.Join(", ", columnas.Select(c => $"`{c}`"))})";
                        cmd.ExecuteNonQuery();
                        creados++;
                    }
                    catch (Exception ex)
                    {
                        log?.Invoke($"Índice {entidad.Name}({string.Join(",", propiedades)}) no se creó: {ex.Message}");
                    }
                }
            }
            finally
            {
                if (abrir) conexion.Close();
            }
            return creados;
        }
    }
}
