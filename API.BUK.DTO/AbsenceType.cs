using System;
using System.Collections.Generic;
using System.Text;

namespace API.BUK.DTO
{
    public class AbsenceType
    {
        public int id { get; set; }
        public string kind { get; set; }
        public string name { get; set; }
        public string description { get; set; }
        public string code { get; set; }
        public bool with_pay { get; set; }
        public bool requestable { get; set; }
        public bool editable { get; set; }
        public string created_at { get; set; }
        public string updated_at { get; set; }
    }
}
