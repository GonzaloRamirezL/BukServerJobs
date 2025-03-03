using System;
using System.Collections.Generic;
using System.Text;

namespace API.GV.DTO.Filters
{
    public class TimeOffToAdd
    {
        public string UserIdentifier { get; set; }
        public string TimeOffTypeId { get; set; }
        public string StartDate { get; set; }
        public string EndDate { get; set; }
        public string CreatedByIdentifier { get; set; }
        public string Description { get; set; }
        public string StartTime { get; set; }
        public string EndTime { get; set; }
        public string Origin { get; set; }
        public string Hours { get; set; }

        public override string ToString()
        {
            return "Rut: " + this.UserIdentifier + " Fecha Inicio: " + this.StartDate + " " + this.StartTime + " Fecha Fin: " + this.EndDate + " " + this.EndTime + " Tipo: " + this.TimeOffTypeId + " Descripcion: " + this.Description;
        }
    }
}
