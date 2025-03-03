using API.BUK.DTO;
using API.Helpers.VM;
using BusinessLogic.Interfaces.VM;
using System;
using System.Collections.Generic;
using System.Text;

namespace BusinessLogic.Interfaces
{
    public interface IEmployeeBusiness
    {
        /// <summary>
        /// Devuelve los empleados que se deben utilizar en la sincronización
        /// </summary>        
        /// <returns>
        ///     Listado de employees
        /// </returns>
        List<Employee> GetEmployeesForSync(SesionVM Empresa, CompanyConfiguration companyConfiguration, DateTime from, DateTime to);
    }
}
