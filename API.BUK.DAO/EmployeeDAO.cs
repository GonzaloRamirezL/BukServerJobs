using API.BUK.DTO;
using API.BUK.DTO.Filters;
using API.BUK.IDAO;
using API.Helpers;
using API.Helpers.VM;
using System;

namespace API.BUK.DAO
{
    public class EmployeeDAO :BUKDAO, IEmployeeDAO
    {
        public PaginatedResponse<Employee> Get(PaginatedEmployeeFilter filter, SesionVM empresa)
        {
            var result = new RestConsumer(BaseAPI.BUK, empresa.Url, empresa.BukKey, empresa).GetResponse<PaginatedResponse<Employee>, object>("employees?page_size=" + filter.page_size + "&page=" + filter.page, new object{} );
            if (result == null)
            {
                throw new Exception("No response from BUK");
            }
            return result;
        }

        
    }
}
