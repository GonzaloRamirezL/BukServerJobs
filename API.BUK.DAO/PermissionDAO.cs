using API.BUK.DTO;
using API.BUK.DTO.Filters;
using API.BUK.IDAO;
using API.GV.DTO;
using API.Helpers;
using API.Helpers.Commons;
using API.Helpers.VM;
using System;
using System.Collections.Generic;

namespace API.BUK.DAO
{
    public class PermissionDAO :BUKDAO, IPermissionDAO
    {
        private PaginatedResponse<T> GetPermissionsData<T>(PaginatedAbsenceFilter filter, SesionVM empresa)
        {
            string uri = "absences/permission?from=" + filter.from + "&to=" + filter.to;

            if (filter.page_size > 0)
            {
                uri += "&page_size=" + filter.page_size;
            }
            var result = new RestConsumer(BaseAPI.BUK, empresa.Url, empresa.BukKey, empresa).GetResponse<PaginatedResponse<T>, object>(uri, new object { });

            return result ?? throw new Exception("No response from BUK"); ;
        }

        public PaginatedResponse<Permission> GetPermissions(PaginatedAbsenceFilter filter, SesionVM empresa) => GetPermissionsData<Permission>(filter, empresa);

        public PaginatedResponse<Permission> GetPermissionsExtended(PaginatedAbsenceFilter filter, SesionVM empresa) => GetPermissionsData<Permission>(filter, empresa);

        public PaginatedResponse<PermissionType> GetPermissionTypes(PaginatedAbsenceFilter filter, SesionVM empresa)
        {
            string uri = "absences/permission/types";

            if (filter.page_size > 0)
            {
                uri += "?page_size=" + filter.page_size;
            }

            var result = new RestConsumer(BaseAPI.BUK, empresa.Url, empresa.BukKey, empresa).GetResponse<PaginatedResponse<PermissionType>, object>(uri, new object { });
            
            if (result == null)
            {
                throw new Exception("No response from BUK");
            }

            return result;
        }

        public ApiResponse SendPermission(SesionVM session, PermissionToAdd permission)
        {
            string uri = "absences/permission";
            ApiResponse response = new RestConsumer(BaseAPI.BUK, session.Url, session.BukKey, session).PostResponse<ApiResponse, PermissionToAdd>(uri, permission);
           
            return response ?? throw new Exception("No response from BUK");
        }

        public bool DeletePermission(SesionVM session, List<int> userIdsToDelete, FechasProcesamientoVM fechas)
        {
            string uri = "absences/permission";
            string ids = String.Join("%2C", userIdsToDelete);
            uri += "?employee_ids=" + ids;
            uri += "&start_date=" + 
                DateTimeHelper.parseToBUKFormat(fechas.PermisosStartDate) + "&end_date=" + DateTimeHelper.parseToBUKFormat(fechas.PermisosEndDate);

            var response = new RestConsumer(BaseAPI.BUK, session.Url, session.BukKey, session).DeleteResponse<EliminationResponse, Permission>(uri, null);

            if (response == null)
            {
                throw new Exception("No response from BUK");
            }

            return response.deleted;
        }
    }
}
