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

namespace BusinessLogic.Implementation
{
    public class UserBusiness : IUserBusiness
    {
        public virtual void Sync(SesionVM Empresa, CompanyConfiguration companyConfiguration, ProcessPeriod periodo)
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

            FileLogHelper.log(LogConstants.general, LogConstants.get, "", "PROCESANDO OPERACIONES A REALIZAR CON USUARIOS", null, Empresa);
            var process = ProcessUsers(employees, users, Empresa);
            ExecuteUserOperations(process, properties, Empresa, companyConfiguration);

            InsightHelper.logMetric("SyncUsers", startMetric - DateTime.Now, properties);
        }

        /// <summary>
        /// Ejecuta las operaciones asociadas a la sincronización de usuarios guardando información de cada operación
        /// </summary>
        protected virtual void ExecuteUserOperations(UserProcessVM process, Dictionary<string, string> properties, SesionVM Empresa, CompanyConfiguration companyConfiguration)
        {
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
            ;
            FileLogHelper.log(LogConstants.general, LogConstants.get, "", "EDITANDO USUARIOS EN GV", null, Empresa);
            EditUsers(process.toEdit, Empresa, companyConfiguration);
        }

        /// <summary>
        /// Devuelve los usuarios desde GV
        /// </summary>
        protected List<User> GetUsers(SesionVM Empresa, CompanyConfiguration companyConfiguration)
        {
            return companyConfiguration.UserDAO.GetList(Empresa).FindAll(u => string.IsNullOrWhiteSpace(u.HideInReports) || u.HideInReports == bool.FalseString);
        }

        /// <summary>
        /// Crea un usuario con los valores estandar a partir de un Empleado
        /// </summary>
        protected virtual User createUserWithStandardValues(Employee employee, int? synEmail)
        {
            User newUser = new User();
            newUser.Identifier = CommonHelper.rutToGVFormat(employee.rut);
            newUser.Name = employee.first_name;
            int pos = employee.full_name.IndexOf(employee.first_name);
            newUser.LastName = (pos >= 0 ? employee.full_name.Remove(pos, employee.first_name.Length) : employee.full_name).TrimStart();
                switch (synEmail)
                {
                    case SincronizarCorreo.NoSincronizarCorreos:
                        newUser.Email = null;
                        break;

                    case SincronizarCorreo.SincronizarCorreosCorporativos:
                        newUser.Email = employee.email;
                        break;

                    case SincronizarCorreo.SincronizarCorreosPersonales:
                        newUser.Email = employee.personal_email;
                        break;

                    case null:
                        newUser.Email = employee.personal_email.IsNullOrEmpty() ? employee.email : employee.personal_email;
                        if (employee.custom_attributes != null && employee.custom_attributes.ContainsKey(CustomAtributesEmployee.SincronizarCorreoGV))
                        {
                            var customAttributeValue = employee.custom_attributes[CustomAtributesEmployee.SincronizarCorreoGV].ToString();
                            if (customAttributeValue == AdverbValues.No)
                            {
                                newUser.Email = null;
                            }
                        }
                        break;
                }
            return newUser;
        }

        /// <summary>
        /// Crea un usuario con los valores estandar a partir de otro Usuario
        /// </summary>
        protected virtual User copyUserStandardValues(User user)
        {
            User newUser = new User();
            newUser.Identifier = user.Identifier;
            newUser.Email = user.Email;
            newUser.Name = user.Name;
            newUser.LastName = user.LastName;
            newUser.Enabled = user.Enabled;
            return newUser;
        }

        /// <summary>
        /// Procesa los usuarios para saber las distintas acciones a realizar con ellos
        /// </summary>
        protected virtual UserProcessVM ProcessUsers(List<Employee> employees, List<User> users, SesionVM Empresa)
        {
            int? syncEmail = Empresa.SincronizarCorreo;
            UserProcessVM result = new UserProcessVM();
            result.toAdd = new List<User>();
            result.toDeactivate = new List<User>();
            result.toActivate = new List<User>();
            result.toEdit = new List<User>();
            object _lock = new object();

            employees.AsParallel().ForAll(employee =>
            {
                User user = users.FirstOrDefault(u => (u.integrationCode != null && long.Parse(u.integrationCode) == employee.id) || (u.Identifier != null && (String.Equals(CommonHelper.rutToGVFormat(employee.rut), u.Identifier, StringComparison.OrdinalIgnoreCase))));
                if (user == null)
                {
                    if (employee.first_name.Length > 3 && employee.full_name.Length > 3 && (employee.rut.Length > 7) && (employee.status == EmployeeStatus.Activo))
                    {
                        User newUser = createUserWithStandardValues(employee, syncEmail);
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
                                User newUser = createUserWithStandardValues(employee, syncEmail);
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
                                        FileLogHelper.log(LogConstants.general, LogConstants.get, newUser.Identifier, "Marcado para desactivacion", null, Empresa);
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
                            User newUser = createUserWithStandardValues(employee, syncEmail);
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
                            User newUser = createUserWithStandardValues(employee, syncEmail);
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
            users.AsParallel().ForAll(user =>
            {
                Employee match = employees.FirstOrDefault(e => (user.integrationCode != null && long.Parse(user.integrationCode) == e.id) || (user.Identifier != null && (CommonHelper.rutToGVFormat(e.rut).ToLower() == user.Identifier.ToLower())));
                if (match == null && user.Enabled == 1)
                {
                    user.Enabled = 0;
                    lock (_lock)
                    {
                        result.toDeactivate.Add(user);
                    }
                    FileLogHelper.log(LogConstants.general, LogConstants.get, user.Identifier, "Marcado para desactivar", null, Empresa);
                }
            });

            return result;
        }

        /// <summary>
        /// Determina si un Usuario y un Employee son iguales
        /// </summary>
        protected virtual bool AreEqualUserEmployee(Employee employee, User user)
        {
            int pos = employee.full_name.IndexOf(employee.first_name);
            string lastName = (pos >= 0 ? employee.full_name.Remove(pos, employee.first_name.Length) : employee.full_name).TrimStart();

            //si no sincroniza correo, se tiene que descartar la verificacion del email
            if (employee.custom_attributes != null
                                && employee.custom_attributes.ContainsKey(CustomAtributesEmployee.SincronizarCorreoGV)
                                && employee.custom_attributes[CustomAtributesEmployee.SincronizarCorreoGV].ToString() == AdverbValues.No)
            {

                //si el user(GV) tiene correo, hay que actualizar
                if (!string.IsNullOrWhiteSpace(user.Email))
                {
                    return false;
                }

                //el valor es no, hay que eliminar el email de la condicion
                return string.Equals(CommonHelper.rutToGVFormat(employee.rut), user.Identifier, StringComparison.OrdinalIgnoreCase)
                    && user.Name == employee.first_name
                    && user.LastName.TrimStart() == lastName
                    && user.integrationCode != null
                    && long.Parse(user.integrationCode) == employee.id;
            }


            return String.Equals(CommonHelper.rutToGVFormat(employee.rut), user.Identifier, StringComparison.OrdinalIgnoreCase)
                && String.Equals(user.Email, (employee.personal_email.IsNullOrEmpty() ? employee.email : employee.personal_email), StringComparison.OrdinalIgnoreCase)
                && user.Name == employee.first_name
                && user.LastName.TrimStart() == lastName
                && user.integrationCode != null
                && long.Parse(user.integrationCode) == employee.id;
        }

        /// <summary>
        /// Determina si un Employee debe estar desactivado
        /// </summary>
        protected bool MustBeDeactivated(Employee employee)
        {
            if (employee.current_job != null && !String.IsNullOrWhiteSpace(employee.current_job.active_until))
            {
                return DateTime.Today > DateTimeHelper.parseFromBUKFormat(employee.current_job.active_until);
            }
            return true;
        }

        /// <summary>
        /// Añade los usuarios nuevos a GV
        /// </summary>
        public void AddUsers(List<User> usersToAdd, SesionVM Empresa, CompanyConfiguration companyConfiguration)
        {
            FileLogHelper.log(LogConstants.general, LogConstants.get, "", "Se intentará(n) crear " + usersToAdd.Count + " usuario(s)", null, Empresa);
            foreach (User user in usersToAdd)
            {
                try
                {
                    FileLogHelper.log(LogConstants.general, LogConstants.get, user.Identifier, "Enviando petición para crear usuario: (" + user.Identifier + ")", null, Empresa);
                    var responde = companyConfiguration.UserDAO.Add(user, Empresa);
                    if (responde._statusCode == System.Net.HttpStatusCode.BadRequest)
                    {
                        InsightHelper.logTrace("User not created in GV: " + user.Identifier + " Error: " + responde._message, Empresa.Empresa);
                        FileLogHelper.log(LogConstants.user, LogConstants.error_add, user.Identifier, responde._message, user, Empresa);
                    }
                    else
                    {
                        FileLogHelper.log(LogConstants.user, LogConstants.add, user.Identifier, "", user, Empresa);
                    }
                }
                catch (Exception ex)
                {
                    Dictionary<string, string> properties = new Dictionary<string, string>();
                    properties["Method"] = "User/Add";
                    properties["User-RUT"] = user.Identifier;
                    properties["User-Status"] = user.Enabled.ToString();
                    properties["User-Email"] = user.Email;
                    properties["User-LastName"] = user.LastName;
                    properties["User-Name"] = user.Name;
                    string Data = "RUT: " + user.Identifier + " ";
                    InsightHelper.logException(ex, Empresa.Empresa, properties);
                    FileLogHelper.log(LogConstants.user, LogConstants.error_add, user.Identifier, ex.Message, user, Empresa);
                }

            }
        }

        /// <summary>
        /// Desactiva los usuarios en GV
        /// </summary>
        public void DeactivateUsers(List<User> usersToDeactivate, SesionVM Empresa, CompanyConfiguration companyConfiguration)
        {
            FileLogHelper.log(LogConstants.general, LogConstants.get, "", "Se intentará(n) desactivar " + usersToDeactivate.Count + " usuario(s)", null, Empresa);
            foreach (User user in usersToDeactivate)
            {
                // Remove from groups
                try
                {
                    // Fix multifichas
                    user.integrationCode = null;
                    FileLogHelper.log(LogConstants.general, LogConstants.get, user.Identifier, "Removiendo usuario de grupos", null, Empresa);
                    var responde = companyConfiguration.UserDAO.RemoveFromGroups(user, Empresa);
                    if (responde._statusCode == System.Net.HttpStatusCode.BadRequest)
                    {
                        InsightHelper.logTrace("User not removed from groups in GV: " + user.Identifier + " Error: " + responde._message, Empresa.Empresa);
                        FileLogHelper.log(LogConstants.general, LogConstants.get, user.Identifier, "No se logró remover de grupos: " + responde._message, null, Empresa);
                    }
                    else
                    {
                        FileLogHelper.log(LogConstants.general, LogConstants.get, user.Identifier, "Removido de grupos", null, Empresa);
                    }
                }
                catch (Exception ex)
                {

                    InsightHelper.logTrace("User not removed from groups in GV: " + user.Identifier + " Error: " + ex.Message, Empresa.Empresa);
                    FileLogHelper.log(LogConstants.general, LogConstants.get, user.Identifier, "No se logró remover de grupos " + ex.Message, null, Empresa);
                }
                // Disable
                try
                {
                    FileLogHelper.log(LogConstants.general, LogConstants.get, user.Identifier, "Enviando petición para desactivar usuario: (" + user.Identifier + ")", null, Empresa);
                    var responde = companyConfiguration.UserDAO.Disable(user, Empresa);
                    if (responde._statusCode == System.Net.HttpStatusCode.BadRequest)
                    {
                        InsightHelper.logTrace("User not deactivated in GV: " + user.Identifier + " Error: " + responde._message, Empresa.Empresa);
                        FileLogHelper.log(LogConstants.user, LogConstants.error_deactivate, user.Identifier, responde._message, user, Empresa);
                    }
                    else
                    {
                        FileLogHelper.log(LogConstants.user, LogConstants.deactivate, user.Identifier, "", user, Empresa);
                    }
                }
                catch (Exception ex)
                {
                    Dictionary<string, string> properties = new Dictionary<string, string>();
                    properties["Method"] = "User/Disable";
                    properties["User-RUT"] = user.Identifier;
                    properties["User-Status"] = user.Enabled.ToString();
                    properties["User-Email"] = user.Email;
                    properties["User-LastName"] = user.LastName;
                    properties["User-Name"] = user.Name;
                    string Data = "RUT: " + user.Identifier + " ";

                    InsightHelper.logException(ex, Empresa.Empresa, properties);
                    FileLogHelper.log(LogConstants.user, LogConstants.error_add, user.Identifier, ex.Message, user, Empresa);
                }

            }
        }

        /// <summary>
        /// Activa los usuarios en GV
        /// </summary>
        public void ActivateUsers(List<User> usersToActivate, SesionVM Empresa, CompanyConfiguration companyConfiguration)
        {
            FileLogHelper.log(LogConstants.general, LogConstants.get, "", "Se intentará(n) activar " + usersToActivate.Count + " usuario(s)", null, Empresa);
            foreach (User user in usersToActivate)
            {
                try
                {
                    FileLogHelper.log(LogConstants.general, LogConstants.get, user.Identifier, "Enviando petición para activar usuario: (" + user.Identifier + ")", null, Empresa);
                    // Fix multifichas
                    user.integrationCode = null;
                    var responde = companyConfiguration.UserDAO.Enable(user, Empresa);
                    if (responde._statusCode == System.Net.HttpStatusCode.BadRequest)
                    {
                        InsightHelper.logTrace("User not activated in GV: " + user.Identifier + " Error: " + responde._message, Empresa.Empresa);
                        FileLogHelper.log(LogConstants.user, LogConstants.error_activate, user.Identifier, "", user, Empresa);
                    }
                    else
                    {
                        FileLogHelper.log(LogConstants.user, LogConstants.activate, user.Identifier, "", user, Empresa);
                    }
                }
                catch (Exception ex)
                {
                    Dictionary<string, string> properties = new Dictionary<string, string>();
                    properties["Method"] = "User/Enable";
                    properties["User-RUT"] = user.Identifier;
                    properties["User-Status"] = user.Enabled.ToString();
                    properties["User-Email"] = user.Email;
                    properties["User-LastName"] = user.LastName;
                    properties["User-Name"] = user.Name;
                    string Data = "RUT: " + user.Identifier + " ";
                    InsightHelper.logException(ex, Empresa.Empresa, properties);
                    FileLogHelper.log(LogConstants.user, LogConstants.error_activate, user.Identifier, ex.Message, user, Empresa);
                }

            }
        }

        /// <summary>
        /// Edita los usuarios en GV
        /// </summary>
        public void EditUsers(List<User> usersToEdit, SesionVM Empresa, CompanyConfiguration companyConfiguration)
        {
            FileLogHelper.log(LogConstants.general, LogConstants.get, "", "Se intentará(n) editar " + usersToEdit.Count + " usuario(s)", null, Empresa);
            foreach (User user in usersToEdit)
            {
                try
                {
                    FileLogHelper.log(LogConstants.general, LogConstants.get, user.Identifier, "Enviando petición para editar usuario: (" + user.Identifier + ")", null, Empresa);
                    var responde = companyConfiguration.UserDAO.Edit(user, Empresa);
                    if (responde._statusCode == System.Net.HttpStatusCode.BadRequest)
                    {
                        InsightHelper.logTrace("User not edited in GV: " + user.Identifier + " Error: " + responde._message, Empresa.Empresa);
                        FileLogHelper.log(LogConstants.user, LogConstants.error_edit, user.Identifier, "", user, Empresa);
                    }
                    else
                    {
                        FileLogHelper.log(LogConstants.user, LogConstants.edit, user.Identifier, "", user, Empresa);
                    }
                }
                catch (Exception ex)
                {
                    Dictionary<string, string> properties = new Dictionary<string, string>();
                    properties["Method"] = "User/edit";
                    properties["User-RUT"] = user.Identifier;
                    properties["User-Status"] = user.Enabled.ToString();
                    properties["User-Email"] = user.Email;
                    properties["User-LastName"] = user.LastName;
                    properties["User-Name"] = user.Name;
                    string Data = "RUT: " + user.Identifier + " ";
                    InsightHelper.logException(ex, Empresa.Empresa, properties);
                    FileLogHelper.log(LogConstants.user, LogConstants.error_edit, user.Identifier, ex.Message, user, Empresa);
                }

            }
        }

        public void UpdateUserGroup(List<User> users, SesionVM Empresa, CompanyConfiguration companyConfiguration)
        {
            foreach (User user in users)
            {
                try
                {
                    var responde = companyConfiguration.UserDAO.MoveToGroup(user, Empresa);
                    if (responde._statusCode == System.Net.HttpStatusCode.BadRequest)
                    {
                        FileLogHelper.log(LogConstants.user, LogConstants.error_move, user.Identifier, "", user, Empresa);
                    }
                    else
                    {
                        FileLogHelper.log(LogConstants.user, LogConstants.move, user.Identifier, "", user, Empresa);
                    }
                }
                catch (Exception ex)
                {
                    Dictionary<string, string> properties = new Dictionary<string, string>();
                    properties["Method"] = "User/edit";
                    properties["User-RUT"] = user.Identifier;
                    properties["User-Status"] = user.Enabled.ToString();
                    properties["User-Email"] = user.Email;
                    properties["User-LastName"] = user.LastName;
                    properties["User-Name"] = user.Name;
                    properties["User-GroupIdentifier"] = user.GroupIdentifier;
                    string Data = "RUT: " + user.Identifier + " ";
                    InsightHelper.logException(ex, Empresa.Empresa, properties);
                    FileLogHelper.log(LogConstants.user, LogConstants.error_move, user.Identifier, ex.Message, user, Empresa);
                }

            }
        }

        public virtual (List<User>, List<Employee>) GetUsersForSync(SesionVM Empresa, CompanyConfiguration companyConfiguration, List<Employee> employees, Operacion module)
        {
            List<User> users = GetUsers(Empresa, companyConfiguration);
            switch (module)
            {

                case Operacion.PERMISOS:
                case Operacion.ASISTENCIA:
                    return (users.FindAll(u => !string.IsNullOrWhiteSpace(u.integrationCode)), employees);
                default://Usuarios
                    return (users, employees);

            }
        }
    }
}
