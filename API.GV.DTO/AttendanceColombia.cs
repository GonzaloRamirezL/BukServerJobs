using System;
using System.Collections.Generic;
using System.Text;

namespace API.GV.DTO
{
    public class AttendanceColombia : Attendance
    {
        public List<CalculatedUserColombia> Users { get; set; }
        public override List<CompanyExtraTimeValues> ExtraTimeValues { get; set; }
    }
}
