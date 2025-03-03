using API.BUK.DTO;
using API.BUK.DTO.Filters;
using API.Helpers.Commons;
using API.Helpers.VM;
using API.Helpers.VM.Consts;
using BusinessLogic.Interfaces;
using BusinessLogic.Interfaces.VM;
using System;
using System.Collections.Generic;
using System.Text;

namespace BusinessLogic.Implementation
{
    public class CompanyBusiness : ICompanyBusiness
    {
        public List<Company> GetCompanies(SesionVM sesionActiva, CompanyConfiguration companyConfiguration)
        {
            List<Company> companies = new List<Company>();
            try
            {
                var companiesResponse = companyConfiguration.CompanyDAO.GetCompanies(new PaginatedFilter
                {
                    page_size = OperationalConsts.MAXIMUN_REGISTERS_PER_PAGE
                }, sesionActiva);
                if (!CollectionsHelper.IsNullOrEmpty<Company>(companiesResponse.data))
                {
                    companies.AddRange(companiesResponse.data);
                }
                while (companiesResponse.pagination != null && !string.IsNullOrWhiteSpace(companiesResponse.pagination.next))
                {
                    companiesResponse = companyConfiguration.CompanyDAO.GetNext<Company>(companiesResponse.pagination.next, sesionActiva.Url, sesionActiva.BukKey, sesionActiva);
                    if (!CollectionsHelper.IsNullOrEmpty<Company>(companiesResponse.data))
                    {
                        companies.AddRange(companiesResponse.data);
                    }
                }
            }
            catch (Exception ex)
            {
                InsightHelper.logException(ex, sesionActiva.Empresa);
                FileLogHelper.log(LogConstants.general, LogConstants.get, "", "ERROR AL TRAER COMPANIES - " + ex.ToString(), null, sesionActiva);
                throw new Exception("Incomplete data from BUK");
            }

            return companies;
        }
    }
}
