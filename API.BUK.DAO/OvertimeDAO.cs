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
    public class OvertimeDAO : BUKDAO, IOvertimeDAO
    {
        public void AddOverTime(Overtime overtime, SesionVM empresa)
        {
            string uri = "attendances/overtime";
            var result = new RestConsumer(BaseAPI.BUK, empresa.Url, empresa.BukKey, empresa).PutResponse<object, Overtime>(uri, overtime);
        }

        public PaginatedResponse<OvertimeType> GetOvertimeTypes(PaginatedFilter filter, SesionVM empresa)
        {
            string uri = "attendances/overtime/types";


            if (filter.page_size > 0)
            {
                uri += "?page_size=" + filter.page_size;
            }
            var result = new RestConsumer(BaseAPI.BUK, empresa.Url, empresa.BukKey, empresa).GetResponse<PaginatedResponse<OvertimeType>, object>(uri, new object { });
            if (result == null)
            {
                throw new Exception("No response from BUK");
            }
            return result;
        }
    }
}
