using API.BUK.DTO;
using API.BUK.DTO.Consts;
using API.GV.DTO;
using API.Helpers;
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
    public class PicharaUserLogFilterBusiness : UserByCompaniesBusiness, IPicharaUserLogFilterBusiness
    {
        /// <summary>
        /// Este override añade los usuarios independiente si está activo en BUK,
        /// Además le establece la fecha de termino de contrato en GV si tiene una en BUK
        /// </summary>
        /// <param name="employees"></param>
        /// <param name="users"></param>
        /// <param name="Empresa"></param>
        /// <returns></returns>
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
            int MAX_PROCESS = 0;
            bool success = Int32.TryParse(ConfigurationHelper.Value("maxParallel"), out int maxProcess);
            MAX_PROCESS = success ? maxProcess : 10;

            employees.AsParallel().WithDegreeOfParallelism(MAX_PROCESS).ForAll(employee =>
            {
                User user = usersInCompanies.FirstOrDefault(u => (!string.IsNullOrEmpty(u.integrationCode) && long.Parse(u.integrationCode) == employee.id) || (u.Identifier != null && (String.Equals(CommonHelper.rutToGVFormat(employee.rut), u.Identifier, StringComparison.OrdinalIgnoreCase))));
                if (user == null)
                {
                    if (employee.first_name.Length > 3 && employee.full_name.Length > 3 && (employee.rut.Length > 7))
                    {
                        User newUser = createUserWithStandardValues(employee);
                        newUser.integrationCode = employee.id.ToString();
                        if (!employee.active_since.IsNullOrEmpty())
                        {
                            newUser.ContractDate = DateTimeHelper.parseToGVFormat(DateTimeHelper.parseFromBUKFormat(employee.active_since));
                        }


                        newUser.GroupIdentifier = "";
                        if (employee.status == EmployeeStatus.Activo)
                        {
                            newUser.Enabled = 1;
                        }
                        else
                        {
                            newUser.Enabled = 0;
                        }

                        if (this.HasEndDate(employee))
                        {
                            newUser.endContractDate = DateTimeHelper.parseToGVFormat(DateTimeHelper.parseFromBUKFormat(employee.current_job.active_until, true));
                        }

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
                        if (user.Enabled.Value == 1 && (employee.status != EmployeeStatus.Activo))
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

                                    User newUserWithIntegrationCode = copyUserStandardValues(newUser, employee);
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
                                User newUserWithIntegrationCode = copyUserStandardValues(newUser, employee);
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
                Employee match = employees.FirstOrDefault(e => (!string.IsNullOrEmpty(user.integrationCode) && long.Parse(user.integrationCode) == e.id) || (user.Identifier != null && (CommonHelper.rutToGVFormat(e.rut).ToLower() == user.Identifier.ToLower())));
                if (match == null && user.Enabled == 1)
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
                Employee match = employees.FirstOrDefault(e => (!string.IsNullOrEmpty(user.integrationCode) && long.Parse(user.integrationCode) == e.id) || (user.Identifier != null && (CommonHelper.rutToGVFormat(e.rut).ToLower() == user.Identifier.ToLower())));
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

        protected User copyUserStandardValues(User user, Employee employee)
        {
            User newUser = new User();
            newUser.Identifier = user.Identifier;
            newUser.Email = user.Email;
            newUser.Name = user.Name;
            newUser.LastName = user.LastName;
            newUser.Enabled = user.Enabled;
            newUser.userCompanyIdentifier = user.userCompanyIdentifier;
            if (employee.custom_attributes != null && employee.custom_attributes.ContainsKey("COST_CENTER_GV") && !string.IsNullOrEmpty(employee.custom_attributes[CustomAtributesEmployee.CostCenterGV].ToString()))
            {
                newUser.Custom1 = employee.custom_attributes[CustomAtributesEmployee.CostCenterGV].ToString();
            }
            else
            {
                newUser.Custom1 = string.Empty;
            }
            return newUser;
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
            if (employee.custom_attributes != null && employee.custom_attributes.ContainsKey("COST_CENTER_GV") && !string.IsNullOrEmpty(employee.custom_attributes[CustomAtributesEmployee.CostCenterGV].ToString()))
            {
                newUser.Custom1 = employee.custom_attributes[CustomAtributesEmployee.CostCenterGV].ToString();
            }
            return newUser;
        }

        protected override bool AreEqualUserEmployee(Employee employee, User user)
        {
            int pos = employee.full_name.IndexOf(employee.first_name);
            string lastName = (pos >= 0 ? employee.full_name.Remove(pos, employee.first_name.Length) : employee.full_name).TrimStart();
            
            string costCenterPichara = "";
            if (employee.custom_attributes.ContainsKey(CustomAtributesEmployee.CostCenterGV) && !string.IsNullOrEmpty(employee.custom_attributes[CustomAtributesEmployee.CostCenterGV].ToString()))
            {
                costCenterPichara = employee.custom_attributes[CustomAtributesEmployee.CostCenterGV].ToString();
            }

            return String.Equals(CommonHelper.rutToGVFormat(employee.rut), user.Identifier, StringComparison.OrdinalIgnoreCase)
                && String.Equals(user.Email, (employee.personal_email.IsNullOrEmpty() ? employee.email : employee.personal_email), StringComparison.OrdinalIgnoreCase)
                && user.Name == employee.first_name
                && user.LastName.TrimStart() == lastName
                && user.integrationCode != null
                && user.Custom1 == costCenterPichara
                && long.Parse(user.integrationCode) == employee.id
                && String.Equals(CommonHelper.rutToGVFormat(employee.companyRut), user.userCompanyIdentifier, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Verifica que el usuario tiene fecha de termino establecida en BUK
        /// </summary>
        /// <param name="employee"></param>
        /// <returns></returns>
        private bool HasEndDate(Employee employee)
        {
            return employee.current_job != null
                && employee.current_job.active_until != null;
        }

        public List<User> LogFilterUsers(List<User> users, List<Employee> employees, SesionVM Empresa, bool filter = true)
        {
            List<User> result = new List<User>();
            foreach (User user in users)
            {
                if (!string.IsNullOrEmpty(user.integrationCode) && employees.Any(e => e.id == long.Parse(user.integrationCode) && string.Equals(CommonHelper.rutToGVFormat(e.rut), user.Identifier, StringComparison.OrdinalIgnoreCase)))
                {
                    if (!user.Custom1.IsNullOrEmpty() && user.Custom1.ToLower() == Empresa.CargoEmpleo.ToLower() && filter)
                    {
                        result.Add(user);
                    }
                    else if (filter)
                    {
                        FileLogHelper.UserLog(LogConstants.general, LogConstants.no_cutoff, user.Identifier, $"Usuario sin filtro ({Empresa.CargoEmpleo}) en el campo personalizado 1", user, Empresa, user.userCompanyIdentifier);
                    }
                    else
                    {
                        result.Add(user);
                    }
                }
            }
            return result;
        }

        public List<Employee> FilterEmployees(List<Employee> employees, SesionVM Empresa)
        {
            return employees.FindAll(e => this.IsSameCostCenter(e, Empresa.CargoEmpleo));
        }
        /// <summary>
        /// Valida que el empleado tenga el centro de costos a filtrar
        /// </summary>
        /// <param name="employee"></param>
        /// <param name="job"></param>
        /// <returns></returns>
        private bool IsSameCostCenter(Employee employee, string job)
        {
            return employee.custom_attributes.ContainsKey(CustomAtributesEmployee.CostCenterGV)
                && !string.IsNullOrEmpty(employee.custom_attributes[CustomAtributesEmployee.CostCenterGV].ToString())
                && (employee.custom_attributes[CustomAtributesEmployee.CostCenterGV].ToString().Trim().ToLower() == job.ToLower()
                || employee.custom_attributes[CustomAtributesEmployee.CostCenterGV].ToString().Trim().ToLower().Contains(job.ToLower()));
        }
        /// <summary>
        /// Añade el rut a los usuarios de BUK
        /// </summary>
        /// <param name="employees"></param>
        /// <param name="Empresa"></param>
        /// <param name="companyConfiguration"></param>
        /// <returns></returns>
        private List<Employee> addCompanyRut(List<Employee> employees, SesionVM Empresa, CompanyConfiguration companyConfiguration)
        {
            FileLogHelper.log(LogConstants.general, LogConstants.get, "", "PIDIENDO EMPRESAS (RS) A BUK", null, Empresa);
            List<Company> companies = companyConfiguration.CompanyBusiness.GetCompanies(Empresa, companyConfiguration);
            foreach (var employee in employees)
            {
                employee.companyRut = "";
                if (employee.current_job != null && employee.current_job.company_id > 0)
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
    }
}
