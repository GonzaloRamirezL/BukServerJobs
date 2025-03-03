using API.BUK.DTO;
using API.Helpers.VM;
using BusinessLogic.Interfaces.VM;
using System;
using System.Collections.Generic;
using System.Text;

namespace BusinessLogic.Interfaces
{
    public interface INonWorkedHoursBusiness
    {
        /// <summary>
        /// Agrega una cantidad de horas no trabajadas a un empleado en un periodo en BUK
        /// </summary>        
        /// <returns>
        ///    No retorna nada
        /// </returns>
        void AddNonWorkedHours(NonWorkedHours nwh, SesionVM empresa, CompanyConfiguration companyConfiguration);
    }
}
