using System;
using System.Collections.Generic;
using System.Text;

namespace API.GV.DTO
{
    public class SurchargeColombia
    {
        public string OrdinaryNocturnalSurchargeHours { get; set; }

        public string NonCompensatedSundayDiurnalSurchargeHours { get; set; }
        public string NonCompensatedSundayNocturnalSurchargeHours { get; set; }

        public string NonCompensatedHolidayDiurnalSurchargeHours { get; set; }
        public string NonCompensatedHolidayNocturnalSurchargeHours { get; set; }

        public string CompensatedSundayDiurnalSurchargeHours { get; set; }
        public string CompensatedSundayNocturnalSurchargeHours { get; set; }

        public string CompensatedHolidayDiurnalSurchargeHours { get; set; }
        public string CompensatedHolidayNocturnalSurchargeHours { get; set; }
        public string CompensatedSunday { get; set; }
        public string CompensatedHoliday { get; set; }
    }
}
