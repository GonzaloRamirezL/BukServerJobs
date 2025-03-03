using System;
using System.Collections.Generic;
using System.Text;

namespace API.BUK.DTO
{
    public class Vacation
    {
        public int id { get; set; }
        public int employee_id { get; set; }
        public double working_days { get; set; }
        public string start_date { get; set; }
        public string end_date { get; set; }
        public string status { get; set; }
    }
}
