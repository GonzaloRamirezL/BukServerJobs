using System;
using System.Collections.Generic;
using System.Text;

namespace API.BUK.DTO
{
    public class AbsencesToDelete
    {
        public List<int> employees_id { get; set; }
        public string start_date { get; set; }
        public string end_date { get; set; }
    }
}
