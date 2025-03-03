using System;
using System.Collections.Generic;
using System.Text;

namespace API.BUK.DTO
{
    public class NonWorkedHours
    {
        public int month { get; set; }
        public int year { get; set; }
        public double hours { get; set; }
        public int employee_id { get; set; }
        public int type_id { get; set; }

        public override string ToString()
        {
            return "Mes: " + month +
                " Año: " + year +
                " id: " + type_id +
                " hora(s): " + hours +
                " Id empleado: " + employee_id;
        }
    }
}
