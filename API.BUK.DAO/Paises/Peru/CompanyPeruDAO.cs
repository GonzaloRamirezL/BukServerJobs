using API.BUK.DTO;
using API.BUK.DTO.Filters;
using API.BUK.IDAO.Paises.Peru;
using API.Helpers;
using API.Helpers.VM;
using System;

namespace API.BUK.DAO.Paises.Peru
{
    public class CompanyPeruDAO : BUKDAO, ICompanyPeruDAO
    {
        public PaginatedResponse<Company> GetCompanies(PaginatedFilter filter, SesionVM empresa)
        {
            string uri = "companies";

            if (filter.page_size > 0)
            {
                uri += "?page_size=" + filter.page_size;
            }
            var result = new RestConsumer(BaseAPI.BUK, empresa.Url, empresa.BukKey, empresa).GetResponse<PaginatedResponse<Company>, object>(uri, new object { });
            if (result == null)
            {
                throw new Exception("No response from BUK");
            }
            return result;
        }
    }
}
