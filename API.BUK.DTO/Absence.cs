namespace API.BUK.DTO
{
    public class Absence
    {
        public int id { get; set; }
        public int employee_id { get; set; }
        public string type { get; set; }
        public string status { get; set; }
        public string start_date { get; set; }
        public string end_date { get; set; }
        public bool half_working_day { get; set; }
        public string created_at { get; set; }
        public string updated_at { get; set; }
        public int permission_type_id { get; set; }
        public double day_percent { get; set; }
        public bool paid { get; set; }

    }
}
