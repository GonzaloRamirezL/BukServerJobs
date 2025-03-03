using System;
using System.Collections.Generic;
using System.Text;

namespace API.GV.DTO
{
    public class TimeOffReport
    {
        public string UserIdentifier { get; set; }
        
        public string Enabled { get; set; }
        
        public string IsParcial { get; set; }
       
        public string LengthInHours { get; set; }
    }
}
