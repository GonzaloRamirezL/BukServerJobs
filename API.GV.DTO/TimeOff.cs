using System;

namespace API.GV.DTO
{
    public class TimeOff
    {
        public string TimeOffTypeId { get; set; }
        public string Description { get; set; }
        public string Starts { get; set; }
        public string Ends { get; set; }
        public string TimeOffTypeDescription { get; set; }
        public string UserIdentifier { get; set; }
    }
}
