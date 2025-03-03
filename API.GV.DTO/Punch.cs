using System;
using System.Collections.Generic;
using System.Text;

namespace API.GV.DTO
{
    public class Punch
    {
  

        public string Type { get; set; }

        public string Date { get; set; }

       
        public string Origin { get; set; }
        
        public string JustifiedTimeJustification { get; set; }
        
        public string CommentJustification { get; set; }
        
        public string ResponsableJustification { get; set; }
        
        public string TimeOffTypeJustifaction { get; set; }

        public string UploadDate { get; set; }
        
        public string Day { get; set; }

       
        public string SequenceNumber { get; set; }

        
        public string CompanyId { get; set; }

        
        public string ShiftPunchType { get; set; }
    }
}
