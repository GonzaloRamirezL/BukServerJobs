namespace API.BUK.DTO
{
    public class ItemToAssign
    {
        public int employee_id { get; set; }
        public int item_id { get; set; }
        public string start_date { get; set; }
        public string end_date { get; set; }
        public string description { get; set; }
        public double amount { get; set; }
        public int? advance_payment_day { get; set; }
        public bool? overwrite_existing_assign { get; set; }
    }
}
