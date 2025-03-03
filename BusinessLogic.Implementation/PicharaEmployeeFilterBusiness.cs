using API.BUK.DTO;
using API.BUK.DTO.Consts;
using API.Helpers.Commons;
using API.Helpers.VM;
using BusinessLogic.Interfaces;
using BusinessLogic.Interfaces.VM;
using System;
using System.Collections.Generic;
using System.Text;

namespace BusinessLogic.Implementation
{
    public class PicharaEmployeeFilterBusiness : EmployeeBusiness, IEmployeeBusiness
    {
        public override List<Employee> GetEmployeesForSync(SesionVM Empresa, CompanyConfiguration companyConfiguration, DateTime from, DateTime to)
        {
            List<Employee> employees = base.GetEmployeeCache(Empresa, companyConfiguration);
            employees = CommonHelper.cleanSheets(employees, from, to);
            employees = employees.FindAll(x => x.current_job != null
            && (x.current_job.working_schedule_type == WorkingScheduleType.OrdinariaArt22 || x.current_job.working_schedule_type.IsNullOrEmpty()));

            return employees;
        }
    }
}
