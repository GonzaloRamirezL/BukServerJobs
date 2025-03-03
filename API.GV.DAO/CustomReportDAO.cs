using API.GV.DTO.Filters;
using API.Helpers;
using API.Helpers.VM;
using System;

namespace API.GV.DAO
{
    public class CustomReportDAO<T>
    {
        public T Get(CustomReportFilter filter, SesionVM empresa)
        {
            var result = new RestConsumer(BaseAPI.GV, empresa.GvUrl, empresa.GvKey, empresa).PostResponse<T, CustomReportFilter>("CustomReport", filter);
            if (result == null)
            {
                throw new Exception("No response from GV");
            }

            return result;
        }
    }
}
