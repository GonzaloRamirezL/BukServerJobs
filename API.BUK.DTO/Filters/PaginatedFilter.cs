using System;
using System.Collections.Generic;
using System.Text;

namespace API.BUK.DTO.Filters
{
    public class PaginatedFilter
    {
        
        public string company_id { get; set; }
        public int page_size { get; set; }
        public int page { get; set; }
    }
}
