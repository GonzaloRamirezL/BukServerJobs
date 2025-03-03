using API.BUK.DTO;
using API.BUK.DTO.Consts;
using API.GV.DTO;
using API.Helpers.Commons;
using API.Helpers.VM;
using API.Helpers.VM.Consts;
using BusinessLogic.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BusinessLogic.Implementation
{
    public class UserDirectBusiness : UserBusiness, IUserBusiness
    {
        protected override UserProcessVM ProcessUsers(List<Employee> employees, List<User> users, SesionVM Empresa)
        {
            UserProcessVM result = new UserProcessVM();
            result.toAdd = new List<User>();
            result.toDeactivate = new List<User>();
            result.toActivate = new List<User>();
            result.toEdit = new List<User>();
            object _lock = new object();

            List<User> directUsers = users.FindAll(u => u.Custom1 == null || u.Custom1.ToLower() != UsersMultiUrlConts.Temporales);
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
                    if (user.Enabled.HasValue && (user.Custom1 == null || user.Custom1.ToLower() != UsersMultiUrlConts.Temporales))
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
            directUsers.AsParallel().ForAll(user => {
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
    }
}
