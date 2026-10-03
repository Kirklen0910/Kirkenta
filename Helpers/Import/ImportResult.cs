namespace Kirkenta.Helpers.Import
{
    /// <summary>
    /// Resultado de una operación de importación.
    /// Acumula cuántos se crearon, actualizaron, y errores por fila.
    /// </summary>
    public class ImportResult
    {
        public int TotalFilas { get; set; }
        public int Creados { get; set; }
        public int Actualizados { get; set; }
        public int SinCambios { get; set; }
        public int Ignorados { get; set; }
        public List<ImportRowError> Errores { get; set; } = new();
        public List<ImportRowError> Advertencias { get; set; } = new();

        public bool Exitoso => Errores.Count == 0;
        public int TotalProcesados => Creados + Actualizados + SinCambios + Ignorados;

        public void AgregarError(int numeroFila, string mensaje)
        {
            Errores.Add(new ImportRowError { NumeroFila = numeroFila, Mensaje = mensaje });
        }

        public void AgregarAdvertencia(int numeroFila, string mensaje)
        {
            Advertencias.Add(new ImportRowError { NumeroFila = numeroFila, Mensaje = mensaje });
        }

        public string Resumen()
        {
            var partes = new List<string>();
            if (Creados > 0) partes.Add($"{Creados} creados");
            if (Actualizados > 0) partes.Add($"{Actualizados} actualizados");
            if (SinCambios > 0) partes.Add($"{SinCambios} sin cambios");
            if (Ignorados > 0) partes.Add($"{Ignorados} ignorados");
            if (Errores.Count > 0) partes.Add($"{Errores.Count} errores");
            if (Advertencias.Count > 0) partes.Add($"{Advertencias.Count} advertencias");

            return partes.Count > 0 ? string.Join(", ", partes) : "Sin cambios";
        }
    }

    public class ImportRowError
    {
        public int NumeroFila { get; set; }
        public string Mensaje { get; set; } = "";
    }
}