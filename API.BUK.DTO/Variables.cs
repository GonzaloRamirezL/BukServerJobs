using System;
using System.Collections.Generic;
using System.Text;

namespace API.BUK.DTO
{
    public class Variables
    {
        public int mes { get; set; }
        public int ano { get; set; }
        public int identificador_interno_de_ellos { get; set; }
        public string rut { get; set; }
        public double atrasos { get; set; }
        public double adelantos { get; set; }
        public double descuentosColacion { get; set; }
        public double nwh { get; set; }
        public Dictionary<int, double> overtimes { get; set; }
        public List<AbsenceToAdd> ausencias { get; set; }
        public DateTime ausenciasDesde { get; set; }
        public DateTime ausenciasHasta { get; set; }
        public List<NonWorkedHours> nonWorkedHoursByEmployees { get; set; }
        public List<Overtime> overtimesByEmployees { get; set; }
        public List<int> absenceses_sheets_id { get; set; }
    }
}
