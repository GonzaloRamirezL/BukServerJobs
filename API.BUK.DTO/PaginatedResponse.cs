using System;
using System.Collections.Generic;
using System.Text;

namespace API.BUK.DTO
{
    public class PaginatedResponse<T>
    {
        public Pagination pagination { get; set; }
        public List<T> data { get; set; }
    }
}
