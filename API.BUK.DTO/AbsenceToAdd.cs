using System;

namespace API.BUK.DTO
{
    public class AbsenceToAdd
    {
        public string start_date { get; set; }
        public int days_count { get; set; }
        public string day_percent { get; set; }
        public string application_date { get; set; }
        public int employee_id { get; set; }
        public int absence_type_id { get; set; }
        public DateTime applicationDateTime { get; set; }

    public override string ToString()
        {
            return "Fecha inicio: " + start_date +
                " Fecha aplicación: " + application_date +
                " Hora aplicación: " + applicationDateTime.ToString() +
                " id: " + absence_type_id +
                " día(s): " + days_count +
                " Id empleado: " + employee_id;
        }
    }
}
