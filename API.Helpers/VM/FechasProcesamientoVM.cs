using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace API.Helpers.VM
{
    public class FechasProcesamientoVM
    {
        public DateTime InasistenciasStartDate { get; set; }
        public DateTime InasistenciasEndDate { get; set; }
        public DateTime HorasExtrasStartDate { get; set; }
        public DateTime HorasExtrasEndDate { get; set; }
        public DateTime HorasNoTrabajadasStartDate { get; set; }
        public DateTime HorasNoTrabajadasEndDate { get; set; }
        public DateTime PermisosStartDate { get; set; }
        public DateTime PermisosEndDate { get; set; }

        public List<DateTime> ToList()
        {
            List < DateTime > lista = new List<DateTime> { this.InasistenciasStartDate, this.InasistenciasEndDate, this.HorasExtrasStartDate, this.HorasExtrasEndDate, this.HorasNoTrabajadasStartDate, this.HorasNoTrabajadasEndDate, this.PermisosStartDate, this.PermisosEndDate };
            lista.Sort();
            return lista;
        }

        public override string ToString()
        {
            return "Inicio permisos: " + PermisosStartDate +
                " Fin permisos: " + PermisosEndDate +
                "Inicio horas no trabajadas: " + HorasNoTrabajadasStartDate +
                " Fin horas no trabajadas: " + HorasNoTrabajadasEndDate +
                "Inicio horas extra: " + HorasExtrasStartDate +
                " Fin horas extra: " + HorasExtrasEndDate +
                "Inicio ausencias: " + InasistenciasStartDate +
                " Fin ausencias: " + InasistenciasEndDate;
        }
    }
}
