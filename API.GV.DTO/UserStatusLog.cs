using System;
using System.Collections.Generic;
using System.Text;

namespace API.GV.DTO
{
    public class UserStatusLog
    {
        public string Identifier { get; set; }
        public List<ActivePeriod> ActivePeriods { get; set; }
    }
}
