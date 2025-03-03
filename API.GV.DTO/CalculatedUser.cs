using System;
using System.Collections.Generic;
using System.Text;

namespace API.GV.DTO
{
    public class CalculatedUser
    {

        
        public string GroupId { get; set; }
        
        public string Id { get; set; }
        public string Identifier { get; set; }
        public string Name { get; set; }
        public string LastName { get; set; }
        public string Enabled { get; set; }
        public string GroupDescription { get; set; }
        
        public string PositionId { get; set; }
        public string PositionDescription { get; set; }
       
        public string GracePeriod { get; set; }
        
        public string UsesGracePeriod { get; set; }
        
        public string WeeklyWorkingHoursCodeId { get; set; }
        
        public string ProfileId { get; set; }
        public string Email { get; set; }
        
        public string GracePeriodDisplay { get; set; }
        public string CustomColumn1 { get; set; }
        public string CustomColumn2 { get; set; }
        public string CustomColumn3 { get; set; }
        
        public string TimeZone { get; set; }
       
        public string IsHiddenForReports { get; set; }
        public List<TimeInterval> PlannedInterval { get; set; }
    }
}
