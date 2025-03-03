using API.BUK.DTO;
using API.BUK.DTO.Filters;
using API.BUK.IDAO;
using API.GV.DTO;
using API.Helpers;
using API.Helpers.Commons;
using API.Helpers.VM;
using API.Helpers.VM.Consts;
using BusinessLogic.Interfaces;
using BusinessLogic.Interfaces.VM;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessLogic.Implementation
{
    public class PermissionBusiness :  IPermissionBusiness
    {
        private PaginatedAbsenceFilter GetBaseAbsenceFilter() => new PaginatedAbsenceFilter { page_size = OperationalConsts.MAXIMUN_REGISTERS_PER_PAGE };

        private PaginatedAbsenceFilter GetPaginatedAbsenseFilter(DateTime startDate, DateTime endDate)
        {
            PaginatedAbsenceFilter filter = GetBaseAbsenceFilter();
            filter.from = DateTimeHelper.parseToBUKFormat(startDate);
            filter.to = DateTimeHelper.parseToBUKFormat(endDate);
            return filter;
        }

        public virtual List<Permission> GetPermissions(DateTime startDate, DateTime endDate, SesionVM sesionActiva, CompanyConfiguration companyConfiguration)
        {
            List<Permission> permissions = new List<Permission>();
            try
            {
                var permissionResponse = companyConfiguration.PermissionDAO.GetPermissions(GetPaginatedAbsenseFilter(startDate, endDate), sesionActiva);
                if (!CollectionsHelper.IsNullOrEmpty<Permission>(permissionResponse.data))
                {
                    permissions.AddRange(permissionResponse.data);
                }
                while (permissionResponse.pagination != null && !string.IsNullOrWhiteSpace(permissionResponse.pagination.next))
                {
                    permissionResponse = companyConfiguration.PermissionDAO.GetNext<Permission>(permissionResponse.pagination.next, sesionActiva.Url, sesionActiva.BukKey, sesionActiva);
                    if (!CollectionsHelper.IsNullOrEmpty<Permission>(permissionResponse.data))
                    {
                        permissions.AddRange(permissionResponse.data);
                    }
                }
            }
            catch (Exception ex)
            {

                InsightHelper.logException(ex, sesionActiva.Empresa);
                FileLogHelper.log(LogConstants.timeOff, LogConstants.error_add, "", "Error al obtener permisos desde BUK " + ex.ToString(), null, sesionActiva);
                throw new Exception("Incomplete data from BUK");
            }

            permissions = permissions.FindAll(p => p.days_count % 1 == 0);
            Parallel.ForEach(permissions, p => {
                if (p.end_date == null)
                {
                    DateTime endDate = DateTimeHelper.parseFromBUKFormat(p.start_date);
                    if (p.days_count >= 2)
                    {
                        endDate = endDate.AddDays(p.days_count - 1);
                    }
                    p.end_date = DateTimeHelper.parseToBUKFormat(endDate);
                }
            });

            return permissions;
        }

        public List<PermissionType> GetPermissionTypes(SesionVM sesionActiva, CompanyConfiguration companyConfiguration)
        {
            List<PermissionType> permissionsTypes = new List<PermissionType>();
            try
            {
                var permissionResponse = companyConfiguration.PermissionDAO.GetPermissionTypes(GetBaseAbsenceFilter(), sesionActiva);

                if (!CollectionsHelper.IsNullOrEmpty<PermissionType>(permissionResponse.data))
                {
                    permissionsTypes.AddRange(permissionResponse.data);
                }
                while (permissionResponse.pagination != null && !string.IsNullOrWhiteSpace(permissionResponse.pagination.next))
                {
                    permissionResponse = companyConfiguration.PermissionDAO.GetNext<PermissionType>(permissionResponse.pagination.next, sesionActiva.Url, sesionActiva.BukKey, sesionActiva);
                    if (!CollectionsHelper.IsNullOrEmpty<PermissionType>(permissionResponse.data))
                    {
                        permissionsTypes.AddRange(permissionResponse.data);
                    }
                }
            }
            catch (Exception ex)
            {
                InsightHelper.logException(ex, sesionActiva.Empresa);
                FileLogHelper.log(LogConstants.timeOff, LogConstants.error_add, "", "Error al obtener tipos de permiso desde BUK " + ex.ToString(), null, sesionActiva);
                throw new Exception("Incomplete data from BUK");
            }

            return permissionsTypes;
        }

        public List<Permission> GetPermissionWithMatch(List<Permission> permissions, List<PermissionType> permissionTypes, SesionVM sesionActiva, CompanyConfiguration companyConfiguration)
        {
            List<Permission> finalPermissions = new List<Permission>();
            try
            {
                foreach (Permission currentPermission in permissions)
                {
                    PermissionType matchedPermissionType = permissionTypes.FirstOrDefault(x => x.id == currentPermission.permission_type_id);
                    if (matchedPermissionType != null)
                    {
                        currentPermission.matched_type = matchedPermissionType.code;
                    }
                    finalPermissions.Add(currentPermission);
                }
            }
            catch (Exception ex)
            {
                InsightHelper.logException(ex, sesionActiva.Empresa);
                FileLogHelper.log(LogConstants.timeOff, LogConstants.error_add, "", "Error al hacer el match entre tipos de permiso y permisos proveninentes de BUK " + ex.ToString(), null, sesionActiva);
                throw new Exception("Incomplete permission match");
            }

            return finalPermissions;
        }

        public void SendPermissions(SesionVM session, CompanyConfiguration companyConfiguration, List<PermissionToAdd> permissions)
        {
            try
            {
                foreach (var permission in permissions)
                {
                    companyConfiguration.PermissionDAO.SendPermission(session, permission);
                }
            }
            catch (Exception ex)
            {
                InsightHelper.logException(ex, session.Empresa);
                throw new Exception("Permission could not be created");
            }
        }

        public void DeletePermissions(SesionVM session, CompanyConfiguration companyConfiguration, List<int> userIdsToDelete, FechasProcesamientoVM fechas)
        {
            try
            {
                if (userIdsToDelete.Any())
                {
                    companyConfiguration.PermissionDAO.DeletePermission(session, userIdsToDelete, fechas);
                }
            }
            catch (Exception ex)
            {
                InsightHelper.logException(ex, session.Empresa);
                throw new Exception("Permission could not be deleted");
            }
        }
    }
}
