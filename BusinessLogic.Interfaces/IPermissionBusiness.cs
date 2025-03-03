using API.BUK.DTO;
using API.GV.DTO;
using API.Helpers.VM;
using BusinessLogic.Interfaces.VM;
using System;
using System.Collections.Generic;
using System.Text;

namespace BusinessLogic.Interfaces
{
    public interface IPermissionBusiness
    {
        /// <summary>
        /// Obtiene los permisos desde BUK para el periodo solicitado
        /// </summary>        
        List<Permission> GetPermissions(DateTime startDate, DateTime endDate, SesionVM sesionActiva, CompanyConfiguration companyConfiguration);

        /// <summary>
        /// Obtiene los tipos de permiso disponibles en BUK
        /// </summary>
        List<PermissionType> GetPermissionTypes(SesionVM empresa, CompanyConfiguration companyConfiguration);

        /// <summary>
        /// Genera una lista de permisos que coincidan con algun tipo de permiso válido en BUK.
        /// </summary>
        List<Permission> GetPermissionWithMatch(List<Permission> permissions, List<PermissionType> permissionTypesBuk, SesionVM empresa, CompanyConfiguration companyConfiguration);

        /// <summary>
        /// Crea permisos en Buk
        /// </summary>
        void SendPermissions(SesionVM session, CompanyConfiguration companyConfiguration, List<PermissionToAdd> permissions);

        /// <summary>
        /// Elimina permisos en buk
        /// </summary>
        void DeletePermissions(SesionVM session, CompanyConfiguration companyConfiguration, List<int> userIdsToDelete, FechasProcesamientoVM fechas);
    }
}
