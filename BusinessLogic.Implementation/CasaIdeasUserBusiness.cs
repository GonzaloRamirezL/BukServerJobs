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
    public class CasaIdeasUserBusiness : UserByCompaniesBusiness
    {
        private readonly IGroupBusiness groupBusiness;
        private readonly IPositionBusiness positionBusiness;
        public CasaIdeasUserBusiness()
        {
            this.groupBusiness = new GroupBusiness();
            this.positionBusiness = new PositionBusiness();
        }
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

            List<GroupVM> grupos = this.groupBusiness.GetCompanyGroups(Empresa);

            grupos = this.AddGroups(Empresa, grupos, employees);

            List<PositionVM> cargos = this.positionBusiness.GetCompanyPositions(Empresa);

            cargos = this.AddPositions(Empresa, cargos, employees);

            employees = addCompanyRut(employees, Empresa, companyConfiguration);

            FileLogHelper.log(LogConstants.general, LogConstants.get, "", "PROCESANDO OPERACIONES A REALIZAR CON USUARIOS", null, Empresa);
            var process = this.ProcessUsers(employees, users, Empresa, grupos, cargos);
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

            if (process.toEdit.Any(u => !string.IsNullOrEmpty(u.GroupIdentifier)))
            {
                process.toMove = process.toEdit.FindAll(u => !string.IsNullOrEmpty(u.GroupIdentifier) && employees.Any(e => (string.Equals(CommonHelper.rutToGVFormat(e.rut), u.Identifier, StringComparison.OrdinalIgnoreCase)) && !IsSameCostCenter(e, u)));
                UpdateUserGroup(process.toMove, Empresa, companyConfiguration);
            }

            InsightHelper.logMetric("SyncUsers", startMetric - DateTime.Now, properties);
        }
        /// <summary>
        /// Este override añade los usuarios Activos o Pendientes de BUK a GV,
        /// Cliente solicita establecer en el campo personalizado 1 si es part time o full time dependiendo del WorkingScheduleType
        /// en el campo personalizado 2 establecer el código de ficha de BUK
        /// y establecer el centro de costos para los ususarios
        /// </summary>
        /// <param name="employees"></param>
        /// <param name="users"></param>
        /// <param name="Empresa"></param>
        /// <returns></returns>
        protected UserProcessVM ProcessUsers(List<Employee> employees, List<User> users, SesionVM Empresa, List<GroupVM> grupos, List<PositionVM> cargos)
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
                    if (employee.first_name.Length > 3 && employee.full_name.Length > 3 && (employee.rut.Length > 7) && (employee.status == EmployeeStatus.Activo || employee.status == EmployeeStatus.Pendiente))
                    {
                        User newUser = createUserWithStandardValues(employee);
                        newUser.integrationCode = employee.id.ToString();
                        newUser.Custom2 = employee.id.ToString();
                        if (!employee.active_since.IsNullOrEmpty())
                        {
                            newUser.ContractDate = DateTimeHelper.parseToGVFormat(DateTimeHelper.parseFromBUKFormat(employee.active_since));
                        }

                        newUser.GroupIdentifier = string.Empty;

                        newUser = this.AddExtraValues(employee, newUser, grupos, cargos);

                        newUser.Enabled = 1;


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
                        if (user.Enabled.Value == 1 && employee.status != EmployeeStatus.Activo && employee.status != EmployeeStatus.Pendiente)
                        {
                            if (MustBeDeactivated(employee))
                            {
                                bool wasSyncronized = user.integrationCode == employee.id.ToString();
                                User newUser = createUserWithStandardValues(employee);
                                newUser = this.AddExtraValues(employee, newUser, grupos, cargos);
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
                                    newUserWithIntegrationCode = this.AddExtraValues(employee, newUserWithIntegrationCode, grupos, cargos);
                                    newUserWithIntegrationCode.integrationCode = employee.id.ToString();
                                    lock (_lock)
                                    {
                                        result.toEdit.Add(newUserWithIntegrationCode);
                                    }
                                    FileLogHelper.log(LogConstants.general, LogConstants.get, newUser.Identifier, "Marcado para editar con codigo de integracion (" + newUser.integrationCode + ")", null, Empresa);
                                }

                            }

                        }
                        else if (user.Enabled.Value == 0 && (employee.status == EmployeeStatus.Activo || employee.status == EmployeeStatus.Pendiente))
                        {
                            bool wasSyncronized = user.integrationCode == employee.id.ToString();
                            User newUser = createUserWithStandardValues(employee);
                            newUser.Enabled = 1;
                            newUser = this.AddExtraValues(employee, newUser, grupos, cargos);
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
                                newUserWithIntegrationCode = this.AddExtraValues(employee, newUserWithIntegrationCode, grupos, cargos);
                                lock (_lock)
                                {
                                    result.toEdit.Add(newUserWithIntegrationCode);
                                }

                                FileLogHelper.log(LogConstants.general, LogConstants.get, newUser.Identifier, "Marcado para editar con el codigo de integracion (" + newUser.integrationCode + ")", null, Empresa);
                            }
                        }
                        else if (user.Enabled.Value == 1 && (employee.status == EmployeeStatus.Activo || employee.status == EmployeeStatus.Pendiente) && this.CostCenterExists(employee, grupos) && (!this.IsSameCostCenter(employee, user) || !AreEqualUserEmployee(employee, user) || !this.HasSamePositions(employee, user)))
                        {
                            User newUser = createUserWithStandardValues(employee);
                            newUser.integrationCode = employee.id.ToString();
                            newUser = this.AddExtraValues(employee, newUser, grupos, cargos);
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
        /// <summary>
        /// Override que verifica que el campo personalizado es igual al workingscheduletype, para saber si es full time o part time
        /// </summary>
        /// <param name="employee"></param>
        /// <param name="user"></param>
        /// <returns></returns>
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
                && String.Equals(CommonHelper.rutToGVFormat(employee.companyRut), user.userCompanyIdentifier, StringComparison.OrdinalIgnoreCase)
                && this.GetWorkingScheduleType(employee) == user.Custom1
                && this.HasCodeSheet(employee)
                && employee.code_sheet == user.Custom2;
        }
        /// <summary>
        /// Valida que el empleado tenga el centro de costos a filtrar
        /// </summary>
        /// <param name="employee"></param>
        /// <param name="job"></param>
        /// <returns></returns>
        private bool IsSameCostCenter(Employee employee, User user)
        {
            return employee.current_job != null
                && !string.IsNullOrEmpty(user.GroupDescription)
                && !employee.current_job.cost_center.IsNullOrEmpty()
                && employee.current_job.cost_center.Trim().ToLower() == user.GroupIdentifier.Trim().ToLower();
        }
        /// <summary>
        /// Valida que el centro de costos exista dentro de la empresa
        /// </summary>
        /// <param name="employee"></param>
        /// <param name="grupos"></param>
        /// <returns></returns>
        private bool CostCenterExists(Employee employee, List<GroupVM> grupos)
        {
            return employee.current_job != null
                && !employee.current_job.cost_center.IsNullOrEmpty()
                && grupos.Exists(g => g.CostCenter.Trim().ToLower() == employee.current_job.cost_center.Trim().ToLower());
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
        /// <summary>
        /// Establece el working schedule type al usuario en el campo personalizado 1
        /// </summary>
        /// <param name="employee"></param>
        /// <param name="user"></param>
        /// <returns></returns>
        private User SetWorkingScheduleType(Employee employee, User user)
        {
            if (employee.current_job != null && !string.IsNullOrEmpty(employee.current_job.working_schedule_type))
            {
                user.Custom1 = this.GetWorkingScheduleType(employee);
            }

            return user;
        }
        /// <summary>
        /// Verifica que el objeto current job no esté null y devuelve si es part time o full time dependiendo del workingscheduletype
        /// por defecto devuelve Full Time como indicó el cliente
        /// </summary>
        /// <param name="employee"></param>
        /// <returns></returns>
        private string GetWorkingScheduleType(Employee employee)
        {
            if (employee.current_job == null)
            {
                return JornadaTrabajo.FullTime;
            }
            switch (employee.current_job.working_schedule_type)
            {
                case WorkingScheduleType.OrdinariaArt22:
                    return JornadaTrabajo.FullTime;
                case WorkingScheduleType.ParcialArt40:
                    return JornadaTrabajo.PartTime;
                default:
                    return JornadaTrabajo.FullTime;
            }
        }
        /// <summary>
        /// Añade los valores extras al usuario
        /// </summary>
        /// <param name="employee"></param>
        /// <param name="user"></param>
        /// <param name="grupos"></param>
        /// <returns></returns>
        private User AddExtraValues(Employee employee, User user, List<GroupVM> grupos, List<PositionVM> cargos)
        {
            user = this.SetWorkingScheduleType(employee, user);

            if (this.CostCenterExists(employee, grupos))
            {
                user.GroupIdentifier = employee.current_job.cost_center;
            }
            if (this.HasPositionRole(employee))
            {
                user.positionIdentifier = cargos.FirstOrDefault(c => employee.current_job.role.name == c.DESCRIPCION_CARGO).IDENTIFICADOR;
            }
            if (this.HasCodeSheet(employee))
            {
                user.Custom2 = employee.code_sheet;
            }

            return user;
        }

        private bool HasCodeSheet(Employee employee)
        {
            return !employee.code_sheet.IsNullOrEmpty();
        }

        /// <summary>
        /// Comprueba que el empleado tenga un cargo asignado
        /// </summary>
        /// <param name="employee"></param>
        /// <returns></returns>
        private bool HasPositionRole(Employee employee)
        {
            return employee.current_job != null
                && employee.current_job.role != null
                && !employee.current_job.role.name.IsNullOrEmpty();
        }
        /// <summary>
        /// Comprueba que el usuario sincronizado tenga el mismo cargo que en BUK
        /// </summary>
        /// <param name="employee"></param>
        /// <param name="user"></param>
        /// <returns></returns>
        private bool HasSamePositions(Employee employee, User user)
        {
            if (!this.HasPositionRole(employee))
            {
                return true;
            }
            if (user.positionName.IsNullOrEmpty())
            {
                return false;
            }

            return employee.current_job.role.name == user.positionName;
        }
        /// <summary>
        /// Añade los cargos que no existen en geovictoria
        /// </summary>
        /// <param name="Empresa"></param>
        /// <param name="cargos"></param>
        /// <param name="employees"></param>
        /// <returns></returns>
        private List<PositionVM> AddPositions(SesionVM Empresa, List<PositionVM> cargos, List<Employee> employees)
        {
            List<PositionDTO> positionsBuk = employees
                .Where(e => e.current_job.role != null && !cargos.Exists(p => p.DESCRIPCION_CARGO == e.current_job.role.name))
                .Select(e => e.current_job.role.name)
                .Distinct()
                .ToList()
                .ConvertAll(p => new PositionDTO
                {
                    Description = p
                });
            if (positionsBuk.Count > 0)
            {
                cargos.AddRange(positionBusiness.AddCompanyPositions(Empresa, positionsBuk));
            }

            return cargos;
        }
        /// <summary>
        /// Añade los grupos que no existen en GV pero si en BUK
        /// </summary>
        /// <param name="Empresa"></param>
        /// <param name="grupos"></param>
        /// <param name="employees"></param>
        /// <returns></returns>
        private List<GroupVM> AddGroups(SesionVM Empresa, List<GroupVM> grupos, List<Employee> employees)
        {
            List<GroupVM> costCentersBuk = employees
                .Where(e => !grupos.Exists(g => g.CostCenter == e.current_job.cost_center))
                .Select(e => e.current_job.cost_center)
                .Distinct()
                .ToList()
                .ConvertAll(cc => new GroupVM
                {
                    Description = cc,
                    CostCenter = cc,
                    Path = "//"
                });
            List<GroupVM> nonInserted = new List<GroupVM>();
            if (costCentersBuk.Count > 0)
            {
                foreach (GroupVM group in costCentersBuk)
                {
                    bool result = this.groupBusiness.AddGroup(Empresa, group);
                    if (!result)
                    {
                        nonInserted.Add(group);
                    }
                }
                grupos.Clear();
                grupos = this.groupBusiness.GetCompanyGroups(Empresa);
            }

            return grupos.FindAll(g => !g.CostCenter.IsNullOrEmpty());
        }
    }
}
