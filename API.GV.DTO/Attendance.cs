using System;
using System.Collections.Generic;
using System.Text;

namespace API.GV.DTO
{
    public class Attendance
    {
        public virtual List<CalculatedUser> Users { get; set; }
        public virtual List<CompanyExtraTimeValues> ExtraTimeValues { get; set; }
    }
}
