namespace API.BUK.DTO
{
    public class KpiData
    {
        public int? id { get; set; }
        public int kpi_id { get; set; }
        public int? empresa_id { get; set; }
        public int? area_id { get; set; }
        public int? employee_id { get; set; }
        public decimal value { get; set; }
    }
}
