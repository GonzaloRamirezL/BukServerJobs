using API.BUK.DTO;
using API.Helpers.VM;
using BusinessLogic.Interfaces.VM;
using System;
using System.Collections.Generic;
using System.Text;

namespace BusinessLogic.Interfaces
{
    public interface IOvertimeBusiness
    {
        /// <summary>
        /// Devuelve los tipos de horas extras desde BUK
        /// </summary>        
        /// <returns>
        ///     Listado de tipos de horas extras
        /// </returns>
        List<OvertimeType> GetOvertimeTypes(SesionVM sesionActiva, CompanyConfiguration companyConfiguration);

        /// <summary>
        /// Agrega una cantidad de horas extras de un tipo específico a un empleado en un periodo en BUK
        /// </summary>        
        /// <returns>
        ///    No retorna nada
        /// </returns>
        void AddOverTime(Overtime overtime, SesionVM empresa, CompanyConfiguration companyConfiguration);
    }
}
