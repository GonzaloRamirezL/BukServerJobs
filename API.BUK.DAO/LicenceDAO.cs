using API.BUK.DTO;
using API.BUK.DTO.Filters;
using API.BUK.IDAO;
using API.GV.DTO;
using API.Helpers;
using API.Helpers.Commons;
using API.Helpers.VM;
using System;
using System.Collections.Generic;
using System.Text;

namespace API.BUK.DAO
{
    public class LicenceDAO :BUKDAO, ILicenceDAO
    {
        public PaginatedResponse<Licence> GetLicences(PaginatedAbsenceFilter filter, SesionVM empresa)
        {
            string uri = "absences/licence?from=" + filter.from + "&to=" + filter.to;
           

            if (filter.page_size > 0)
            {
                uri += "&page_size=" + filter.page_size;
            }
            var result = new RestConsumer(BaseAPI.BUK, empresa.Url, empresa.BukKey, empresa).GetResponse<PaginatedResponse<Licence>, object>(uri, new object { });
            if (result == null)
            {
                throw new Exception("No response from BUK");
            }
            return result;
        }

        public PaginatedResponse<LicenceType> GetLicenceTypes(PaginatedAbsenceFilter filter, SesionVM empresa)
        {
            string uri = "absences/licence/types";

            if (filter.page_size > 0)
            {
                uri += "?page_size=" + filter.page_size;
            }

            var result = new RestConsumer(BaseAPI.BUK, empresa.Url, empresa.BukKey, empresa).GetResponse<PaginatedResponse<LicenceType>, object>(uri, new object { });

            if (result == null)
            {
                throw new Exception("No response from BUK");
            }

            return result;
        }

        public ApiResponse SendLicence(SesionVM session, LicenceToSend license)
        {
            string uri = "absences/licence";
            ApiResponse response = new RestConsumer(BaseAPI.BUK, session.Url, session.BukKey, session).PostResponse<ApiResponse, LicenceToSend>(uri, license);

            return response ?? throw new Exception("No response from BUK");
        }

        public bool DeleteLicence(SesionVM session, List<int> userIdsToDelete, FechasProcesamientoVM fechas)
        {
            string uri = "absences/licence";
            string ids = String.Join(',', userIdsToDelete);
            ids = ids.Replace(",", "%2C");
            uri += "?employee_ids=" + ids;
            uri += "&start_date=" +
                DateTimeHelper.parseToBUKFormat(fechas.PermisosStartDate) + "&end_date=" + DateTimeHelper.parseToBUKFormat(fechas.PermisosEndDate);

            var response = new RestConsumer(BaseAPI.BUK, session.Url, session.BukKey, session).DeleteResponse<EliminationResponse, Licence>(uri, null);

            if (response == null)
            {
                throw new Exception("No response from BUK");
            }

            return response.deleted;
        }
    }
}
