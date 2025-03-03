using API.GV.DTO;
using API.GV.DTO.Filters;
using API.Helpers.VM;
using System;
using System.Collections.Generic;
using System.Text;

namespace API.GV.IDAO
{
    public interface IAttendanceDAO
    {
        /// <summary>
        /// Devuelve los libros de asistencia desde la api de GV
        /// </summary>        
        /// <returns>
        ///     Objeto Attendance que contiene el listado de usuarios con sus libros de asistencia del periodo
        /// </returns>
        Attendance Get(AttendanceFilter filter, SesionVM empresa);
    }
}
