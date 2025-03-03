using API.BUK.DTO;
using API.BUK.DTO.Consts;
using API.Helpers.Commons;
using API.Helpers.VM;
using BusinessLogic.Interfaces;
using BusinessLogic.Interfaces.VM;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using static Microsoft.Azure.Amqp.Serialization.SerializableType;

namespace BusinessLogic.Implementation
{
    public class EmployeeArt22Business : EmployeeBusiness, IEmployeeBusiness
    {
        public override List<Employee> GetEmployeesForSync(SesionVM Empresa, CompanyConfiguration companyConfiguration, DateTime from, DateTime to)
        {
            List<Employee> employees = base.GetEmployeeCache(Empresa, companyConfiguration);
            employees = CommonHelper.cleanSheets(employees, from, to);

            return employees;
        }
    }
}
