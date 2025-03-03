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
    public class UserMultipleSheetBusiness : UserBusiness
    {
        protected override UserProcessVM ProcessUsers(List<Employee> employees, List<User> users, SesionVM Empresa)
        {
            UserProcessVM result = new UserProcessVM();
            result.toAdd = new List<User>();
            result.toDeactivate = new List<User>();
            result.toActivate = new List<User>();
            result.toEdit = new List<User>();
            object _lock = new object();
            List<string> distinctRuts = employees.Select(e => e.rut).Distinct().ToList();
            foreach (var rut in distinctRuts)
            {
                User user = users.FirstOrDefault(u => u.Identifier != null && (CommonHelper.rutToGVFormat(rut).ToLower() == u.Identifier.ToLower()));
                List<Employee> fichasEmpleado = employees.FindAll(e => e.rut == rut);
                List<Employee> fichasEmpleadoActivas = fichasEmpleado.FindAll(e => e.status == EmployeeStatus.Activo);
                List<long> fichasEmpleadosIds = fichasEmpleado.Select(f => f.id).ToList();

                if (user != null)
                {
                    var notSynchronyzed = findNotSynchronizedSheets(user, fichasEmpleado);
                    if (!notSynchronyzed.IsNullOrEmpty())
                    {
                        User newUserWithIntegrationCode = new User();
                        newUserWithIntegrationCode.Identifier = user.Identifier;
                        newUserWithIntegrationCode.Email = user.Email;
                        newUserWithIntegrationCode.Name = user.Name;
                        newUserWithIntegrationCode.LastName = user.LastName;
                        newUserWithIntegrationCode.Enabled = user.Enabled;
                        newUserWithIntegrationCode.integrationCode = string.Join(',', fichasEmpleadosIds);
                        if (newUserWithIntegrationCode.integrationCode.Count() > 100)
                        {
                            fichasEmpleado.Sort();
                            string idsToCheckLength = "";
                            List<int> identifiersToUpdate = new List<int>();
                            for (int i = fichasEmpleado.Count - 1; i >= 0; i--)
                            {
                                idsToCheckLength = string.Join(',', identifiersToUpdate);
                                idsToCheckLength += "," + fichasEmpleado[i].id;
                                if (idsToCheckLength.Count() > 100)
                                {
                                    break;
                                }
                                identifiersToUpdate.Add((int)fichasEmpleado[i].id);
                            }
                            newUserWithIntegrationCode.integrationCode = string.Join(',', identifiersToUpdate);
                        }
                        lock (_lock)
                        {
                            result.toEdit.Add(newUserWithIntegrationCode);
                        }
                        FileLogHelper.log(LogConstants.general, LogConstants.get, newUserWithIntegrationCode.Identifier, "Marcado para editar con el codigo de integracion de las fichas actualizadas (" + newUserWithIntegrationCode.Identifier + ")", null, Empresa);
                    }
                }

                if (user == null)
                {
                    if (!fichasEmpleadoActivas.IsNullOrEmpty())
                    {
                        Employee employee = fichasEmpleadoActivas.Last();
                        if (employee.first_name.Length > 3 && employee.full_name.Length > 3 && (employee.rut.Length > 7))
                        {
                            User newUser = new User();
                            newUser.Identifier = CommonHelper.rutToGVFormat(employee.rut);
                            newUser.Email = employee.personal_email.IsNullOrEmpty() ? employee.email : employee.personal_email;
                            newUser.Name = employee.first_name;
                            int pos = employee.full_name.IndexOf(employee.first_name);
                            newUser.LastName = pos >= 0 ? employee.full_name.Remove(pos, employee.first_name.Length) : employee.full_name;
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

                }
                else
                {
                    if (user.Enabled.HasValue)
                    {
                        if (user.Enabled.Value == 1 && fichasEmpleadoActivas.IsNullOrEmpty())
                        {
                            Employee employee = fichasEmpleado.Last();
                            if (MustBeDeactivated(fichasEmpleado))
                            {
                                bool wasSyncronized = user.integrationCode != null && user.integrationCode.Contains(employee.id.ToString());
                                User newUser = new User();
                                newUser.Identifier = CommonHelper.rutToGVFormat(employee.rut);
                                newUser.Email = employee.personal_email.IsNullOrEmpty() ? employee.email : employee.personal_email;
                                newUser.Name = employee.first_name;
                                int pos = employee.full_name.IndexOf(employee.first_name);
                                newUser.LastName = pos >= 0 ? employee.full_name.Remove(pos, employee.first_name.Length) : employee.full_name;
                                newUser.integrationCode = employee.id.ToString();
                                newUser.Enabled = 0;
                                if (wasSyncronized)
                                {
                                    newUser.integrationCode = employee.id.ToString();
                                    lock (_lock)
                                    {
                                        result.toDeactivate.Add(newUser);
                                    }
                                    FileLogHelper.log(LogConstants.general, LogConstants.get, newUser.Identifier, "Marcado para desactivar", null, Empresa);
                                }
                                else
                                {

                                    lock (_lock)
                                    {
                                        result.toDeactivate.Add(newUser);
                                    }
                                    FileLogHelper.log(LogConstants.general, LogConstants.get, newUser.Identifier, "Marcado para desactivar sin codigo de integracion", null, Empresa);
                                    User newUserWithIntegrationCode = new User();
                                    newUserWithIntegrationCode.Identifier = newUser.Identifier;
                                    newUserWithIntegrationCode.Email = newUser.Email;
                                    newUserWithIntegrationCode.Name = newUser.Name;
                                    newUserWithIntegrationCode.LastName = newUser.LastName;
                                    newUserWithIntegrationCode.Enabled = newUser.Enabled;
                                    newUserWithIntegrationCode.integrationCode = string.Join(',', fichasEmpleadosIds);
                                    if (newUserWithIntegrationCode.integrationCode.Count() > 100)
                                    {
                                        fichasEmpleado.Sort();
                                        string idsToCheckLength = "";
                                        List<int> identifiersToUpdate = new List<int>();
                                        for (int i = fichasEmpleado.Count-1; i >= 0; i--)
                                        {
                                            idsToCheckLength = string.Join(',', identifiersToUpdate);
                                            idsToCheckLength += "," + fichasEmpleado[i].id;
                                            if (idsToCheckLength.Count() > 100)
                                            {
                                                break;
                                            }
                                            identifiersToUpdate.Add((int)fichasEmpleado[i].id);
                                        }
                                        newUserWithIntegrationCode.integrationCode = string.Join(',', identifiersToUpdate);

                                    }
                                    lock (_lock)
                                    {
                                        result.toEdit.Add(newUserWithIntegrationCode);
                                    }
                                    FileLogHelper.log(LogConstants.general, LogConstants.get, newUser.Identifier, "Marcado para editar con codigo de integracion (" + newUser.integrationCode + ")", null, Empresa);
                                }
                            }
                        }
                        else if (user.Enabled.Value == 0 && !fichasEmpleadoActivas.IsNullOrEmpty())
                        {
                            Employee employee = fichasEmpleadoActivas.Last();
                            bool wasSyncronized = user.integrationCode != null && user.integrationCode.Contains(employee.id.ToString());
                            User newUser = new User();
                            newUser.Identifier = CommonHelper.rutToGVFormat(employee.rut);
                            newUser.Email = employee.personal_email.IsNullOrEmpty() ? employee.email : employee.personal_email;
                            newUser.Name = employee.first_name;
                            int pos = employee.full_name.IndexOf(employee.first_name);
                            newUser.LastName = pos >= 0 ? employee.full_name.Remove(pos, employee.first_name.Length) : employee.full_name;
                            newUser.Enabled = 1;
                            if (wasSyncronized)
                            {
                                newUser.integrationCode = employee.id.ToString();
                                lock (_lock)
                                {
                                    result.toActivate.Add(newUser);
                                    FileLogHelper.log(LogConstants.general, LogConstants.get, newUser.Identifier, "Marcado para activar", null, Empresa);
                                }
                            }
                            else
                            {

                                lock (_lock)
                                {
                                    result.toActivate.Add(newUser);
                                }
                                FileLogHelper.log(LogConstants.general, LogConstants.get, newUser.Identifier, "Marcado para activar sin codigo de integracion", null, Empresa);
                                User newUserWithIntegrationCode = new User();
                                newUserWithIntegrationCode.Identifier = newUser.Identifier;
                                newUserWithIntegrationCode.Email = newUser.Email;
                                newUserWithIntegrationCode.Name = newUser.Name;
                                newUserWithIntegrationCode.LastName = newUser.LastName;
                                newUserWithIntegrationCode.Enabled = newUser.Enabled;
                                newUserWithIntegrationCode.integrationCode = string.Join(',', fichasEmpleadosIds);
                                if (newUserWithIntegrationCode.integrationCode.Count() > 100)
                                {
                                    fichasEmpleado.Sort();
                                    string idsToCheckLength = "";
                                    List<int> identifiersToUpdate = new List<int>();
                                    for (int i = fichasEmpleado.Count - 1; i >= 0; i--)
                                    {
                                        idsToCheckLength = string.Join(',', identifiersToUpdate);
                                        idsToCheckLength += "," + fichasEmpleado[i].id;
                                        if (idsToCheckLength.Count() > 100)
                                        {
                                            break;
                                        }
                                        identifiersToUpdate.Add((int)fichasEmpleado[i].id);
                                    }
                                    newUserWithIntegrationCode.integrationCode = string.Join(',', identifiersToUpdate);

                                }
                                lock (_lock)
                                {
                                    result.toEdit.Add(newUserWithIntegrationCode);
                                    FileLogHelper.log(LogConstants.general, LogConstants.get, newUser.Identifier, "Marcado para editar con el codigo de integracion (" + newUser.integrationCode + ")", null, Empresa);
                                }
                            }
                        }
                        else
                        {
                            Employee employee = fichasEmpleadoActivas.FirstOrDefault();
                            if (employee != null)
                            {
                                if (!AreEqualUserEmployeeMultiSheet(employee, user))
                                {
                                    User newUser = createUserWithStandardValues(employee);
                                    lock (_lock)
                                    {
                                        result.toEdit.Add(newUser);
                                    }
                                    FileLogHelper.log(LogConstants.general, LogConstants.get, newUser.Identifier, "Marcado para editar", null, Empresa);
                                }
                            }
                        }
                    }
                }
            }

            users.AsParallel().ForAll(user => {
                Employee match = employees.FirstOrDefault(e => (user.integrationCode != null && user.integrationCode.Contains(e.id.ToString())) || (user.Identifier != null && (CommonHelper.rutToGVFormat(e.rut).ToLower() == user.Identifier.ToLower())));
                if (match == null)
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
        protected virtual bool AreEqualUserEmployeeMultiSheet(Employee employee, User user)
        {
            int pos = employee.full_name.IndexOf(employee.first_name);
            string lastName = (pos >= 0 ? employee.full_name.Remove(pos, employee.first_name.Length) : employee.full_name).TrimStart(); return String.Equals(CommonHelper.rutToGVFormat(employee.rut), user.Identifier, StringComparison.OrdinalIgnoreCase)
            && String.Equals(user.Email, (employee.personal_email.IsNullOrEmpty() ? employee.email : employee.personal_email), StringComparison.OrdinalIgnoreCase)
            && user.Name == employee.first_name
            && user.LastName.TrimStart() == lastName
            && user.integrationCode != null;
        }

        private List<long> findNotSynchronizedSheets(User user, List<Employee> sheets)
        {
            List<long> notSynchronizedSheets = new List<long>();
            var alreadySynchronized = new string[] { };
            if (user.integrationCode != null)
            {
                alreadySynchronized = user.integrationCode.Split(',');
            }
               
            foreach (var sheet in sheets)
            {
                if (!alreadySynchronized.Contains(sheet.id.ToString()))
                {
                    notSynchronizedSheets.Add(sheet.id);
                }
            }
            return notSynchronizedSheets;
        }

        private bool MustBeDeactivated(List<Employee> employees)
        {
            foreach (var employee in employees)
            {
                if (!MustBeDeactivated(employee))
                {
                    return false;
                }
            }
            return true;
        }

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
                            noArt22.Add(employee);
                        }
                        else if (employee.custom_attributes[CustomAtributesEmployee.SincronizarGeovictoria].ToString() != CustomAtributesEmployee.NoSincronizar)
                        {
                            if (Empresa.SincronizaArticulos22)
                            {
                                noArt22.Add(employee);
                            }
                            else if (employee.current_job == null || employee.current_job.periodicity != BUKArticulos22Job.SinJornada)
                            {
                                noArt22.Add(employee);
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
                        noArt22.Add(employee);
                    }
                    else if (employee.current_job == null || employee.current_job.periodicity != BUKArticulos22Job.SinJornada)
                    {
                        noArt22.Add(employee);
                    }
                    else
                    {
                        Art22.Add(employee);
                    }
                }
            }
            List<string> art22ids = Art22.Select(a => CommonHelper.rutToGVFormat(a.rut)).ToList();
            List<User> users = GetUsers(Empresa, companyConfiguration).FindAll(u => (u.Identifier != null && !art22ids.Contains(u.Identifier)) || u.Identifier == null);

            switch (module)
            {
                case Operacion.PERMISOS:
                case Operacion.ASISTENCIA:
                    return (users.FindAll(u => !string.IsNullOrWhiteSpace(u.integrationCode)), noArt22);
                default:
                    //Usuarios
                    return (users, noArt22);
            }
        }


    }
}
