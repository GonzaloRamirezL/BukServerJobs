using API.BUK.DTO;
using API.BUK.DTO.Consts;
using API.GV.DTO;
using API.Helpers.Commons;
using API.Helpers.VM;
using BusinessLogic.Interfaces;
using BusinessLogic.Interfaces.VM;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BusinessLogic.Implementation
{
    public class UserArt22Business: UserBusiness, IUserBusiness
    {
        public override (List<User>, List<Employee>) GetUsersForSync(SesionVM Empresa, CompanyConfiguration companyConfiguration, List<Employee> employees, Operacion module)
        {
            List<Employee> noArt22 = new List<Employee>();
            List<Employee> Art22 = new List<Employee>();
            foreach (var employee in employees)
            {
                if (employee.custom_attributes != null && employee.custom_attributes.ContainsKey(CustomAtributesEmployee.SincronizarGeovictoria))
                {
                    if (employee.custom_attributes[CustomAtributesEmployee.SincronizarGeovictoria] != null)
                    {
                        if (employee.custom_attributes[CustomAtributesEmployee.SincronizarGeovictoria].ToString() == CustomAtributesEmployee.SiSincronizar)
                        {
                            if (employees.Count(x => x.rut == employee.rut) > 1)
                            {
                                if (employee.status == EmployeeStatus.Activo)
                                {
                                    noArt22.Add(employee);
                                }
                            }
                            else
                            {
                                noArt22.Add(employee);
                            }
                        }
                        else if (employee.custom_attributes[CustomAtributesEmployee.SincronizarGeovictoria].ToString() != CustomAtributesEmployee.NoSincronizar)
                        {
                            if (Empresa.SincronizaArticulos22)
                            {
                                if (employees.Count(x => x.rut == employee.rut) > 1)
                                {
                                    if (employee.status == EmployeeStatus.Activo)
                                    {
                                        noArt22.Add(employee);
                                    }
                                }
                                else
                                {
                                    noArt22.Add(employee);
                                }
                            }
                            else if (employee.current_job == null || employee.current_job.periodicity != BUKArticulos22Job.SinJornada)
                            {
                                if (employees.Count(x => x.rut == employee.rut) > 1)
                                {
                                    if (employee.status == EmployeeStatus.Activo)
                                    {
                                        noArt22.Add(employee);
                                    }
                                }
                                else
                                {
                                    noArt22.Add(employee);
                                }
                            }
                            else
                            {
                                Art22.Add(employee);
                            }
                        }
                        else
                        {
                            Art22.Add(employee);
                        }
                    }

                }
                else
                {
                    if (Empresa.SincronizaArticulos22)
                    {
                        if (employees.Count(x => x.rut == employee.rut) > 1)
                        {
                            if (employee.status == EmployeeStatus.Activo)
                            {
                                noArt22.Add(employee);
                            }
                        }
                        else
                        {
                            noArt22.Add(employee);
                        }
                    }
                    else if (employee.current_job == null || employee.current_job.periodicity != BUKArticulos22Job.SinJornada)
                    {
                        if (employees.Count(x => x.rut == employee.rut) > 1)
                        {
                            if (employee.status == EmployeeStatus.Activo)
                            {
                                noArt22.Add(employee);
                            }
                        }
                        else
                        {
                            noArt22.Add(employee);
                        }
                    }
                    else
                    {
                        Art22.Add(employee);
                    }
                }
            }

            List<long> noArt22Ids = noArt22.Select(a => a.id).ToList();
            List<string> art22Ruts = Art22.Select(a => CommonHelper.rutToGVFormat(a.rut).ToUpper()).ToList();
            List<User> users = GetUsers(Empresa, companyConfiguration);

            users = users.FindAll(u => (u.integrationCode != null && noArt22Ids.Contains(int.Parse(u.integrationCode)))
            || (!string.IsNullOrWhiteSpace(u.Identifier) && !art22Ruts.Contains(u.Identifier.ToUpper())));

            switch (module)
            {
                case Operacion.PERMISOS:
                case Operacion.ASISTENCIA:
                    return (users.FindAll(u => !string.IsNullOrWhiteSpace(u.integrationCode)), noArt22);
                default://Usuarios
                    return (users, noArt22);
            }
        }
    }
}
