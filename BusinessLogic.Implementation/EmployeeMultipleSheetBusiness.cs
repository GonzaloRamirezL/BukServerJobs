using API.BUK.DTO;
using API.Helpers.VM;
using BusinessLogic.Interfaces;
using BusinessLogic.Interfaces.VM;
using System;
using System.Collections.Generic;
using System.Text;

namespace BusinessLogic.Implementation
{
    public class EmployeeMultipleSheetBusiness : EmployeeBusiness, IEmployeeBusiness
    {
        public override List<Employee> GetEmployeesForSync(SesionVM Empresa, CompanyConfiguration companyConfiguration, DateTime from, DateTime to)
        {
            return GetEmployees(Empresa, companyConfiguration);
        }
    }
}
