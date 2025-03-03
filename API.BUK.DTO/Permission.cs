namespace API.BUK.DTO
{
    public class Permission:Absence
    {
        public string application_date { get; set; }
        public double days_count { get; set; }
        public string justification { get; set; }
        public bool paid { get; set; }
        public int permission_type_id { get; set; }
        public string matched_type { get; set; }
    }
}
