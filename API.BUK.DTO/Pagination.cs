using System;
using System.Collections.Generic;
using System.Text;

namespace API.BUK.DTO
{
    public class Pagination
    {
        public string next { get; set; }
        public string previous { get; set; }
        public int count { get; set; }
        public int total_pages { get; set; }
    }
}
