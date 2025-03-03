using API.BUK.DTO;
using API.Helpers.VM;
using BusinessLogic.Interfaces.VM;
using System;
using System.Collections.Generic;
using System.Text;

namespace BusinessLogic.Interfaces
{
    public interface ICompanyBusiness
    {
        List<Company> GetCompanies(SesionVM sesionActiva, CompanyConfiguration companyConfiguration);
    }
}
