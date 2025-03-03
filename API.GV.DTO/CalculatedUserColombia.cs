using System;
using System.Collections.Generic;
using System.Text;

namespace API.GV.DTO
{
    public class CalculatedUserColombia : CalculatedUser
    {
        public new List<TimeIntervalColombia> PlannedInterval { get; set; }


        public string NonCompensatedSundayDiurnalSurchargeHours { get; set; }
        public string NonCompensatedHolidayDiurnalSurchargeHours { get; set; }

        public string NonCompensatedSundayNocturnalSurchargeHours { get; set; }
        public string NonCompensatedHolidayNocturnalSurchargeHours { get; set; }

        public string CompensatedSundayDiurnalSurchargeHours { get; set; }
        public string CompensatedSundayNocturnalSurchargeHours { get; set; }

        public string CompensatedHolidayDiurnalSurchargeHours { get; set; }
        public string CompensatedHolidayNocturnalSurchargeHours { get; set; }

        public string OrdinaryNocturnalSurchargeHours { get; set; }

        public List<CompensatedDay> Compensated { get; set; }
        public string CompensatedDaysAmount { get; set; }
        public string CompensatedSundaysAmount { get; set; }
        public string CompensatedHolidaysAmount { get; set; }

        public string TotalOrdinaryDiurnalOvertime { get; set; }
        public string TotalOrdinaryNocturnalOvertime { get; set; }

        public string TotalSundayDiurnalOvertime { get; set; }
        public string TotalSundayNocturnalOvertime { get; set; }

        public string TotalHolidayDiurnalOvertime { get; set; }
        public string TotalHolidayNocturnalOvertime { get; set; }

        public string TotalPlannedTime { get; set; }
    }
}
