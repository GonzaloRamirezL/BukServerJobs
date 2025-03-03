using API.GV.DTO;
using API.Helpers.VM;
using System;
using System.Collections.Generic;
using System.Text;

namespace API.GV.IDAO
{
    public interface IUserStatusLogDAO
    {
        /// <summary>
        /// Devuelve los estados de activación desde la api de GV
        /// </summary>        
        /// <returns>
        ///     El listado de estados de activación de los usuarios solicitados
        /// </returns>
        List<UserStatusLog> GetStatusLog(string users, SesionVM empresa);
    }
}
