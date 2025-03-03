using API.BUK.DTO;
using API.GV.DTO;
using API.Helpers.VM;
using BusinessLogic.Interfaces.VM;
using System;
using System.Collections.Generic;
using System.Text;

namespace BusinessLogic.Interfaces
{
    public interface IAttendanceBusiness
    {
        void Sync(SesionVM Empresa, ProcessPeriod periodo, List<PeriodConfiguration> configs, CompanyConfiguration companyConfiguration);

        /// <summary>
        /// Obtiene los libros de asistencia desde GV para los usaurios en Users desde startDate hasta endDate
        /// </summary>
        /// <param name="Empresa"></param>
        /// <param name="users"></param>
        /// <param name="startDate"></param>
        /// <param name="endDate"></param>        
        /// <returns></returns>
        Attendance GetAttendance(SesionVM Empresa, List<User> users, DateTime startDate, DateTime endDate, CompanyConfiguration companyConfiguration);

        /// <summary>
        /// Procesa la data de asistencia de un usuario en un periodo determinado
        /// </summary>
        /// <returns>
        /// Coleccion de datos de usuarios procesados
        /// </returns>
        Variables processUserData(CalculatedUser cUser, User user, List<CompanyExtraTimeValues> ExtraTimeValues, FechasProcesamientoVM fechas, ProcessPeriod periodo, UserStatusLogCalculatedVM userStatusLogs, DateTime? activeUntil);
       
    }
}
