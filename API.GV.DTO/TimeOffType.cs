using System;
using System.Collections.Generic;
using System.Text;

namespace API.GV.DTO
{
    public class TimeOffType
    {
        public string Id { get; set; }
        public string Description { get; set; }
        public string Status { get; set; }
        public string IsPayable { get; set; }
        public string ShowsPunch { get; set; }
        public string ExternalId { get; set; }
        public string IsParcial { get; set; }
        public string LengthInHours { get; set; }
        public string AllowsPunch { get; set; }
        public string IsByHours { get; set; }
        public string DiscountsWorkedHours { get; set; }
        public string CountsWorkedHours { get; set; }

    }
}
