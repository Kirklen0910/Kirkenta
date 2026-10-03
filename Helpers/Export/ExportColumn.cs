namespace Kirkenta.Helpers.Export
{
    public class ExportColumn<T>
    {
        public string Header { get; set; } = "";
        public Func<T, object?> Value { get; set; } = _ => null;
        public string? Format { get; set; }
        public int Width { get; set; } = 18;

        public ExportColumn(string header, Func<T, object?> value, string? format = null, int width = 18)
        {
            Header = header;
            Value = value;
            Format = format;
            Width = width;
        }
    }
}