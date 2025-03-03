using API.BUK.DTO;
using API.GV.DTO;
using System;
using System.Collections.Generic;
using System.Text;

namespace API.Helpers.Commons
{
    public static class ModelExtensions
    {
        public static string Stringify(this Absence absence)
        {
            return "Type: " + absence.type + " Employee Id: " + absence.employee_id + " Start Date: " + absence.start_date + " End Date: " + absence.end_date + " Status: " + absence.status + " Half Working Day?: " + absence.half_working_day;
        }

        public static string Stringify(this Licence Licence)
        {
            return "Type: " + Licence.licence_type_id + " Employee Id: " + Licence.employee_id + " Start Date: " + Licence.start_date + " End Date: " + Licence.end_date + "Application Date: " +Licence.application_date + "Days: "+Licence.days_count + "Created at: " + Licence.created_at;
        }

        public static string Stringify(this Permission permission)
        {
            return "Type: " + permission.permission_type_id + " Employee Id: " + permission.employee_id + " Start Date: " + permission.start_date + " End Date: " + permission.end_date + "Application Date: " + permission.application_date + "Days: " + permission.days_count + "Created at: " + permission.created_at;
        }
        public static string Stringify(this Vacation vacation)
        {
            return "Employee Id: " + vacation.employee_id + " Start Date: " + vacation.start_date + " End Date: " + vacation.end_date + "Days: " + vacation.working_days;
        }

        public static string Stringify(this Suspension suspension)
        {
            return "Type: " + suspension.suspension_type + " Employee Id: " + suspension.employee_id + " Start Date: " + suspension.start_date + " End Date: " + suspension.end_date + "Created at: " + suspension.created_at + "Updated at: " + suspension.updated_at;
        }

        public static string Stringify(this TimeOffType timeOff)
        {
            return "Type: " + timeOff.Description + " Con Goce de Sueldo: " + timeOff.IsPayable +" Es Parcial: "  + timeOff.IsParcial ;
        }
    }
}
