using API.BUK.DTO;
using API.BUK.DTO.Consts;
using API.GV.DTO;
using API.Helpers.Commons;
using API.Helpers.VM;
using API.Helpers.VM.Consts;
using BusinessLogic.Interfaces;
using BusinessLogic.Interfaces.VM;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BusinessLogic.Implementation
{
    public class UserMultiURLBusiness :UserBusiness, IUserBusiness
    {
        public override void Sync(SesionVM Empresa, CompanyConfiguration companyConfiguration, ProcessPeriod periodo)
        {
            FileLogHelper.log(LogConstants.period, LogConstants.get, "", string.Empty, periodo, Empresa);
            Console.WriteLine("PROCESANDO PERIODO: " + periodo.month);

            DateTime startMetric = DateTime.Now;
            Dictionary<string, string> properties = new Dictionary<string, string>();
            #region Fechas
            DateTime fechaBase = DateTimeHelper.parseFromBUKFormat(periodo.month);
            int lastDay = DateTime.DaysInMonth(fechaBase.Year, fechaBase.Month);
            int dayEndDate = (Empresa.FechaCorte > lastDay) ? lastDay : Empresa.FechaCorte;
            DateTime endDate = new DateTime(fechaBase.Year, fechaBase.Month, dayEndDate);
            DateTime startDate = endDate.AddMonths(-1).AddDays(1);
            if (startDate.Day <= Empresa.FechaCorte && startDate.Month < endDate.Month)
            {
                startDate = startDate.AddDays(Empresa.FechaCorte - startDate.Day + 1);
            }
            properties["EMPRESA"] = Empresa.Empresa;
            properties["startDate"] = DateTimeHelper.parseToGVFormat(startDate);
            properties["endDate"] = DateTimeHelper.parseToGVFormat(endDate);
            #endregion

            FileLogHelper.log(LogConstants.general, LogConstants.get, "", "EMPIEZA SYNCUSER-MULTIURL", null, Empresa);

            FileLogHelper.log(LogConstants.general, LogConstants.get, "", "PIDIENDO EMPLEADOS A BUK", null, Empresa);
            List<Employee> employeesURL1 = companyConfiguration.EmployeeBusiness.GetEmployeesForSync(Empresa, companyConfiguration, startDate, endDate);
            List<Employee> employeesURL2 = companyConfiguration.EmployeeBusiness.GetEmployeesForSync(new SesionVM { Empresa = Empresa.Empresa, Url = Empresa.Url2, BukKey = Empresa.BukKey2}, companyConfiguration, startDate, endDate);
            List<Employee> allEmployees = new List<Employee>();
            allEmployees.AddRange(employeesURL1);
            allEmployees.AddRange(employeesURL2);
            properties["EMPRESA"] = Empresa.Empresa;
            properties["GetEmployees"] = allEmployees.Count.ToString();

            FileLogHelper.log(LogConstants.general, LogConstants.get, "", "PIDIENDO USUARIOS A GV", null, Empresa);
            var persons = GetUsersForSync(Empresa, companyConfiguration, allEmployees, Operacion.USUARIOS);
            List<User> users = persons.Item1;
            allEmployees = persons.Item2;
            List<long> allEmployeesIds = allEmployees.Select(ae => ae.id).ToList();
            employeesURL1 = employeesURL1.FindAll(e => allEmployeesIds.Contains(e.id));
            employeesURL2 = employeesURL2.FindAll(e => allEmployeesIds.Contains(e.id));
            properties["GetUsers"] = users.Count.ToString();

            FileLogHelper.log(LogConstants.general, LogConstants.get, "", "PROCESANDO OPERACIONES A REALIZAR CON USUARIOS", null, Empresa);
            var process = ProcessUsers(employeesURL1, users, Empresa);
            var processTemporales = ProcessUsersTemporales(employeesURL2, users, Empresa);
            process.toEdit.AddRange(processTemporales.toEdit);
            properties["AddUsers"] = process.toAdd.Count.ToString();
            properties["DisableUsers"] = process.toDeactivate.Count.ToString();
            properties["EnableUsers"] = process.toActivate.Count.ToString();
            properties["EditUsers"] = process.toEdit.Count.ToString();

            FileLogHelper.log(LogConstants.general, LogConstants.get, "", "AÑADIENDO USUARIOS A GV", null, Empresa);
            AddUsers(process.toAdd, Empresa, companyConfiguration);

            FileLogHelper.log(LogConstants.general, LogConstants.get, "", "DESACTIVANDO USUARIOS EN GV", null, Empresa);
            DeactivateUsers(process.toDeactivate, Empresa, companyConfiguration);

            FileLogHelper.log(LogConstants.general, LogConstants.get, "", "ACTIVANDO USUARIOS EN GV", null, Empresa);
            ActivateUsers(process.toActivate, Empresa, companyConfiguration);

            FileLogHelper.log(LogConstants.general, LogConstants.get, "", "EDITANDO USUARIOS EN GV", null, Empresa);
            EditUsers(process.toEdit, Empresa, companyConfiguration);

            InsightHelper.logMetric("SyncUsers", startMetric - DateTime.Now, properties);
        }

        protected override UserProcessVM ProcessUsers(List<Employee> employees, List<User> users, SesionVM Empresa)
        {
            UserProcessVM result = new UserProcessVM();
            result.toAdd = new List<User>();
            result.toDeactivate = new List<User>();
            result.toActivate = new List<User>();
            result.toEdit = new List<User>();
            object _lock = new object();

            List<User> usersTemporales = users.FindAll(u => u.Custom1 != null && u.Custom1.ToLower() == UsersMultiUrlConts.Temporales);
            List<User> usersDirectos = users.FindAll(u => u.Custom1 == null || u.Custom1.ToLower() != UsersMultiUrlConts.Temporales);
            employees.AsParallel().ForAll(employee =>
            {
                User user = users.FirstOrDefault(u => (u.integrationCode != null && long.Parse(u.integrationCode) == employee.id) || (u.Identifier != null && (String.Equals(CommonHelper.rutToGVFormat(employee.rut), u.Identifier, StringComparison.OrdinalIgnoreCase))));
                if (user == null)
                {
                    if (employee.first_name.Length > 3 && employee.full_name.Length > 3 && (employee.rut.Length > 7) && (employee.status == EmployeeStatus.Activo))
                    {
                        User newUser = createUserWithStandardValues(employee);
                        newUser.integrationCode = employee.id.ToString();
                        if (!employee.active_since.IsNullOrEmpty())
                        {
                            newUser.ContractDate = DateTimeHelper.parseToGVFormat(DateTimeHelper.parseFromBUKFormat(employee.active_since));
                        }

                        newUser.GroupIdentifier = "";
                        newUser.Enabled = 1;
                        lock (_lock)
                        {
                            result.toAdd.Add(newUser);
                            FileLogHelper.log(LogConstants.general, LogConstants.get, newUser.Identifier, "Marcado para crear", null, Empresa);
                        }
                    }
                }
                else
                {
                    if (user.Enabled.HasValue && (user.Custom1 == null || user.Custom1.ToLower() != UsersMultiUrlConts.Temporales ))
                    {
                        if (user.Enabled.Value == 1 && employee.status != EmployeeStatus.Activo)
                        {
                            if (MustBeDeactivated(employee))
                            {
                                bool wasSyncronized = user.integrationCode == employee.id.ToString();
                                User newUser = createUserWithStandardValues(employee);
                                newUser.Enabled = 0;
                                if (wasSyncronized)
                                {
                                    newUser.integrationCode = employee.id.ToString();
                                    lock (_lock)
                                    {
                                        result.toDeactivate.Add(newUser);
                                        FileLogHelper.log(LogConstants.general, LogConstants.get, newUser.Identifier, "Marcado para desactivar", null, Empresa);
                                    }
                                    if (!AreEqualUserEmployee(employee, user))
                                    {
                                        lock (_lock)
                                        {
                                            result.toEdit.Add(newUser);
                                            FileLogHelper.log(LogConstants.general, LogConstants.get, newUser.Identifier, "Marcado para desactivacion", null, Empresa);
                                        }
                                    }


                                }
                                else
                                {

                                    lock (_lock)
                                    {
                                        result.toDeactivate.Add(newUser);
                                        FileLogHelper.log(LogConstants.general, LogConstants.get, newUser.Identifier, "Marcado para desactivar sin codigo de integracion", null, Empresa);
                                    }
                                    User newUserWithIntegrationCode = copyUserStandardValues(newUser);
                                    newUserWithIntegrationCode.integrationCode = employee.id.ToString();
                                    lock (_lock)
                                    {
                                        result.toEdit.Add(newUserWithIntegrationCode);
                                        FileLogHelper.log(LogConstants.general, LogConstants.get, newUser.Identifier, "Marcado para editar con codigo de integracion (" + newUser.integrationCode + ")", null, Empresa);
                                    }
                                }

                            }

                        }
                        else if (user.Enabled.Value == 0 && employee.status == EmployeeStatus.Activo)
                        {
                            bool wasSyncronized = user.integrationCode == employee.id.ToString();
                            User newUser = createUserWithStandardValues(employee);
                            newUser.Enabled = 1;
                            if (wasSyncronized)
                            {
                                newUser.integrationCode = employee.id.ToString();
                                lock (_lock)
                                {
                                    result.toActivate.Add(newUser);
                                    FileLogHelper.log(LogConstants.general, LogConstants.get, newUser.Identifier, "Marcado para activar", null, Empresa);
                                }
                                if (!AreEqualUserEmployee(employee, user))
                                {
                                    lock (_lock)
                                    {
                                        result.toEdit.Add(newUser);
                                        FileLogHelper.log(LogConstants.general, LogConstants.get, newUser.Identifier, "Marcado para editar despues de activacion", null, Empresa);
                                    }
                                }

                            }
                            else
                            {

                                lock (_lock)
                                {
                                    result.toActivate.Add(newUser);
                                    FileLogHelper.log(LogConstants.general, LogConstants.get, newUser.Identifier, "Marcado para activar sin codigo de integracion", null, Empresa);
                                }
                                User newUserWithIntegrationCode = copyUserStandardValues(newUser);
                                newUserWithIntegrationCode.integrationCode = employee.id.ToString();
                                lock (_lock)
                                {
                                    result.toEdit.Add(newUserWithIntegrationCode);
                                    FileLogHelper.log(LogConstants.general, LogConstants.get, newUser.Identifier, "Marcado para editar con el codigo de integracion (" + newUser.integrationCode + ")", null, Empresa);
                                }
                            }



                        }
                        else if (user.Enabled.Value == 1 && employee.status == EmployeeStatus.Activo && !AreEqualUserEmployee(employee, user))
                        {
                            User newUser = createUserWithStandardValues(employee);
                            newUser.integrationCode = employee.id.ToString();
                            newUser.Enabled = 1;
                            lock (_lock)
                            {
                                result.toEdit.Add(newUser);
                                FileLogHelper.log(LogConstants.general, LogConstants.get, newUser.Identifier, "Marcado para editar", null, Empresa);
                            }
                        }

                    }
                }

            });
            usersDirectos.AsParallel().ForAll(user => {
                Employee match = employees.FirstOrDefault(e => (user.integrationCode != null && long.Parse(user.integrationCode) == e.id) || (user.Identifier != null && (CommonHelper.rutToGVFormat(e.rut).ToLower() == user.Identifier.ToLower())));
                if (match == null)
                {
                    user.Enabled = 0;
                    lock (_lock)
                    {
                        result.toDeactivate.Add(user);
                        FileLogHelper.log(LogConstants.general, LogConstants.get, user.Identifier, "Marcado para desactivar", null, Empresa);
                    }
                }
            });

            return result;
        }

        protected UserProcessVM ProcessUsersTemporales(List<Employee> employees, List<User> users, SesionVM Empresa)
        {
            UserProcessVM result = new UserProcessVM();
            result.toAdd = new List<User>();
            result.toDeactivate = new List<User>();
            result.toActivate = new List<User>();
            result.toEdit = new List<User>();
            object _lock = new object();

            List<User> usersTemporales = users.FindAll(u => u.Custom1 != null && u.Custom1.ToLower() == UsersMultiUrlConts.Temporales);
           
            employees.AsParallel().ForAll(employee =>
            {
                User user = users.FirstOrDefault(u => (u.integrationCode != null && long.Parse(u.integrationCode) == employee.id) || (u.Identifier != null && (String.Equals(CommonHelper.rutToGVFormat(employee.rut), u.Identifier, StringComparison.OrdinalIgnoreCase))));
                if (user == null)
                {
                    
                }
                else
                {
                    if (user.Enabled.HasValue && (user.Custom1 == null || user.Custom1.ToLower() == UsersMultiUrlConts.Temporales))
                    {
                        if (user.Enabled.Value == 1 && employee.status != EmployeeStatus.Activo)
                        {
                            if (MustBeDeactivated(employee))
                            {
                                
                            }
                        }
                        else if (user.Enabled.Value == 0 && employee.status == EmployeeStatus.Activo)
                        {
                            
                        }
                        else if (user.Enabled.Value == 1 && employee.status == EmployeeStatus.Activo && user.Custom1 != null && user.Custom1.ToLower() == UsersMultiUrlConts.Temporales && user.integrationCode.IsNullOrEmpty())
                        {
                            User newUser = copyUserStandardValues(user);
                            newUser.integrationCode = employee.id.ToString();
                            newUser.Custom1 = user.Custom1;
                            lock (_lock)
                            {
                                result.toEdit.Add(newUser);
                                FileLogHelper.log(LogConstants.general, LogConstants.get, newUser.Identifier, "Marcado para editar con el codigo de integracion (" + newUser.integrationCode + ")", null, Empresa);
                            }
                        }
                    }
                }

            });

            return result;
        }
    }
}
