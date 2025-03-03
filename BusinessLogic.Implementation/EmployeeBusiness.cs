using API.BUK.DTO;
using API.BUK.DTO.Filters;
using API.BUK.IDAO;
using API.Helpers;
using API.Helpers.Commons;
using API.Helpers.VM;
using API.Helpers.VM.Consts;
using BusinessLogic.Interfaces;
using BusinessLogic.Interfaces.VM;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace BusinessLogic.Implementation
{
    public class EmployeeBusiness : IEmployeeBusiness
    {
        public EmployeeBusiness()
        {
            CacheFileHelper.CheckDirectory();
            CacheFileHelper.CheckCache();
        }
        /// <summary>
        /// Devuelve los empleados desde BUK
        /// </summary>
        protected List<Employee> GetEmployees(SesionVM Empresa, CompanyConfiguration companyConfiguration)
        {
            List<Employee> employees = new List<Employee>();
            try
            {
                int pagina = 1;
                FileLogHelper.log(LogConstants.general, LogConstants.get, "", "Solicitando pagina " + pagina + " a BUK", null, Empresa);
                var employeesResponse = companyConfiguration.EmployeeDAO.Get(new PaginatedEmployeeFilter()
                {
                    page = 1,
                    page_size = OperationalConsts.MAXIMUN_REGISTERS_PER_PAGE
                }, Empresa);

                if (employeesResponse != null)
                {
                    object _lock = new object();
                    if (!CollectionsHelper.IsNullOrEmpty<Employee>(employeesResponse.data))
                    {
                        employees.AddRange(employeesResponse.data);
                    }
                    while (employeesResponse.pagination != null && !string.IsNullOrWhiteSpace(employeesResponse.pagination.next))
                    {
                        pagina++;
                        FileLogHelper.log(LogConstants.general, LogConstants.get, "", "Solicitando pagina " + pagina + " a BUK", null, Empresa);
                        employeesResponse = companyConfiguration.EmployeeDAO.GetNext<Employee>(employeesResponse.pagination.next, Empresa.Url, Empresa.BukKey, Empresa);
                        if (!CollectionsHelper.IsNullOrEmpty<Employee>(employeesResponse.data))
                        {
                            employees.AddRange(employeesResponse.data);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                InsightHelper.logException(ex, Empresa.Empresa);
                FileLogHelper.log(LogConstants.general, LogConstants.get, "", "ERROR OBTENIENDO EMPLEADOS DESDE BUK", null, Empresa);
                throw new Exception("Incomplete data from BUK");
            }
            return employees;
        }

        public virtual List<Employee> GetEmployeesForSync(SesionVM Empresa, CompanyConfiguration companyConfiguration, DateTime from, DateTime to)
        {
            return CommonHelper.cleanSheets(GetEmployees(Empresa, companyConfiguration), from, to);
        }
        /// <summary>
        /// Verifica el cache y obtiene los empleados
        /// </summary>
        /// <param name="Empresa"></param>
        /// <param name="companyConfiguration"></param>
        /// <returns></returns>
        protected List<Employee> GetEmployeeCache(SesionVM Empresa, CompanyConfiguration companyConfiguration)
        {
            List<Employee> employees = new List<Employee>();
            string jsonFileName = CacheFileHelper.GetFileName(Empresa);
            if (!CacheFileHelper.CheckFile(jsonFileName))
            {
                employees = GetEmployees(Empresa, companyConfiguration);

                CacheFileHelper.WriteToFile(employees, jsonFileName);
            }

            employees = CacheFileHelper.GetCacheContent<List<Employee>>(jsonFileName);


            return employees;
        }
    }
}
