using System;
using System.Collections.Generic;
using System.Text;

namespace API.BUK.DTO
{
    public class VacationToSend : Vacation
    {
        public string @type { get; set; }

        public int percent_day { get; set; }
    }
}
