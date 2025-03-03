using System;
using System.Collections.Generic;
using BusinessLogic.Interfaces;
using API.BUK.DTO;
using API.BUK.DTO.Consts;
using API.GV.DTO;
using API.Helpers.Commons;
using API.Helpers.VM;
using API.Helpers.VM.Consts;
using BusinessLogic.Interfaces.VM;
using System.Linq;

namespace BusinessLogic.Implementation
{
    public class UserAconcaguaBusiness : UserBusiness, IUserBusiness
    {
        private readonly string externalGroupLabel = "e";
        private readonly string undefinedContractLabel = "Indefinido";

        protected override void ExecuteUserOperations(UserProcessVM process, Dictionary<string, string> properties, SesionVM Empresa, CompanyConfiguration companyConfiguration)
        {
            base.ExecuteUserOperations(process, properties, Empresa, companyConfiguration);

            properties["MoveUsers"] = process.toMove.Count.ToString();

            FileLogHelper.log(LogConstants.general, LogConstants.get, "", "MOVIENDO USUARIOS ENTRE GRUPOS EN GV", null, Empresa);
            MoveUsers(process.toMove, Empresa, companyConfiguration);
        }

        protected override bool AreEqualUserEmployee(Employee employee, User user)
        {
            return base.AreEqualUserEmployee(employee, user) && user.Custom1 == employee.current_job.contract_type;
        }

        protected override User createUserWithStandardValues(Employee employee)
        {
            User newUser = base.createUserWithStandardValues(employee);
            newUser.Custom1 = employee.current_job.contract_type;
            newUser.GroupIdentifier = employee.current_job.cost_center;
            return newUser;
        }

        public override (List<User>, List<Employee>) GetUsersForSync(SesionVM Empresa, CompanyConfiguration companyConfiguration, List<Employee> employees, Operacion module)
        {
            (List<User> users, List<Employee> employeesList) = base.GetUsersForSync(Empresa, companyConfiguration, employees, module);
            users = users.FindAll(u => string.IsNullOrWhiteSpace(u.GroupIdentifier) || !u.GroupIdentifier.StartsWith(externalGroupLabel));
            if (module == Operacion.ASISTENCIA)
            {
                if (Empresa.FechaCorte == 31)
                {
                    // empleados temporales
                    users = users.FindAll(u => string.IsNullOrWhiteSpace(u.Custom1) || u.Custom1.Trim().ToLower() != undefinedContractLabel.ToLower());
                }
                else
                {
                    // empleados indefinidos
                    users = users.FindAll(u => !string.IsNullOrWhiteSpace(u.Custom1) && u.Custom1.Trim().ToLower() == undefinedContractLabel.ToLower());
                }
            }
            return (users, employeesList);
        }

        protected override UserProcessVM ProcessUsers(List<Employee> employees, List<User> users, SesionVM Empresa)
        {
            UserProcessVM result =  base.ProcessUsers(employees, users, Empresa);
            result.toMove = new List<User>();
            object _lock = new object();

            employees.AsParallel().ForAll(employee =>
            {
                User user = users.FirstOrDefault(u => (u.integrationCode != null && long.Parse(u.integrationCode) == employee.id) || (!String.IsNullOrWhiteSpace(u.Identifier) && (String.Equals(CommonHelper.rutToGVFormat(employee.rut), u.Identifier, StringComparison.OrdinalIgnoreCase))));
                if (user != null && employee.status == EmployeeStatus.Activo && employee.current_job != null)
                {
                    string employeeGroupId = employee.current_job.cost_center;
                    if (!string.IsNullOrWhiteSpace(employeeGroupId) && user.GroupIdentifier != employeeGroupId)
                    {
                        User newUser = createUserWithStandardValues(employee);
                        newUser.integrationCode = employee.id.ToString();
                        newUser.Enabled = 1;
                        newUser.GroupIdentifier = employeeGroupId;
                        lock (_lock)
                        {
                            result.toMove.Add(newUser);
                        }
                        FileLogHelper.log(LogConstants.general, LogConstants.get, newUser.Identifier, "Marcado para mover", null, Empresa);
                    }
                }
            });

            return result;
        }

        /// <summary>
        /// Mueve los usuarios de un grupo a otro en GV
        /// </summary>
        protected void MoveUsers(List<User> usersToMove, SesionVM Empresa, CompanyConfiguration companyConfiguration)
        {
            FileLogHelper.log(LogConstants.general, LogConstants.get, "", "Se intentará(n) mover " + usersToMove.Count + " usuario(s)", null, Empresa);
            foreach (User user in usersToMove)
            {
                try
                {
                    FileLogHelper.log(LogConstants.general, LogConstants.get, user.Identifier, "Enviando petición para mover usuario: (" + user.Identifier + ")", null, Empresa);
                    var responde = companyConfiguration.UserDAO.MoveToGroup(user, Empresa);
                    if (responde._statusCode == System.Net.HttpStatusCode.BadRequest)
                    {
                        InsightHelper.logTrace("User not moved in GV: " + user.Identifier + " Error: " + responde._message, Empresa.Empresa);
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
                    properties["Method"] = "User/moveGeneral";
                    properties["User-RUT"] = user.Identifier;
                    properties["User-Status"] = user.Enabled.ToString();
                    properties["User-Email"] = user.Email;
                    properties["User-LastName"] = user.LastName;
                    properties["User-Name"] = user.Name;
                    string Data = "RUT: " + user.Identifier + " ";
                    InsightHelper.logException(ex, Empresa.Empresa, properties);
                    FileLogHelper.log(LogConstants.user, LogConstants.error_move, user.Identifier, ex.Message, user, Empresa);
                }

            }
        }
    }
}
