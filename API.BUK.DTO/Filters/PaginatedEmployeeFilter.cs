using System;
using System.Collections.Generic;
using System.Text;

namespace API.BUK.DTO.Filters
{
    public class PaginatedEmployeeFilter : PaginatedFilter
    {
        public int status { get; set; }
    }
}
