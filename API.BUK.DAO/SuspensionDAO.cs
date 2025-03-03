using API.BUK.DTO;
using API.BUK.DTO.Filters;
using API.BUK.IDAO;
using API.GV.DTO;
using API.Helpers;
using API.Helpers.VM;
using System;

namespace API.BUK.DAO
{
    public class SuspensionDAO : BUKDAO, ISuspensionDAO
    {
        public PaginatedResponse<Suspension> GetSuspensions(PaginatedAbsenceFilter filter, SesionVM empresa)
        {
            string uri = "job_suspensions?start_date=" + filter.from + "&end_date=" + filter.to;
            if (!string.IsNullOrWhiteSpace(filter.type))
            {
                uri += "suspension_type=" + filter.type;
            }

            if (filter.page_size > 0)
            {
                uri += "&page_size=" + filter.page_size;
            }
            var result = new RestConsumer(BaseAPI.BUK, empresa.Url, empresa.BukKey, empresa).GetResponse<PaginatedResponse<Suspension>, object>(uri, new object { });
            if (result == null)
            {
                throw new Exception("No response from BUK");
            }
            return result;
        }
    }
}
