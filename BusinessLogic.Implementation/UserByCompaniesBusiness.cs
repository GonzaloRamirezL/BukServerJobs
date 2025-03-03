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
    public class UserByCompaniesBusiness : UserBusiness, IUserBusiness
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

            FileLogHelper.log(LogConstants.general, LogConstants.get, "", "PIDIENDO EMPLEADOS A BUK", null, Empresa);
            List<Employee> employees = companyConfiguration.EmployeeBusiness.GetEmployeesForSync(Empresa, companyConfiguration, startDate, endDate);
            properties["EMPRESA"] = Empresa.Empresa;
            properties["GetEmployees"] = employees.Count.ToString();

            FileLogHelper.log(LogConstants.general, LogConstants.get, "", "PIDIENDO USUARIOS A GV", null, Empresa);
            var persons = GetUsersForSync(Empresa, companyConfiguration, employees, Operacion.USUARIOS);
            List<User> users = persons.Item1;
            employees = persons.Item2;
            properties["GetUsers"] = users.Count.ToString();

            employees = addCompanyRut(employees, Empresa, companyConfiguration);

            FileLogHelper.log(LogConstants.general, LogConstants.get, "", "PROCESANDO OPERACIONES A REALIZAR CON USUARIOS", null, Empresa);
            var process = ProcessUsers(employees, users, Empresa);
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
            List<User> usersInCompanies = users.FindAll(u => !string.IsNullOrWhiteSpace(u.userCompanyIdentifier));
            List<User> usersWithoutCompany = users.FindAll(u => string.IsNullOrWhiteSpace(u.userCompanyIdentifier));

            employees.AsParallel().ForAll(employee =>
            {
                User user = usersInCompanies.FirstOrDefault(u => (u.integrationCode != null && long.Parse(u.integrationCode) == employee.id) || (u.Identifier != null && (String.Equals(CommonHelper.rutToGVFormat(employee.rut), u.Identifier, StringComparison.OrdinalIgnoreCase))));
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
                        }
                        FileLogHelper.log(LogConstants.general, LogConstants.get, newUser.Identifier, "Marcado para crear", null, Empresa);
                    }
                }
                else
                {
                    if (user.Enabled.HasValue)
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
                                    }

                                    FileLogHelper.log(LogConstants.general, LogConstants.get, newUser.Identifier, "Marcado para desactivar", null, Empresa);

                                    if (!AreEqualUserEmployee(employee, user))
                                    {
                                        lock (_lock)
                                        {
                                            result.toEdit.Add(newUser);
                                        }
                                        FileLogHelper.log(LogConstants.general, LogConstants.get, newUser.Identifier, "Marcado para editar despues de desactivacion", null, Empresa);
                                    }
                                }
                                else
                                {
                                    lock (_lock)
                                    {
                                        result.toDeactivate.Add(newUser);
                                    }
                                    FileLogHelper.log(LogConstants.general, LogConstants.get, newUser.Identifier, "Marcado para desactivar sin codigo de integracion", null, Empresa);

                                    User newUserWithIntegrationCode = copyUserStandardValues(newUser);
                                    newUserWithIntegrationCode.integrationCode = employee.id.ToString();
                                    lock (_lock)
                                    {
                                        result.toEdit.Add(newUserWithIntegrationCode);
                                    }
                                    FileLogHelper.log(LogConstants.general, LogConstants.get, newUser.Identifier, "Marcado para editar con codigo de integracion (" + newUser.integrationCode + ")", null, Empresa);
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
                                }
                                FileLogHelper.log(LogConstants.general, LogConstants.get, newUser.Identifier, "Marcado para activar", null, Empresa);
                                if (!AreEqualUserEmployee(employee, user))
                                {
                                    lock (_lock)
                                    {
                                        result.toEdit.Add(newUser);
                                    }
                                    FileLogHelper.log(LogConstants.general, LogConstants.get, newUser.Identifier, "Marcado para editar despues de activacion", null, Empresa);
                                }
                            }
                            else
                            {
                                lock (_lock)
                                {
                                    result.toActivate.Add(newUser);
                                }
                                FileLogHelper.log(LogConstants.general, LogConstants.get, newUser.Identifier, "Marcado para activar sin codigo de integracion", null, Empresa);
                                User newUserWithIntegrationCode = copyUserStandardValues(newUser);
                                newUserWithIntegrationCode.integrationCode = employee.id.ToString();
                                lock (_lock)
                                {
                                    result.toEdit.Add(newUserWithIntegrationCode);
                                }

                                FileLogHelper.log(LogConstants.general, LogConstants.get, newUser.Identifier, "Marcado para editar con el codigo de integracion (" + newUser.integrationCode + ")", null, Empresa);
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
                            }
                            FileLogHelper.log(LogConstants.general, LogConstants.get, newUser.Identifier, "Marcado para editar", null, Empresa);
                        }

                    }
                }

            });
            usersInCompanies.AsParallel().ForAll(user =>
            {
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
            usersWithoutCompany.AsParallel().ForAll(user =>
            {
                Employee match = employees.FirstOrDefault(e => (user.integrationCode != null && long.Parse(user.integrationCode) == e.id) || (user.Identifier != null && (CommonHelper.rutToGVFormat(e.rut).ToLower() == user.Identifier.ToLower())));
                if (match != null)
                {
                    User newUser = createUserWithStandardValues(match);
                    newUser.integrationCode = match.id.ToString();
                    newUser.Enabled = 1;
                    lock (_lock)
                    {
                        result.toEdit.Add(newUser);
                    }
                    FileLogHelper.log(LogConstants.general, LogConstants.get, user.Identifier, "Marcado para editar con razon social", null, Empresa);
                }
            });

            return result;
        }

        public override (List<User>, List<Employee>) GetUsersForSync(SesionVM Empresa, CompanyConfiguration companyConfiguration, List<Employee> employees, Operacion module)
        {
            List<User> users = GetUsers(Empresa, companyConfiguration);
            var companies = companyConfiguration.CompanyBusiness.GetCompanies(Empresa, companyConfiguration);
            List<string> razonesSociales = companies.Select(c => CommonHelper.rutToGVFormat(c.rut)).ToList();
            users = users.FindAll(u => string.IsNullOrWhiteSpace(u.userCompanyIdentifier) || razonesSociales.Contains(u.userCompanyIdentifier));
            switch (module)
            {

                case Operacion.PERMISOS:
                case Operacion.ASISTENCIA:
                    return (users.FindAll(u => !string.IsNullOrWhiteSpace(u.integrationCode) && razonesSociales.Contains(u.userCompanyIdentifier)), employees);
                default://Usuarios
                    return (users, employees);

            }
        }

        protected override User createUserWithStandardValues(Employee employee)
        {
            User newUser = new User();
            newUser.Identifier = CommonHelper.rutToGVFormat(employee.rut);
            newUser.Email = employee.personal_email.IsNullOrEmpty() ? employee.email : employee.personal_email;
            newUser.Name = employee.first_name;
            int pos = employee.full_name.IndexOf(employee.first_name);
            newUser.LastName = (pos >= 0 ? employee.full_name.Remove(pos, employee.first_name.Length) : employee.full_name).TrimStart();
            newUser.userCompanyIdentifier = employee.companyRut;
            return newUser;
        }

        protected override bool AreEqualUserEmployee(Employee employee, User user)
        {
            int pos = employee.full_name.IndexOf(employee.first_name);
            string lastName = (pos >= 0 ? employee.full_name.Remove(pos, employee.first_name.Length) : employee.full_name).TrimStart();
            return String.Equals(CommonHelper.rutToGVFormat(employee.rut), user.Identifier, StringComparison.OrdinalIgnoreCase)
                && String.Equals(user.Email, (employee.personal_email.IsNullOrEmpty() ? employee.email : employee.personal_email), StringComparison.OrdinalIgnoreCase)
                && user.Name == employee.first_name
                && user.LastName.TrimStart() == lastName
                && user.integrationCode != null
                && long.Parse(user.integrationCode) == employee.id
                && String.Equals(CommonHelper.rutToGVFormat(employee.companyRut), user.userCompanyIdentifier, StringComparison.OrdinalIgnoreCase);
        }

        private List<Employee> addCompanyRut(List<Employee> employees, SesionVM Empresa, CompanyConfiguration companyConfiguration)
        {
            FileLogHelper.log(LogConstants.general, LogConstants.get, "", "PIDIENDO EMPRESAS (RS) A BUK", null, Empresa);
            List<Company> companies = companyConfiguration.CompanyBusiness.GetCompanies(Empresa, companyConfiguration);
            foreach (var employee in employees)
            {
                employee.companyRut = "";
                if (employee.current_job != null && employee.current_job.company_id != null)
                {
                    var company = companies.FirstOrDefault(c => c.id == employee.current_job.company_id);
                    if (company != null)
                    {
                        employee.companyRut = CommonHelper.rutToGVFormat(company.rut);
                    }
                }
            }
            return employees;
        }

        protected override User copyUserStandardValues(User user)
        {
            User newUser = new User();
            newUser.Identifier = user.Identifier;
            newUser.Email = user.Email;
            newUser.Name = user.Name;
            newUser.LastName = user.LastName;
            newUser.Enabled = user.Enabled;
            newUser.userCompanyIdentifier = user.userCompanyIdentifier;
            return newUser;
        }
    }
}
