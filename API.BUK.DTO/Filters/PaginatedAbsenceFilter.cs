using System;
using System.Collections.Generic;
using System.Text;

namespace API.BUK.DTO.Filters
{
    public class PaginatedAbsenceFilter: PaginatedFilter
    {
        public string from { get; set; }
        public string to { get; set; }
        public string type { get; set; }
    }
}
