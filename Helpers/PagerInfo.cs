namespace Kirkenta.Helpers
{
    /// <summary>Datos de paginación para el partial _Pager.</summary>
    public class PagerInfo
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 50;
        public int Total { get; set; }
        public int TotalPages => PageSize <= 0 ? 1 : Math.Max(1, (int)Math.Ceiling(Total / (double)PageSize));

        public PagerInfo() { }

        public PagerInfo(int page, int pageSize, int total)
        {
            PageSize = pageSize;
            Total = total;
            Page = Math.Min(Math.Max(1, page), TotalPages);
        }

        public int Skip => (Page - 1) * PageSize;
    }
}
