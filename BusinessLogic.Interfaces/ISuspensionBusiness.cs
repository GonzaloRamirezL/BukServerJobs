using API.BUK.DTO;
using API.Helpers.VM;
using BusinessLogic.Interfaces.VM;
using System;
using System.Collections.Generic;
using System.Text;

namespace BusinessLogic.Interfaces
{
    public interface ISuspensionBusiness
    {
        /// <summary>
        /// Devuelve las suspensiones desde BUK en el periodo solicitado
        /// </summary>        
        /// <returns>
        ///     Listado de suspension
        /// </returns>
        List<Suspension> GetSuspensionsBUK(DateTime startDate, DateTime endDate, SesionVM sesionActiva, CompanyConfiguration companyConfiguration);
    }
}
