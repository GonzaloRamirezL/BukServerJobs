using System;
using System.Collections.Generic;
using System.Text;

namespace API.BUK.DTO.Filters
{
    public class PaginatedVacationFilter:PaginatedFilter
    {
        public string date { get; set; }
        public string end_date { get; set; }
    }
}
