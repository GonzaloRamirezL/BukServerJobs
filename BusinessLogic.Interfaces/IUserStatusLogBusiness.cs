using API.GV.DTO;
using API.Helpers.VM;
using BusinessLogic.Interfaces.VM;
using System;
using System.Collections.Generic;
using System.Text;

namespace BusinessLogic.Interfaces
{
    public interface IUserStatusLogBusiness
    {
        /// <summary>
        /// Devuelve los estados de activación desde GV
        /// </summary>        
        /// <returns>
        ///     El listado de estados de activación de los usuarios solicitados
        /// </returns>
        List<UserStatusLogCalculatedVM> GetUserStatusLogs(List<User> users, SesionVM Empresa, CompanyConfiguration companyConfiguration);
    }
}
