namespace API.BUK.DTO
{
    public  class PermissionToAdd : AbsenceToAdd 
    {
        public string justification { get; set; }
        public bool paid { get; set; }
        public int permission_type_id { get; set; }
        public string end_date { get; set; }
    }
}
