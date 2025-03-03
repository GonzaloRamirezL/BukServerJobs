using API.BUK.DTO;
using API.BUK.DTO.Filters;
using API.Helpers.Commons;
using API.Helpers.VM;
using API.Helpers.VM.Consts;
using BusinessLogic.Interfaces;
using BusinessLogic.Interfaces.VM;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BusinessLogic.Implementation
{
    public class PermissionFridayHalfDayBusiness : PermissionBusiness
    {
        public override List<Permission> GetPermissions(DateTime startDate, DateTime endDate, SesionVM sesionActiva, CompanyConfiguration companyConfiguration)
        {
            List<Permission> permissions = new List<Permission>();
            try
            {
                var permissionResponse = companyConfiguration.PermissionDAO.GetPermissions(new PaginatedAbsenceFilter
                {
                    from = DateTimeHelper.parseToBUKFormat(startDate),
                    to = DateTimeHelper.parseToBUKFormat(endDate),
                    page_size = OperationalConsts.MAXIMUN_REGISTERS_PER_PAGE
                }, sesionActiva);
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

            permissions = permissions.FindAll(p => p.days_count % 1 == 0 || DateTimeHelper.IsFridayTimeOff(p));
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
    }
}
