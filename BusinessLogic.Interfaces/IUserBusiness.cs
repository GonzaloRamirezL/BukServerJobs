using API.BUK.DTO;
using API.GV.DTO;
using API.Helpers.VM;
using BusinessLogic.Interfaces.VM;
using System;
using System.Collections.Generic;
using System.Text;

namespace BusinessLogic.Interfaces
{
    public interface IUserBusiness
    {
        /// <summary>
        /// Realiza la sincronización del módulo de permisos
        /// </summary>        
        /// <returns>
        ///     No devuelve nada
        /// </returns>
        void Sync(SesionVM Empresa, CompanyConfiguration companyConfiguration, ProcessPeriod periodo);

        /// <summary>
        /// Devuelve los usuarios que se deben utilizar en la sincronización
        /// </summary>        
        /// <returns>
        ///     Listado de users y de empleados
        /// </returns>
        (List<User>, List<Employee>) GetUsersForSync(SesionVM Empresa, CompanyConfiguration companyConfiguration, List<Employee> employees, Operacion module);

        /// <summary>
        /// Desactiva los usuarios en GV
        /// </summary>        
        /// <returns>
        ///     Nada
        /// </returns>
        void DeactivateUsers(List<User> usersToDeactivate, SesionVM Empresa, CompanyConfiguration companyConfiguration);

        /// <summary>
        /// Activa los usuarios en GV
        /// </summary>        
        /// <returns>
        ///     Nada
        /// </returns>
        void ActivateUsers(List<User> usersToActivate, SesionVM Empresa, CompanyConfiguration companyConfiguration);
        /// <summary>
        /// Mueve a los usuarios de grupo
        /// </summary>
        /// <param name="users"></param>
        /// <param name="Empresa"></param>
        /// <param name="companyConfiguration"></param>
        void UpdateUserGroup(List<User> users, SesionVM Empresa, CompanyConfiguration companyConfiguration);
    }
}
