using API.BUK.DTO;
using API.BUK.DTO.Filters;
using API.GV.DTO;
using API.Helpers.VM;
using System.Collections.Generic;

namespace API.BUK.IDAO
{
    public interface IPermissionDAO : IBUKDAO
    {
        /// <summary>
        /// Obtiene un listado paginado de permisos en el formato utilizado por BUK
        /// </summary>
        PaginatedResponse<Permission> GetPermissions(PaginatedAbsenceFilter filter, SesionVM empresa);

        /// <summary>
        /// Obtiene un listado de los tipos de permisos existentes en BUK
        /// </summary>
        PaginatedResponse<PermissionType> GetPermissionTypes(PaginatedAbsenceFilter paginatedAbsenceFilter, SesionVM sesionActiva);

        /// <summary>
        /// Crea un permiso en Buk.
        /// </summary>
        ApiResponse SendPermission(SesionVM empresa, PermissionToAdd permission);

        /// <summary>
        /// Elimina un permiso en Buk
        /// </summary>
        bool DeletePermission(SesionVM session, List<int> userIdsToDelete, FechasProcesamientoVM fechas);
    }
}
