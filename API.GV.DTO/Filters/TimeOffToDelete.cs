using System;
using System.Collections.Generic;
using System.Text;

namespace API.GV.DTO.Filters
{
    public class TimeOffToDelete
    {
        public string UserIdentifier { get; set; }        
        public string Start { get; set; }
        public string End { get; set; }
        public string TypeIdentifier { get; set; }
        public string Description { get; set; }
        public override string ToString()
        {
            return "Rut: " + this.UserIdentifier + " Inicio: " + this.Start + " Fin: " + this.End + " Tipo: " + this.TypeIdentifier + " Descripcion: " + this.Description;
        }
    }
}
