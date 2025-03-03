using System;
using System.Collections.Generic;
using System.Text;

namespace API.Helpers.VM
{
    public class UserStatusLogCalculatedVM
    {
        public string Identifier { get; set; }
        public List<ActivePeriodCalculatedVM> ActivePeriods { get; set; }
    }
}
