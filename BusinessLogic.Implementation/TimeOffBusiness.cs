using API.BUK.DTO;
using API.BUK.DTO.Consts;
using API.GV.DTO;
using API.GV.DTO.Consts;
using API.GV.DTO.Filters;
using API.Helpers.Commons;
using API.Helpers.VM;
using API.Helpers.VM.Consts;
using BusinessLogic.Interfaces;
using BusinessLogic.Interfaces.VM;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessLogic.Implementation
{
    public class TimeOffBusiness : ITimeOffBusiness
    {
        public virtual void Sync(SesionVM Empresa, ProcessPeriod periodo, List<PeriodConfiguration> configs, CompanyConfiguration companyConfiguration)
        {
            FileLogHelper.log(LogConstants.period, LogConstants.get, "", string.Empty, periodo, Empresa);
            Console.WriteLine("PROCESANDO PERIODO: " + periodo.month);

            DateTime startMetric = DateTime.Now;
            Dictionary<string, string> properties = new Dictionary<string, string>();
            #region Fechas
            DateTime fechaBase = DateTimeHelper.parseFromBUKFormat(periodo.month);
            FechasProcesamientoVM fechas = DateTimeHelper.calculateProcessDate(configs, fechaBase, Empresa);
            FileLogHelper.log(LogConstants.cutOffDate, LogConstants.get, "", "", fechas, Empresa);
            DateTime startDate = fechas.PermisosStartDate;
            DateTime endDate = fechas.PermisosEndDate;
            FileLogHelper.log(LogConstants.general, LogConstants.get, "", "EMPIEZA SYNCTIMEOFF", null, Empresa);
            properties["EMPRESA"] = Empresa.Empresa;
            properties["startDate"] = DateTimeHelper.parseToGVFormat(startDate);
            properties["endDate"] = DateTimeHelper.parseToGVFormat(endDate);
            #endregion

            #region Usuarios
            FileLogHelper.log(LogConstants.general, LogConstants.get, "", "PIDIENDO EMPLEADOS A BUK", null, Empresa);
            List<Employee> employees = companyConfiguration.EmployeeBusiness.GetEmployeesForSync(Empresa, companyConfiguration, startDate, endDate);

            FileLogHelper.log(LogConstants.general, LogConstants.get, "", "PIDIENDO USUARIOS A GV", null, Empresa);
            var persons = companyConfiguration.UserBusiness.GetUsersForSync(Empresa, companyConfiguration, employees, Operacion.PERMISOS);
            List<User> users = persons.Item1;
            employees = persons.Item2;
            properties["users"] = users.Count.ToString();
            #endregion

            #region PermisosBUK
            FileLogHelper.log(LogConstants.general, LogConstants.get, "", "PIDIENDO LICENCIAS A BUK", null, Empresa);
            List<Licence> licences = companyConfiguration.LicenceBusiness.GetLicences(startDate, endDate, Empresa, companyConfiguration);
            properties["GetLICENCES"] = licences.Count.ToString();


            FileLogHelper.log(LogConstants.general, LogConstants.get, "", "PIDIENDO TIPO PERMISOS A BUK", null, Empresa);
            List<PermissionType> permissionTypesBuk = companyConfiguration.PermissionBusiness.GetPermissionTypes(Empresa, companyConfiguration);


            FileLogHelper.log(LogConstants.general, LogConstants.get, "", "PIDIENDO PERMISOS A BUK", null, Empresa);
            List<Permission> permissions = companyConfiguration.PermissionBusiness.GetPermissions(startDate, endDate, Empresa, companyConfiguration);
            permissions = companyConfiguration.PermissionBusiness.GetPermissionWithMatch(permissions, permissionTypesBuk, Empresa, companyConfiguration);
            properties["GetPERMISSIONS"] = permissions.Count.ToString();


            FileLogHelper.log(LogConstants.general, LogConstants.get, "", "PIDIENDO VACACIONES A BUK", null, Empresa);
            List<Vacation> vacations = companyConfiguration.VacationBusiness.GetVacations(startDate, endDate, Empresa, companyConfiguration);
            properties["GetVacations"] = vacations.Count.ToString();

            FileLogHelper.log(LogConstants.general, LogConstants.get, "", "PIDIENDO SUSPENSIONES A BUK", null, Empresa);
            List<Suspension> suspensions = companyConfiguration.SuspensionBusiness.GetSuspensionsBUK(startDate, endDate, Empresa, companyConfiguration);
            properties["GetSUSPENSIONS"] = suspensions.Count.ToString();
            #endregion

            List<TimeOffToAdd> timeOffsToAdd = new List<TimeOffToAdd>();
            List<TimeOffToDelete> timeOffsToDelete = new List<TimeOffToDelete>();
            List<Absence> absencesEmployeesNotGV = new List<Absence>();
            List<Vacation> vacationsEmployeesNotGV = new List<Vacation>();

            DateTime startDateGV = startDate;
            DateTime endDateGV = endDate;

            FileLogHelper.log(LogConstants.general, LogConstants.get, "", "PIDIENDO PERMISOS A GV", null, Empresa);
            List<TimeOff> timeOffs = GetTimeOffs(users, startDateGV, endDateGV, Empresa, companyConfiguration);

            List<AbsenceType> subTypes = new List<AbsenceType>();

            FileLogHelper.log(LogConstants.general, LogConstants.get, "", "PIDIENDO SUBTIPOS DE LICENCIA", null, Empresa);
            subTypes.AddRange(companyConfiguration.AbsenceBusiness.GetSubTypes(Empresa, BUKMacroAbsenceTypes.Licencia, companyConfiguration));

            FileLogHelper.log(LogConstants.general, LogConstants.get, "", "PIDIENDO SUBTIPOS DE PERMISO", null, Empresa);
            subTypes.AddRange(companyConfiguration.AbsenceBusiness.GetSubTypes(Empresa, BUKMacroAbsenceTypes.Permiso, companyConfiguration));

            FileLogHelper.log(LogConstants.general, LogConstants.get, "", "PIDIENDO TIPOS DE PERMISO A GV", null, Empresa);
            List<TimeOffType> gvTypes = GetTypes(Empresa, companyConfiguration);

            FileLogHelper.log(LogConstants.general, LogConstants.get, "", "PROCESANDO OPERACIONES A REALIZAR CON PERMISOS", null, Empresa);
            var timeoffsBuilt = this.prepareChanges(licences, permissions, vacations, timeOffs, suspensions, users, Empresa, subTypes, gvTypes, startDate, endDate, companyConfiguration, employees);
            timeOffsToAdd = timeoffsBuilt.Item1;

            List<TimeOffToAdd> conErroresAdd = new List<TimeOffToAdd>();
            if (!CollectionsHelper.IsNullOrEmpty<TimeOffToAdd>(timeOffsToAdd))
            {
                properties["AddTimeOffs"] = timeOffsToAdd.Count.ToString();
                FileLogHelper.log(LogConstants.general, LogConstants.get, "", "ENVIANDO PERMISOS A GV", null, Empresa);
                conErroresAdd.AddRange(AddTimeOffs(timeOffsToAdd, Empresa, companyConfiguration));
            }

            properties["ErrorAddingTimeOffs"] = conErroresAdd.Count.ToString();
            timeOffsToDelete = timeoffsBuilt.Item2;

            List<TimeOffToDelete> conErroresDelete = new List<TimeOffToDelete>();
            if (!CollectionsHelper.IsNullOrEmpty<TimeOffToDelete>(timeOffsToDelete))
            {
                properties["AddTimeOffs"] = timeOffsToAdd.Count.ToString();
                FileLogHelper.log(LogConstants.general, LogConstants.get, "", "ELIMINANDO PERMISOS EN GV", null, Empresa);
                conErroresDelete.AddRange(DeleteTimeOffs(timeOffsToDelete, Empresa, companyConfiguration));
            }

            properties["ErrorDeletingTimeOffs"] = conErroresDelete.Count.ToString();

            List<User> usersToDeactivateBySuspension = timeoffsBuilt.Item3;
            if (!CollectionsHelper.IsNullOrEmpty<User>(usersToDeactivateBySuspension))
            {
                properties["DeactivateUsersBySuspensions"] = usersToDeactivateBySuspension.Count.ToString();
                FileLogHelper.log(LogConstants.general, LogConstants.get, "", "DESACTIVANDO USUARIOS EN GV POR SUSPENSIONES", null, Empresa);
                companyConfiguration.UserBusiness.DeactivateUsers(usersToDeactivateBySuspension, Empresa, companyConfiguration);
            }
            properties["ErrorDeletingTimeOffs"] = conErroresDelete.Count.ToString();

            InsightHelper.logMetric("SyncTimeOffs", DateTime.Now - startMetric, properties);
        }

        /// <summary>
        /// Devuelve los permisos desde GV
        /// </summary>
        protected virtual List<TimeOff> GetTimeOffs(List<User> users, DateTime startDate, DateTime endDate, SesionVM Empresa, CompanyConfiguration companyConfiguration)
        {
            List<string> userIdentifiers = users.Select(u => u.Identifier).ToList();
            List<TimeOff> timeOffs = new List<TimeOff>();
            if (userIdentifiers.Count > 0)
            {
                int paso = CommonHelper.calculateIterationIncrement(users.Count, (endDate - startDate).Days);
                var buckets = DateTimeHelper.Batch(startDate, endDate, OperationalConsts.MAXIMUN_AMOUNT_OF_DAYS);
                object _lock = new object();
                ParallelOptions pOptions = new ParallelOptions();
                pOptions.MaxDegreeOfParallelism = OperationalConsts.MAXIMUN_PARALLEL_PROCESS;
                Parallel.ForEach(buckets, pOptions, bucket =>
                {
                    try
                    {
                        for (int i = 0; i <= userIdentifiers.Count; i += paso)
                        {
                            List<string> iterUsers = userIdentifiers.Skip(i).Take(paso).ToList();
                            if (!iterUsers.IsNullOrEmpty())
                            {
                                int final = (i + paso) > userIdentifiers.Count ? userIdentifiers.Count : i + paso;
                                FileLogHelper.log(LogConstants.general, LogConstants.get, "", "PIDIENDO PERMISOS DE USUARIOS (BATCH DE: " + (i + 1) + " A " + final + " DE UN TOTAL DE: " + userIdentifiers.Count + " )", null, Empresa);
                                string concat = String.Join(',', iterUsers);
                                var timeOffsPartial = companyConfiguration.TimeOffDAO.GetList(new TimeOffFilter { UserIds = concat, StartDate = DateTimeHelper.parseToGVFormat(bucket.StartDate), EndDate = DateTimeHelper.parseToGVFormat(bucket.EndDate) }, Empresa);
                                lock (_lock)
                                {
                                    timeOffs.AddRange(timeOffsPartial);
                                }
                            }

                        }
                    }
                    catch (Exception ex)
                    {
                        InsightHelper.logException(ex, Empresa.Empresa);
                        lock (_lock)
                        {
                            FileLogHelper.log(LogConstants.general, LogConstants.get, "", ex.Message, null, Empresa);
                        }

                        throw new Exception("Incomplete data from GV");
                    }
                });
            }


            return timeOffs;
        }

        /// <summary>
        /// Devuelve los tipos de permiso desde GV
        /// </summary>
        protected virtual List<TimeOffType> GetTypes(SesionVM empresa, CompanyConfiguration companyConfiguration)
        {
            return companyConfiguration.TimeOffDAO.GetTypes(empresa);
        }

        /// <summary>
        /// Devuelve los tipos de permiso desde GV.(sin discriminar por "estado")
        /// </summary>
        protected virtual List<TimeOffType> GetAllTypes(SesionVM empresa, CompanyConfiguration companyConfiguration)
        {
            return companyConfiguration.TimeOffDAO.GetAllTypes(empresa);
        }

        /// <summary>
        /// Construye los permisos a agregar en GV
        /// </summary>
        protected virtual List<TimeOffToAdd> buildTimeOffsToAdd(List<(string, Absence)> bukDataForTimeOffs, List<User> users)
        {
            List<TimeOffToAdd> timeOffsToAdd = new List<TimeOffToAdd>();
            foreach (var item in bukDataForTimeOffs)
            {
                User employee = users.FirstOrDefault(u => int.Parse(u.integrationCode) == item.Item2.employee_id);
                if (employee != null)
                {
                    TimeOffToAdd licenseToAdd = new TimeOffToAdd();
                    licenseToAdd.Origin = TimeOffCreationConsts.Origin;
                    licenseToAdd.StartDate = DateTimeHelper.parseToGVFormat(DateTimeHelper.parseFromBUKFormat(item.Item2.start_date));
                    licenseToAdd.EndDate = DateTimeHelper.parseToGVFormat(DateTimeHelper.parseFromBUKFormat(item.Item2.end_date, true));
                    licenseToAdd.StartTime = "00:00";
                    licenseToAdd.EndTime = "23:59";
                    licenseToAdd.TimeOffTypeId = item.Item1;
                    licenseToAdd.UserIdentifier = employee.Identifier;
                    licenseToAdd.CreatedByIdentifier = TimeOffCreationConsts.CreatedByIdentifier;
                    licenseToAdd.Description = item.Item2.type;
                    timeOffsToAdd.Add(licenseToAdd);
                }
            }

            return timeOffsToAdd;
        }

        /// <summary>
        /// Construye las vacaciones a agregar en GV
        /// </summary>

        protected virtual List<TimeOffToAdd> buildVacations(List<Vacation> vacations, List<User> users)
        {
            List<TimeOffToAdd> timeOffsToAdd = new List<TimeOffToAdd>();
            foreach (Vacation vacation in vacations)
            {
                User employee = users.FirstOrDefault(u => int.Parse(u.integrationCode) == vacation.employee_id);
                if (employee != null)
                {
                    TimeOffToAdd vacationToAdd = new TimeOffToAdd();
                    vacationToAdd.Origin = TimeOffCreationConsts.Origin;
                    vacationToAdd.StartDate = DateTimeHelper.parseToGVFormat(DateTimeHelper.parseFromBUKFormat(vacation.start_date));
                    vacationToAdd.EndDate = DateTimeHelper.parseToGVFormat(DateTimeHelper.parseFromBUKFormat(vacation.end_date, true));
                    vacationToAdd.TimeOffTypeId = TimeOffTypeIds.Vacaciones;
                    vacationToAdd.UserIdentifier = employee.Identifier;
                    vacationToAdd.CreatedByIdentifier = TimeOffCreationConsts.CreatedByIdentifier;
                    timeOffsToAdd.Add(vacationToAdd);
                }
            }

            return timeOffsToAdd;
        }

        /// <summary>
        /// Procesa los permisos traidos de BUK para ver cuales deben agregarse a Geovictoria
        /// </summary>
        /// <param name="absences"></param>
        /// <param name="vacations"></param>
        /// <param name="timeOffs"></param>
        /// <param name="users"></param>
        /// <param name="Empresa"></param>
        /// <returns></returns>
        protected virtual (List<TimeOffToAdd>, List<TimeOffToDelete>, List<User>) prepareChanges(List<Licence> licencias, List<Permission> permissions, List<Vacation> vacations, List<TimeOff> timeOffs, List<Suspension> suspensions, List<User> users, SesionVM Empresa, List<AbsenceType> subTypes, List<TimeOffType> gvTypes, DateTime startDate, DateTime endDate, CompanyConfiguration companyConfiguration, List<Employee> employees)
        {
            List<TimeOffToAdd> toUpsert = new List<TimeOffToAdd>();
            List<TimeOffToDelete> toDelete = new List<TimeOffToDelete>();

            List<Permission> conGoce = permissions.FindAll(p => p.paid == true);
            List<Permission> sinGoce = permissions.FindAll(p => p.paid == false);
            suspensions = suspensions.FindAll(s => s.suspension_type != SuspensionsType.ReduccionJornada);

            Parallel.ForEach(conGoce, cg => { FileLogHelper.log(LogConstants.general, LogConstants.get, "", "Permiso Con Goce :" + cg.Stringify(), null, Empresa); });
            Parallel.ForEach(sinGoce, sg => { FileLogHelper.log(LogConstants.general, LogConstants.get, "", "Permiso Sin Goce :" + sg.Stringify(), null, Empresa); });
            Parallel.ForEach(licencias, l => { FileLogHelper.log(LogConstants.general, LogConstants.get, "", "Permiso Licencia :" + l.Stringify(), null, Empresa); });
            Parallel.ForEach(vacations, v => { FileLogHelper.log(LogConstants.general, LogConstants.get, "", "Permiso Vacacion :" + v.Stringify(), null, Empresa); });
            Parallel.ForEach(suspensions, s => { FileLogHelper.log(LogConstants.general, LogConstants.get, "", "Suspension :" + s.Stringify(), null, Empresa); });

            List<(string, Absence)> licencesToUpsert = processLicences(licencias, timeOffs, users, subTypes, gvTypes, companyConfiguration, Empresa, employees);
            List<(string, Absence)> paidLeavesToUpsert = processPermissions(conGoce, timeOffs, users, subTypes, gvTypes, companyConfiguration, Empresa, employees);
            List<(string, Absence)> leavesToUpsert = processPermissions(sinGoce, timeOffs, users, subTypes, gvTypes, companyConfiguration, Empresa, employees);
            List<Vacation> vacationsToUpsert = processVacations(vacations, timeOffs, users);
            List<User> usersToDeactivateBySuspension = processSuspensionsToDeactivateUsers(suspensions, users);

            toUpsert.AddRange(buildTimeOffsToAdd(licencesToUpsert, users));
            toUpsert.AddRange(buildTimeOffsToAdd(leavesToUpsert, users));
            toUpsert.AddRange(buildTimeOffsToAdd(paidLeavesToUpsert, users));
            toUpsert.AddRange(this.buildVacations(vacationsToUpsert, users));

            List<TimeOff> toToDelete = timeOffs.FindAll(t => !this.matchCase(t, vacations, licencias, permissions, suspensions, users, gvTypes.Where(x => x.Id == t.TimeOffTypeId).First()));
            toToDelete = toToDelete.FindAll(t => this.isConsidered(t));

            toDelete = toToDelete.ConvertAll(buildToDelete);

            return (toUpsert, toDelete, usersToDeactivateBySuspension);
        }

        /// <summary>
        /// Procesa las licencias y devuelve las que deben agregarse a GV
        /// </summary>
        protected virtual List<(string, Absence)> processLicences(List<Licence> licencias, List<TimeOff> timeOffs, List<User> users, List<AbsenceType> subTypes, List<TimeOffType> gvTypes, CompanyConfiguration companyConfiguration, SesionVM Empresa, List<Employee> employees)
        {
            List<(string, Absence)> licencesToUpsert = new List<(string, Absence)>();
            Employee employee = new Employee();
            foreach (var item in licencias)
            {
                User usuario = users.FirstOrDefault(u => u.integrationCode == item.employee_id.ToString() && u.Enabled == 1);
                if (usuario != null)
                {
                    string gvTypoId = "";
                    employee = employees.FirstOrDefault(x => x.person_id == item.employee_id);
                    if (matchType(item, subTypes, gvTypes, employee, out gvTypoId))
                    {
                        TimeOff match = timeOffs.FirstOrDefault(t => matchCase(t, item, gvTypoId, usuario));

                        if (match == null)
                        {
                            licencesToUpsert.Add((gvTypoId, item));
                        }
                    }
                    else
                    {
                        var typo = subTypes.FirstOrDefault(s => s.id == item.licence_type_id);
                        if (typo != null)
                        {
                            FileLogHelper.log(LogConstants.general, LogConstants.get, "", "CREANDO TIPO PERMISO: " + item.licence_type_id, null, Empresa);
                            try
                            {
                                TimeOffType newType = AddGVType(typo, Empresa, item.days_count, null, companyConfiguration);
                                gvTypes.Add(newType);
                                licencesToUpsert.Add((newType.Id, item));
                            }
                            catch (Exception)
                            {
                                FileLogHelper.log(LogConstants.general, LogConstants.get, "", "NO SE PUDO CREAR EL TIPO PERMISO: " + item.licence_type_id, null, Empresa);
                            }
                        }
                    }
                }
            }

            return licencesToUpsert;
        }


        /// <summary>
        /// Procesa los permisos y devuelve los que deben agregarse a GV
        /// </summary>
        protected virtual List<(string, Absence)> processPermissions(List<Permission> sinGoce, List<TimeOff> timeOffs, List<User> users, List<AbsenceType> subTypes, List<TimeOffType> gvTypes, CompanyConfiguration companyConfiguration, SesionVM Empresa, List<Employee> employees)
        {
            List<(string, Absence)> leavesToUpsert = new List<(string, Absence)>();
            foreach (var item in sinGoce)
            {
                User usuario = users.FirstOrDefault(u => u.userCompanyIdentifier == item.employee_id.ToString() || u.integrationCode == item.employee_id.ToString() && u.Enabled == 1);
                if (usuario != null)
                {
                    string gvTypoId = "";
                    Employee employee = employees.FirstOrDefault(x => x.person_id == item.employee_id);
                    if (this.matchType(item, subTypes, gvTypes, employee, out gvTypoId))
                    {
                        TimeOff match = timeOffs.FirstOrDefault(t => matchCase(t, item, gvTypoId, usuario));
                        if (match == null)
                        {
                            var currentSubtype = subTypes.Where(x => x.id == item.permission_type_id).First();
                            var currentGvType = gvTypes.Where(x => x.Description == currentSubtype.description).First();
                            Boolean.TryParse(currentGvType.IsParcial, out var is_parcial_permission);
                            if (!is_parcial_permission)
                            {
                                leavesToUpsert.Add((gvTypoId, item));
                            }
                        }
                    }
                    else
                    {
                        var typo = subTypes.FirstOrDefault(s => s.id == item.permission_type_id);
                        if (typo != null)
                        {
                            TimeOffType newType = AddGVType(typo, Empresa, item.days_count, null, companyConfiguration);
                            gvTypes.Add(newType);
                            leavesToUpsert.Add((newType.Id, item));
                        }
                        else if (!gvTypoId.IsNullOrEmpty())
                        {
                            FileLogHelper.log(LogConstants.general, LogConstants.get, "", "CREANDO TIPO PERMISO: " + item.permission_type_id, null, Empresa);
                            try
                            {
                                TimeOffType newType = AddGVType(typo, Empresa, item.days_count, item.paid, companyConfiguration);
                                gvTypes.Add(newType);
                                leavesToUpsert.Add((newType.Id, item));
                            }
                            catch (Exception)
                            {
                                FileLogHelper.log(LogConstants.general, LogConstants.get, "", "NO SE PUDO CREAR EL TIPO PERMISO: " + item.permission_type_id, null, Empresa);
                            }
                        }
                    }
                }
            }

            return leavesToUpsert;
        }

        /// <summary>
        /// Procesa las vacaciones y devuelve las que deben agregarse a GV
        /// </summary>
        protected virtual List<Vacation> processVacations(List<Vacation> vacations, List<TimeOff> timeOffs, List<User> users)
        {
            List<Vacation> vacationsToUpsert = new List<Vacation>();
            foreach (var item in vacations)
            {
                User usuario = users.FirstOrDefault(u => u.userCompanyIdentifier == item.employee_id.ToString() || u.integrationCode == item.employee_id.ToString() && u.Enabled == 1);
                if (usuario != null)
                {
                    TimeOff match = timeOffs.FirstOrDefault(t => matchCase(t, item, TimeOffTypeIds.Vacaciones, usuario));
                    if (match == null)
                    {
                        vacationsToUpsert.Add(item);
                    }
                }
            }

            return vacationsToUpsert;
        }

        /// <summary>
        /// Procesa las suspensiones y devuelve las que deben agregarse a GV
        /// </summary>
        protected virtual List<(string, Absence)> processSuspensions(List<Suspension> suspensions, List<TimeOff> timeOffs, List<User> users, List<AbsenceType> subTypes, List<TimeOffType> gvTypes, CompanyConfiguration companyConfiguration, SesionVM Empresa)
        {
            List<(string, Absence)> suspensionsToUpsert = new List<(string, Absence)>();
            foreach (var item in suspensions)
            {
                User usuario = users.FirstOrDefault(u => u.userCompanyIdentifier == item.employee_id.ToString() || u.integrationCode == item.employee_id.ToString());
                if (usuario != null)
                {
                    string gvTypoId = "";
                    item.type = item.suspension_type;

                    if (matchType(item, gvTypes, out gvTypoId))
                    {
                        TimeOff match = timeOffs.FirstOrDefault(t => matchCase(t, item, gvTypoId, usuario));
                        if (match == null)
                        {
                            suspensionsToUpsert.Add((gvTypoId, item));
                        }
                    }
                    else
                    {
                        if (!string.IsNullOrWhiteSpace(item.suspension_type))
                        {
                            TimeOffType newType = AddSuspensionType(item.suspension_type, Empresa, companyConfiguration);
                            gvTypes.Add(newType);
                            suspensionsToUpsert.Add((newType.Id, item));
                        }
                        else if (!gvTypoId.IsNullOrEmpty())
                        {
                            FileLogHelper.log(LogConstants.general, LogConstants.get, "", "CREANDO TIPO SUSPENSION: " + item.suspension_type, null, Empresa);
                            try
                            {
                                TimeOffType newType = AddSuspensionType(SuspensionsType.SuspensionGeneral, Empresa, companyConfiguration);
                                gvTypes.Add(newType);
                                suspensionsToUpsert.Add((newType.Id, item));
                            }
                            catch (Exception)
                            {
                                FileLogHelper.log(LogConstants.general, LogConstants.get, "", "NO SE PUDO CREAR EL TIPO SUSPENSION: " + item.suspension_type, null, Empresa);
                            }
                        }
                    }
                }
            }

            return suspensionsToUpsert;
        }


        /// <summary>
        /// Procesa las suspensiones y devuelve los usuarios que se deben desactivar
        /// </summary>
        protected List<User> processSuspensionsToDeactivateUsers(List<Suspension> suspensions, List<User> users)
        {
            List<User> usersToDeactivate = new List<User>();
            foreach (var item in suspensions)
            {
                User usuario = users.FirstOrDefault(u => !string.IsNullOrWhiteSpace(u.integrationCode) && u.integrationCode == item.employee_id.ToString());
                if (usuario != null && DateTimeHelper.IsInTimeSpan(DateTime.Today, DateTimeHelper.parseFromBUKFormat(item.start_date), DateTimeHelper.parseFromBUKFormat(item.end_date)) && (!usuario.Enabled.HasValue || (usuario.Enabled.HasValue && usuario.Enabled.Value == 1)))
                {
                    if (!usersToDeactivate.Any(u => u.Identifier == usuario.Identifier))
                    {
                        usersToDeactivate.Add(usuario);
                    }

                }

            }
            return usersToDeactivate;
        }

        /// <summary>
        /// Procesa las suspensiones y devuelve los usuarios que se deben activar
        /// </summary>
        protected List<User> processSuspensionsToActivateUsers(List<Suspension> suspensions, List<User> users)
        {
            List<User> usersToActivate = new List<User>();
            foreach (var item in suspensions)
            {
                User usuario = users.FirstOrDefault(u => !string.IsNullOrWhiteSpace(u.integrationCode) && u.integrationCode == item.employee_id.ToString());
                if (usuario != null && !DateTimeHelper.IsInTimeSpan(DateTime.Today, DateTimeHelper.parseFromBUKFormat(item.start_date), DateTimeHelper.parseFromBUKFormat(item.end_date)) && (!usuario.Enabled.HasValue || (usuario.Enabled.HasValue && usuario.Enabled.Value == 0)))
                {
                    if (!usersToActivate.Any(u => u.Identifier == usuario.Identifier))
                    {
                        usersToActivate.Add(usuario);
                    }
                }
            }

            return usersToActivate;
        }


        /// <summary>
        /// Determina si un permiso traido de GV y que no tien match con ninguno de BUK es de los tipos de permisos comprendidos en la integracion
        /// </summary>
        /// <param name="timeOff"></param>
        /// <returns></returns>
        protected virtual bool isConsidered(TimeOff timeOff)
        {
            return (timeOff.TimeOffTypeId == TimeOffTypeIds.Licencia)
                || (timeOff.TimeOffTypeId == TimeOffTypeIds.ConGoce)
                || (timeOff.TimeOffTypeId == TimeOffTypeIds.SinGoce)
                || (timeOff.TimeOffTypeId == TimeOffTypeIds.Vacaciones)
                || (timeOff.TimeOffTypeId == SuspensionsGVType.SuspensionTemporal)
                || (timeOff.TimeOffTypeId == SuspensionsGVType.ActoAutoridad)
                || (timeOff.TimeOffTypeId == SuspensionsGVType.SuspensionGeneral);
        }

        /// <summary>
        /// Determina si un permiso traido de GV y que no tien match con ninguno de BUK esta dentro de los limites del periodo actual
        /// </summary>
        /// <param name="timeOff"></param>
        /// <returns></returns>
        protected virtual bool isConsideredPeriod(TimeOff timeOff, DateTime startDate, DateTime endDate)
        {
            return this.isConsidered(timeOff) && DateTimeHelper.parseFromGVFormat(timeOff.Starts) >= startDate && DateTimeHelper.parseFromGVFormat(timeOff.Ends) <= startDate;

        }

        /// <summary>
        /// Determina si un permiso traido de BUK es igual a uno de GeoVictoria
        /// </summary>
        /// <param name="timeOff"></param>
        /// <param name="absence"></param>
        /// <param name="expectedIdentifier"></param>
        /// <param name="user"></param>
        /// <returns></returns>
        protected virtual bool matchCase(TimeOff timeOff, Absence absence, string expectedIdentifier, User user)
        {
            return user.Identifier == timeOff.UserIdentifier
                && (DateTimeHelper.parseFromGVFormat(timeOff.Starts) == DateTimeHelper.parseFromBUKFormat(absence.start_date))
                && (DateTimeHelper.parseFromGVFormat(timeOff.Ends) == DateTimeHelper.parseFromBUKFormat(absence.end_date, true))
                && expectedIdentifier == timeOff.TimeOffTypeId;
        }



        /// <summary>
        /// Determina si un tipo de Licencia ya existe en los tipos de permiso en GV
        /// </summary>
        protected virtual bool matchType(Licence licence, List<AbsenceType> subTypes, List<TimeOffType> gvTypes, Employee employee, out string gvTypoId)
        {
            var typo = subTypes.FirstOrDefault(s => s.id == licence.licence_type_id);

            gvTypoId = "";
            if (typo != null)
            {
                if (typo.description == StandardTypes.BukLicencia)
                {
                    typo.description = StandardTypes.GVLicencia;
                }

                var gvTypo = gvTypes.FirstOrDefault(g => g.Description == typo.description);
                if (gvTypo != null)
                {
                    gvTypoId = gvTypo.Id;
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Determina si un tipo de Permiso ya existe en los tipos de permiso en GV
        /// </summary>
        protected virtual bool matchType(Permission permission, List<AbsenceType> subTypes, List<TimeOffType> gvTypes, Employee employee, out string gvTypoId)
        {
            var typo = subTypes.FirstOrDefault(s => s.id == permission.permission_type_id);
            gvTypoId = "";
            if (typo != null)
            {
                if (permission.paid != typo.with_pay)
                {
                    if (permission.paid)
                    {
                        typo.description += " con goce";
                    }
                    else
                    {
                        typo.description += " sin goce";
                    }

                }
                else if (typo.description == StandardTypes.BukPermisoConGoce)
                {
                    typo.description = StandardTypes.GVPermisoConGoce;
                }
                else if (typo.description == StandardTypes.BukPermisoSinGoce)
                {
                    typo.description = StandardTypes.GVPermisoSinGoce;
                }
                var gvTypo = gvTypes.FirstOrDefault(g => g.Description == typo.description);
                if (gvTypo != null)
                {
                    gvTypoId = gvTypo.Id;
                    if (permission.paid == typo.with_pay)
                    {
                        return true;
                    }

                }

            }
            return false;
        }

        /// <summary>
        /// Determina si un tipo de Suspension ya existe en los tipos de permiso en GV
        /// </summary>
        protected virtual bool matchType(Suspension suspension, List<TimeOffType> gvTypes, out string gvTypoId)
        {


            gvTypoId = "";
            if (!string.IsNullOrWhiteSpace(suspension.suspension_type))
            {
                if (suspension.suspension_type == SuspensionsType.ActoAutoridad)
                {
                    suspension.suspension_type = SuspensionsGVType.ActoAutoridad;

                }
                else if (suspension.suspension_type == SuspensionsType.SuspensionTemporal)
                {
                    suspension.suspension_type = SuspensionsGVType.SuspensionTemporal;
                }
                else
                {
                    suspension.suspension_type = SuspensionsType.SuspensionGeneral;
                }
                var gvTypo = gvTypes.FirstOrDefault(g => g.Description == suspension.suspension_type);
                if (gvTypo != null)
                {
                    gvTypoId = gvTypo.Id;
                    return true;
                }

            }
            else
            {
                suspension.suspension_type = SuspensionsType.SuspensionGeneral;
                var gvTypo = gvTypes.FirstOrDefault(g => g.Description == suspension.suspension_type);
                if (gvTypo != null)
                {
                    gvTypoId = gvTypo.Id;
                    return true;
                }
            }


            return false;
        }

        /// <summary>
        /// Determina si una vacación traida de BUK es igual a un permiso de GeoVictoria
        /// </summary>
        /// <param name="timeOff"></param>
        /// <param name="vacation"></param>
        /// <param name="expectedIdentifier"></param>
        /// <param name="user"></param>
        /// <returns></returns>
        protected virtual bool matchCase(TimeOff timeOff, Vacation vacation, string expectedIdentifier, User user)
        {
            return user.Identifier == timeOff.UserIdentifier
                && (DateTimeHelper.parseFromGVFormat(timeOff.Starts) == DateTimeHelper.parseFromBUKFormat(vacation.start_date))
                && (DateTimeHelper.parseFromGVFormat(timeOff.Ends) == DateTimeHelper.parseFromBUKFormat(vacation.end_date, true))
                && expectedIdentifier == timeOff.TimeOffTypeId;
        }

        /// <summary>
        /// Determina si un permiso de GeoVictoria tiene algun match con algun permiso de BUK
        /// </summary>
        /// <param name="timeOff"></param>
        /// <param name="vacations"></param>
        /// <param name="absences"></param>
        /// <param name="users"></param>
        /// <returns></returns>
        protected virtual bool matchCase(TimeOff timeOff, List<Vacation> vacations, List<Licence> licences, List<Permission> permissions, List<Suspension> suspensions, List<User> users, TimeOffType gvType)
        {
            User user = users.FirstOrDefault(u => timeOff.UserIdentifier == u.Identifier);
            if (user == null)
            {
                return false;
            }

            var integrationCodesList = user.integrationCode.Split(',');

            List<Permission> permissionsCandidates = permissions.FindAll(a => (user.userCompanyIdentifier == a.employee_id.ToString() || integrationCodesList.Contains(a.employee_id.ToString()))
                && (DateTimeHelper.parseFromGVFormat(timeOff.Starts) == DateTimeHelper.parseFromBUKFormat(a.start_date))
                && (DateTimeHelper.parseFromGVFormat(timeOff.Ends) == DateTimeHelper.parseFromBUKFormat(a.end_date, true)));
            foreach (var item in permissionsCandidates)
            {
                if (gvType.IsParcial == "False" && item.paid && gvType.IsPayable == "True")
                {
                    return true;
                }

                if (gvType.IsParcial == "False" && !item.paid && gvType.IsPayable == "False")
                {
                    return true;
                }
            }
            List<Licence> licencesCandidates = licences.FindAll(a => (user.userCompanyIdentifier == a.employee_id.ToString() || integrationCodesList.Contains(a.employee_id.ToString()))
                && (DateTimeHelper.parseFromGVFormat(timeOff.Starts) == DateTimeHelper.parseFromBUKFormat(a.start_date))
                && (DateTimeHelper.parseFromGVFormat(timeOff.Ends) == DateTimeHelper.parseFromBUKFormat(a.end_date, true)));
            if (!licencesCandidates.IsNullOrEmpty() && timeOff.TimeOffTypeId == TimeOffTypeIds.Licencia)
            {
                return true;
            }
            List<Suspension> suspensionsCandidates = suspensions.FindAll(a => (user.userCompanyIdentifier == a.employee_id.ToString() || integrationCodesList.Contains(a.employee_id.ToString()))
                && (DateTimeHelper.parseFromGVFormat(timeOff.Starts) == DateTimeHelper.parseFromBUKFormat(a.start_date))
                && (DateTimeHelper.parseFromGVFormat(timeOff.Ends) == DateTimeHelper.parseFromBUKFormat(a.end_date, true)));
            if (!suspensionsCandidates.IsNullOrEmpty() &&
                (timeOff.TimeOffTypeId == SuspensionsGVType.SuspensionTemporal
                || timeOff.TimeOffTypeId == SuspensionsGVType.ActoAutoridad
                || timeOff.TimeOffTypeId == SuspensionsGVType.SuspensionGeneral))
            {
                return true;
            }

            List<Vacation> vacationsCandidates = vacations.FindAll(v => (user.userCompanyIdentifier == v.employee_id.ToString() || integrationCodesList.Contains(v.employee_id.ToString()))
                && (DateTimeHelper.parseFromGVFormat(timeOff.Starts) == DateTimeHelper.parseFromBUKFormat(v.start_date))
                && (DateTimeHelper.parseFromGVFormat(timeOff.Ends) == DateTimeHelper.parseFromBUKFormat(v.end_date, true)));
            return !vacationsCandidates.IsNullOrEmpty() && timeOff.TimeOffTypeId == TimeOffTypeIds.Vacaciones;

        }

        /// <summary>
        /// Convierte un permiso de Geovictoria al formato que debe enviarse para su eliminación
        /// </summary>
        /// <param name="timeOff"></param>
        /// <returns></returns>
        protected virtual TimeOffToDelete buildToDelete(TimeOff timeOff)
        {
            TimeOffToDelete toDelete = new TimeOffToDelete();
            toDelete.TypeIdentifier = timeOff.TimeOffTypeId;
            toDelete.UserIdentifier = timeOff.UserIdentifier;
            toDelete.Start = timeOff.Starts;
            toDelete.End = timeOff.Ends;
            toDelete.Description = timeOff.Description;
            return toDelete;
        }

        /// <summary>
        /// Agrega los permisos nuevos a GV
        /// </summary>
        protected virtual List<TimeOffToAdd> AddTimeOffs(List<TimeOffToAdd> timeOffsToAdd, SesionVM Empresa, CompanyConfiguration companyConfiguration)
        {
            List<TimeOffToAdd> conErrores = new List<TimeOffToAdd>();
            ParallelOptions pOptions = new ParallelOptions();
            pOptions.MaxDegreeOfParallelism = 2;
            int total = timeOffsToAdd.Count;
            FileLogHelper.log(LogConstants.timeOff, LogConstants.get, "", "ENVIANDO OPERACIONES A GV (PERMISOS) PARA UN TOTAL DE " + total, null, Empresa);
            int current = 0;
            object lockCurrent = new object();
            Parallel.ForEach(timeOffsToAdd, pOptions, timeOff =>
            {
                lock (lockCurrent)
                {
                    current++;
                }

                string endHours = timeOff.EndDate.Substring(8);
                if ((timeOff.StartDate == timeOff.EndDate) && (endHours == "000000"))
                {
                    timeOff.EndDate = timeOff.EndDate.Substring(0, 8) + "235959";
                }

                if ((!string.IsNullOrEmpty(timeOff.StartTime) && string.IsNullOrEmpty(timeOff.EndTime)) || (string.IsNullOrEmpty(timeOff.StartTime) && string.IsNullOrEmpty(timeOff.EndTime)))
                {
                    timeOff.StartTime = timeOff.StartDate.Substring(8, 2) + ":" + timeOff.StartDate.Substring(10, 2);
                    timeOff.EndTime = timeOff.EndDate.Substring(8, 2) + ":" + timeOff.EndDate.Substring(10, 2);
                }

                if (!companyConfiguration.TimeOffDAO.Add(timeOff, Empresa))
                {
                    FileLogHelper.log(LogConstants.timeOff, LogConstants.error_add, "", " Error al enviar permiso ", timeOff, Empresa);
                    lock (lockCurrent)
                    {
                        conErrores.Add(timeOff);
                    }
                }
                else
                {
                    FileLogHelper.log(LogConstants.timeOff, LogConstants.add, "", "", timeOff, Empresa);
                }
            });

            return conErrores;
        }

        /// <summary>
        /// Elimina los permisos en GV
        /// </summary>
        protected virtual List<TimeOffToDelete> DeleteTimeOffs(List<TimeOffToDelete> timeOffsToDelete, SesionVM Empresa, CompanyConfiguration companyConfiguration)
        {
            List<TimeOffToDelete> conErrores = new List<TimeOffToDelete>();
            int total = timeOffsToDelete.Count;
            FileLogHelper.log(LogConstants.timeOff, LogConstants.get, "", "ENVIANDO OPERACIONES (ELIMINAR) A GV (PERMISOS) PARA UN TOTAL DE " + total, null, Empresa);
            int current = 0;

            foreach (TimeOffToDelete timeOff in timeOffsToDelete)
            {
                current++;
                if (!companyConfiguration.TimeOffDAO.Delete(timeOff, Empresa))
                {
                    FileLogHelper.log(LogConstants.timeOff, LogConstants.error_deactivate, "", " Error al eliminar permiso ", timeOff, Empresa);
                    conErrores.Add(timeOff);
                }
                else
                {
                    FileLogHelper.log(LogConstants.timeOff, LogConstants.delete, "", "", timeOff, Empresa);
                }
            }

            return conErrores;
        }

        /// <summary>
        /// Agrega un tipo de permiso nuevo en GV
        /// </summary>
        protected virtual TimeOffType AddGVType(AbsenceType bukType, SesionVM empresa, double duration, bool? withpay, CompanyConfiguration companyConfiguration)
        {
            TimeOffType newType = new TimeOffType();
            newType.IsPayable = bukType.with_pay.ToString();
            newType.Description = bukType.description;
            if (withpay.HasValue)
            {
                if (withpay.Value)
                {
                    newType.Description += " con goce";
                }
                else
                {
                    newType.Description += " sin goce";
                }
            }
            if (duration == 1)
            {
                newType.IsParcial = "false";
            }
            else if (duration % 1 == 0)
            {
                newType.IsParcial = "false";
            }
            else
            {
                newType.IsParcial = "true";
            }

            FileLogHelper.log(LogConstants.timeOff, LogConstants.get, "", "TRATANDO DE CREAR TIPO DE PERMISO: " + newType.Stringify(), null, empresa);
            return companyConfiguration.TimeOffDAO.AddType(newType, empresa);
        }

        /// <summary>
        /// Agrega un tipo de suspension nuevo en GV
        /// </summary>
        protected virtual TimeOffType AddSuspensionType(String suspension_type, SesionVM empresa, CompanyConfiguration companyConfiguration)
        {
            TimeOffType newType = new TimeOffType();
            newType.IsPayable = "false";
            newType.Description = suspension_type;
            newType.IsParcial = "false";
            FileLogHelper.log(LogConstants.timeOff, LogConstants.get, "", "TRATANDO DE CREAR TIPO DE PERMISO: " + newType.Stringify(), null, empresa);
            return companyConfiguration.TimeOffDAO.AddType(newType, empresa);
        }
    }
}
