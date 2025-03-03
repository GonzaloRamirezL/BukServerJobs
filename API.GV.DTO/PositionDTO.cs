using System;
using System.Collections.Generic;
using System.Text;

namespace API.GV.DTO
{
    public class PositionDTO
    {
        public bool IsPriority { get; set; }
        public bool IsCritical { get; set; }
        public string Description { get; set; }
    }
}
