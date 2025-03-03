using API.BUK.DTO;
using API.BUK.DTO.Consts;
using API.GV.DTO;
using API.GV.DTO.Consts;
using API.GV.DTO.Filters;
using API.Helpers.Commons;
using API.Helpers.VM;
using API.Helpers.VM.Consts;
using BusinessLogic.Interfaces.VM;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BusinessLogic.Implementation
{
    public class TimeOffMultipleSheetBusiness : TimeOffBusiness
    {
        public override void Sync(SesionVM Empresa, ProcessPeriod periodo, List<PeriodConfiguration> configs, CompanyConfiguration companyConfiguration)
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
            FileLogHelper.log(LogConstants.general, LogConstants.get, "", "EMPIEZA SYNCTIMEOFF-MULTISHEET", null, Empresa);
            properties["EMPRESA"] = Empresa.Empresa;
            properties["startDate"] = DateTimeHelper.parseToGVFormat(startDate);
            properties["endDate"] = DateTimeHelper.parseToGVFormat(endDate);
            #endregion

            #region Usuarios
            FileLogHelper.log(LogConstants.general, LogConstants.get, "", "PIDIENDO EMPLEADOS A BUK", null, Empresa);
            List<Employee> employees = new List<Employee>();
            employees = companyConfiguration.EmployeeBusiness.GetEmployeesForSync(Empresa, companyConfiguration, startDate, endDate);

            FileLogHelper.log(LogConstants.general, LogConstants.get, "", "PIDIENDO USUARIOS A GV", null, Empresa);
            var persons = companyConfiguration.UserBusiness.GetUsersForSync(Empresa, companyConfiguration, employees, Operacion.PERMISOS);
            List<User> users = persons.Item1;
            employees = persons.Item2;
            properties["users"] = users.Count.ToString();
            #endregion

            List<Employee> sheetsEndingActualPeriod = getSheetsEndingActualPeriod(employees, startDate, endDate);

            #region PermisosBUK

            FileLogHelper.log(LogConstants.general, LogConstants.get, "", "PIDIENDO LICENCIAS A BUK", null, Empresa);
            List<Licence> licences = companyConfiguration.LicenceBusiness.GetLicences(startDate, endDate, Empresa, companyConfiguration);
            properties["GetLICENCES"] = licences.Count.ToString();
            //licences = licences.FindAll(l => l.employee_id.ToString() == users[0].integrationCode);

            FileLogHelper.log(LogConstants.general, LogConstants.get, "", "PIDIENDO TIPO PERMISOS A BUK", null, Empresa);
            List<PermissionType> permissionTypesBuk = companyConfiguration.PermissionBusiness.GetPermissionTypes(Empresa, companyConfiguration);

            FileLogHelper.log(LogConstants.general, LogConstants.get, "", "PIDIENDO PERMISOS A BUK", null, Empresa);
            List<Permission> permissions = companyConfiguration.PermissionBusiness.GetPermissions(startDate, endDate, Empresa, companyConfiguration);
            permissions = companyConfiguration.PermissionBusiness.GetPermissionWithMatch(permissions, permissionTypesBuk, Empresa, companyConfiguration);
            properties["GetPERMISSIONS"] = permissions.Count.ToString();
            //permissions = permissions.FindAll(l => l.employee_id.ToString() == users[0].integrationCode);

            FileLogHelper.log(LogConstants.general, LogConstants.get, "", "PIDIENDO VACACIONES A BUK", null, Empresa);
            List<Vacation> vacations = companyConfiguration.VacationBusiness.GetVacations(startDate, endDate, Empresa, companyConfiguration);
            properties["GetVacations"] = vacations.Count.ToString();
            //vacations = vacations.FindAll(l => l.employee_id.ToString() == users[0].integrationCode);

            FileLogHelper.log(LogConstants.general, LogConstants.get, "", "PIDIENDO SUSPENSIONES A BUK", null, Empresa);
            List<Suspension> suspensions = companyConfiguration.SuspensionBusiness.GetSuspensionsBUK(startDate, endDate, Empresa, companyConfiguration);
            properties["GetSUSPENSIONS"] = suspensions.Count.ToString();
            //permissions = permissions.FindAll(l => l.employee_id.ToString() == users[0].integrationCode);
            #endregion


            List<TimeOffToAdd> timeOffsToAdd = new List<TimeOffToAdd>();
            List<TimeOffToDelete> timeOffsToDelete = new List<TimeOffToDelete>();
            List<Absence> absencesEmployeesNotGV = new List<Absence>();
            List<Vacation> vacationsEmployeesNotGV = new List<Vacation>();

            DateTime startDateGV = startDate;
            DateTime endDateGV = endDate;

            List<TimeOff> timeOffs = GetTimeOffs(users, startDateGV, endDateGV, Empresa, companyConfiguration);

            List<AbsenceType> subTypes = new List<AbsenceType>();
            FileLogHelper.log(LogConstants.general, LogConstants.get, "", "PIDIENDO SUBTIPOS DE LICENCIAS", null, Empresa);
            subTypes.AddRange(companyConfiguration.AbsenceBusiness.GetSubTypes(Empresa, BUKMacroAbsenceTypes.Licencia, companyConfiguration));

            FileLogHelper.log(LogConstants.general, LogConstants.get, "", "PIDIENDO SUBTIPOS DE PERMISOS", null, Empresa);
            subTypes.AddRange(companyConfiguration.AbsenceBusiness.GetSubTypes(Empresa, BUKMacroAbsenceTypes.Permiso, companyConfiguration));

            FileLogHelper.log(LogConstants.general, LogConstants.get, "", "PIDIENDO TIPOS DE PERMISO A GV", null, Empresa);
            List<TimeOffType> gvTypes = GetTypes(Empresa, companyConfiguration);

            FileLogHelper.log(LogConstants.general, LogConstants.get, "", "PROCESANDO OPERACIONES A REALIZAR CON PERMISOS", null, Empresa);
            var timeoffsBuilt = prepareChanges(licences, permissions, vacations, timeOffs, suspensions, users, Empresa, subTypes, gvTypes, startDate, endDate, companyConfiguration, employees);
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
            InsightHelper.logMetric("SyncTimeOffs", DateTime.Now - startMetric, properties);
        }

        private List<Employee> getSheetsEndingActualPeriod(List<Employee> employees, DateTime startDate, DateTime endDate)
        {
            List<Employee> sheets = new List<Employee>();
            foreach (var sheet in employees)
            {
                if (sheet.current_job != null && !String.IsNullOrWhiteSpace(sheet.current_job.active_until)
                    && startDate < DateTimeHelper.parseFromBUKFormat(sheet.current_job.active_until) && DateTimeHelper.parseFromBUKFormat(sheet.current_job.active_until) < endDate
                    && (!String.IsNullOrWhiteSpace(sheet.active_since) && startDate > DateTimeHelper.parseFromBUKFormat(sheet.active_since)))
                {
                    sheets.Add(sheet);
                }
            }
            return sheets;
        }

        protected override List<TimeOffToAdd> buildTimeOffsToAdd(List<(string, Absence)> bukDataForTimeOffs, List<User> users)
        {
            List<TimeOffToAdd> timeOffsToAdd = new List<TimeOffToAdd>();
            foreach (var item in bukDataForTimeOffs)
            {
                User employee = users.FirstOrDefault(u => u.integrationCode.Split(',').Contains(item.Item2.employee_id.ToString()));
                if (employee != null)
                {
                    TimeOffToAdd licenseToAdd = new TimeOffToAdd();
                    licenseToAdd.Origin = TimeOffCreationConsts.Origin;
                    licenseToAdd.StartDate = DateTimeHelper.parseToGVFormat(DateTimeHelper.parseFromBUKFormat(item.Item2.start_date));
                    licenseToAdd.EndDate = DateTimeHelper.parseToGVFormat(DateTimeHelper.parseFromBUKFormat(item.Item2.end_date, true));
                    licenseToAdd.TimeOffTypeId = item.Item1;
                    licenseToAdd.UserIdentifier = employee.Identifier;
                    licenseToAdd.CreatedByIdentifier = TimeOffCreationConsts.CreatedByIdentifier;
                    licenseToAdd.Description = item.Item2.type;
                    timeOffsToAdd.Add(licenseToAdd);
                }


            }
            return timeOffsToAdd;
        }

        protected override List<TimeOffToAdd> buildVacations(List<Vacation> vacations, List<User> users)
        {
            List<TimeOffToAdd> timeOffsToAdd = new List<TimeOffToAdd>();
            foreach (Vacation vacation in vacations)
            {
                User employee = users.FirstOrDefault(u => u.integrationCode.Split(',').Contains(vacation.employee_id.ToString()));
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

        protected override List<(string, Absence)> processLicences(List<Licence> licencias, List<TimeOff> timeOffs, List<User> users, List<AbsenceType> subTypes, List<TimeOffType> gvTypes, CompanyConfiguration companyConfiguration, SesionVM Empresa, List<Employee> employees)
        {
            List<(string, Absence)> licencesToUpsert = new List<(string, Absence)>();
            foreach (var item in licencias)
            {
                User usuario = users.FirstOrDefault(u => u.integrationCode.Split(',').Contains(item.employee_id.ToString()));
                if (usuario != null)
                {
                    string gvTypoId = "";
                    Employee employee = employees.FirstOrDefault(x => x.person_id == item.employee_id);
                    if (matchType(item, subTypes, gvTypes, employee, out gvTypoId))
                    {
                        TimeOff match = match = timeOffs.FirstOrDefault(t => matchCase(t, item, gvTypoId, usuario));

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
        protected override List<(string, Absence)> processPermissions(List<Permission> sinGoce, List<TimeOff> timeOffs, List<User> users, List<AbsenceType> subTypes, List<TimeOffType> gvTypes, CompanyConfiguration companyConfiguration, SesionVM Empresa, List<Employee> employees)
        {
            List<(string, Absence)> leavesToUpsert = new List<(string, Absence)>();
            foreach (var item in sinGoce)
            {
                User usuario = users.FirstOrDefault(u => u.integrationCode.Split(',').Contains(item.employee_id.ToString()));
                if (usuario != null)
                {
                    string gvTypoId = "";
                    Employee employee = employees.FirstOrDefault(x => x.person_id == item.employee_id);
                    if (matchType(item, subTypes, gvTypes, employee, out gvTypoId))
                    {
                        TimeOff match = timeOffs.FirstOrDefault(t => matchCase(t, item, gvTypoId, usuario));
                        if (match == null)
                        {
                            leavesToUpsert.Add((gvTypoId, item));
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
        protected override List<Vacation> processVacations(List<Vacation> vacations, List<TimeOff> timeOffs, List<User> users)
        {
            List<Vacation> vacationsToUpsert = new List<Vacation>();
            foreach (var item in vacations)
            {
                User usuario = users.FirstOrDefault(u => u.integrationCode.Split(',').Contains(item.employee_id.ToString()));
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
        protected override List<(string, Absence)> processSuspensions(List<Suspension> suspensions, List<TimeOff> timeOffs, List<User> users, List<AbsenceType> subTypes, List<TimeOffType> gvTypes, CompanyConfiguration companyConfiguration, SesionVM Empresa)
        {
            List<(string, Absence)> suspensionsToUpsert = new List<(string, Absence)>();
            foreach (var item in suspensions)
            {
                User usuario = users.FirstOrDefault(u => u.integrationCode.Split(',').Contains(item.employee_id.ToString()));
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
    }
}
