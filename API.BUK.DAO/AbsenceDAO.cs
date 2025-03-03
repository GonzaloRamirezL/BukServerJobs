using API.BUK.DTO;
using API.BUK.DTO.Filters;
using API.BUK.IDAO;
using API.Helpers;
using API.Helpers.VM;
using System;
using System.Collections.Generic;
using System.Text;

namespace API.BUK.DAO
{
    public class AbsenceDAO:BUKDAO, IAbsenceDAO
    {
        public PaginatedResponse<AbsenceType> GetTypes(PaginatedFilter filter, SesionVM empresa, string macroType)
        {
            string uri = "absences/" + macroType + "/types";


            if (filter.page_size > 0)
            {
                uri += "?page_size=" + filter.page_size;
            }
            var result = new RestConsumer(BaseAPI.BUK, empresa.Url, empresa.BukKey, empresa).GetResponse<PaginatedResponse<AbsenceType>, object>(uri, new object { });
            if (result == null)
            {
                throw new Exception("No response from BUK");
            }
            return result;
        }

        public PaginatedResponse<Absence> GetAbsences(PaginatedAbsenceFilter filter, SesionVM empresa)
        {
            string uri = "absences?from=" + filter.from + "&to=" + filter.to;
            if (!string.IsNullOrWhiteSpace(filter.type))
            {
                uri += "type=" + filter.type;
            }

            if (filter.page_size > 0 )
            {
                uri += "&page_size=" + filter.page_size;
            }
            var result = new RestConsumer(BaseAPI.BUK, empresa.Url, empresa.BukKey, empresa).GetResponse<PaginatedResponse<Absence>, object>(uri, new object { });
            if (result == null)
            {
                throw new Exception("No response from BUK");
            }
            return result;
        }

        public void AddAbsence(AbsenceToAdd absence, SesionVM empresa)
        {
            string uri = "absences/absence" ;
            var response = new RestConsumer(BaseAPI.BUK, empresa.Url, empresa.BukKey, empresa).PostResponse<object, AbsenceToAdd>(uri, absence);
            if (response == null)
            {
                string a = absence.application_date;
                int b = absence.employee_id;
            }
        }

        public bool DeleteAbsence(AbsencesToDelete absences, SesionVM empresa)
        {

            string uri = "absences/absence";
            string ids = String.Join(',', absences.employees_id);
            ids = ids.Replace(",", "%2C");
            uri += "?employee_ids=" + ids;
            uri += "&start_date=" + absences.start_date + "&end_date=" + absences.end_date;


            var response = new RestConsumer(BaseAPI.BUK, empresa.Url, empresa.BukKey, empresa).DeleteResponse<EliminationResponse, AbsencesToDelete>(uri, absences);
            if (response != null)
            {
                return response.deleted;
            }
            throw new Exception("Absences deletion failed!!!");
        }
        
    }
}
