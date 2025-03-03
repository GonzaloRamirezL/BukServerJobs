namespace API.BUK.DTO
{
    public class AssignedItem
    {
        public int id { get; set; }
        public Item item { get; set; }
        public string start_date { get; set; }
        public object end_date { get; set; }
        public object description { get; set; }
    }
}
