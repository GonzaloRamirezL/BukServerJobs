using API.BUK.DTO;
using API.BUK.DTO.Consts;
using API.GV.DTO;
using API.GV.DTO.Filters;
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
    public class TimeOffBusinessCentralServicing : TimeOffBusiness, ITimeOffBusinessCentralServicing
    {
        private const string BUK_PARTIAL_PERMISSION = "0.5";
        private const string BUK_TOTAL_PERMISSION = "1";
        private const string UNION_TIME_BUK_ITEM_CODE = "descuento_permiso_sindical";
        private const string UNION_TIME_GV_TIMEOFF_CODE = "HORAS SINDICALES";

        public override void Sync(SesionVM session, ProcessPeriod periodo, List<PeriodConfiguration> configs, CompanyConfiguration companyConfiguration)
        {
            DateTime startMetric = DateTime.Now;
            Dictionary<string, string> properties = new Dictionary<string, string>();

            FileLogHelper.log(LogConstants.period, LogConstants.get, "", string.Empty, periodo, session);
            Console.WriteLine("PROCESANDO PERIODO: " + periodo.month);

            #region Fechas
            DateTime fechaBase = DateTimeHelper.parseFromBUKFormat(periodo.month);
            FechasProcesamientoVM fechas = DateTimeHelper.calculateProcessDate(configs, fechaBase, session);
            FileLogHelper.log(LogConstants.cutOffDate, LogConstants.get, "", "", fechas, session);
            DateTime startDate = fechas.PermisosStartDate;
            DateTime endDate = fechas.PermisosEndDate;
            FileLogHelper.log(LogConstants.general, LogConstants.get, "", "EMPIEZA SYNCTIMEOFF", null, session);
            properties["EMPRESA"] = session.Empresa;
            properties["startDate"] = DateTimeHelper.parseToGVFormat(startDate);
            properties["endDate"] = DateTimeHelper.parseToGVFormat(endDate);
            #endregion

            #region Usuarios y empleados
            BaseLog(session, "PIDIENDO EMPLEADOS A BUK");
            List<Employee> employees = companyConfiguration.EmployeeBusiness.GetEmployeesForSync(session, companyConfiguration, startDate, endDate);

            BaseLog(session, "PIDIENDO USUARIOS A GV");
            var persons = companyConfiguration.UserBusiness.GetUsersForSync(session, companyConfiguration, employees, Operacion.PERMISOS);
            List<User> gvUsers = persons.Item1;
            employees = persons.Item2;
            properties["users"] = gvUsers.Count.ToString();
            #endregion

            #region PermisosGV
            DateTime startDateGV = startDate;
            DateTime endDateGV = endDate;

            BaseLog(session, "PIDIENDO PERMISOS A GV");
            List<TimeOff> timeOffs = GetTimeOffs(gvUsers, startDateGV, endDateGV, session, companyConfiguration);
            List<TimeOffType> gvTypes = GetAllTypes(session, companyConfiguration);
            #endregion

            #region Consulta y eliminación de datos en Buk
            BaseLog(session, "PIDIENDO PERMISOS A BUK");
            List<PermissionType> bukPermissionTypes = companyConfiguration.PermissionBusiness.GetPermissionTypes(session, companyConfiguration);
            List<Permission> bukPermissions = companyConfiguration.PermissionBusiness.GetPermissions(startDate, endDate, session, companyConfiguration);
            bukPermissions = companyConfiguration.PermissionBusiness.GetPermissionWithMatch(bukPermissions, bukPermissionTypes, session, companyConfiguration);
            properties["GetPERMISSIONS"] = bukPermissions.Count.ToString();
            List<int> userIdsToDelete = bukPermissions.Select(p => p.employee_id).Distinct().ToList();
            companyConfiguration.PermissionBusiness.DeletePermissions(session, companyConfiguration, userIdsToDelete, fechas);

            BaseLog(session, "PIDIENDO LICENCIAS A BUK");
            List<Licence> bukLicenses = companyConfiguration.LicenceBusiness.GetLicences(startDate, endDate, session, companyConfiguration);
            properties["GetLICENCES"] = bukLicenses.Count.ToString();

            BaseLog(session, "PIDIENDO VACACIONES A BUK");
            List<Vacation> bukVacations = companyConfiguration.VacationBusiness.GetVacations(startDate, endDate, session, companyConfiguration);
            properties["GetVacations"] = bukVacations.Count.ToString();
            #endregion

            #region Envío de datos hacia Buk
            BaseLog(session, "ENVIANDO PERMISOS A BUK");
            Attendance attendance = companyConfiguration.AttendanceBusiness.GetAttendance(session, gvUsers, startDate, endDate, companyConfiguration);
            List<TimeOff> preFilteredTimeoffs = FilterTimeOffs(gvUsers, timeOffs, bukLicenses, bukVacations);
            var procesedPermissions =  ProcessTimeOffs(gvUsers, preFilteredTimeoffs, gvTypes, bukPermissionTypes, attendance, session, companyConfiguration);
            companyConfiguration.PermissionBusiness.SendPermissions(session, companyConfiguration, procesedPermissions.Item1);

            BaseLog(session, "ENVIANDO HORAS SINDICALES A BUK");
            DeleteAssings(companyConfiguration, session, procesedPermissions.Item2);
            companyConfiguration.ItemBusiness.AssignItems(companyConfiguration, session, procesedPermissions.Item2);
            #endregion

            #region Alta de datos en Geovictoria
            BaseLog(session, "AGREGANDO LICENCIAS A GEOVICTORIA");
            List<AbsenceType> subTypes = new List<AbsenceType>();
            subTypes.AddRange(companyConfiguration.AbsenceBusiness.GetSubTypes(session, BUKMacroAbsenceTypes.Licencia, companyConfiguration));
            List<(string, Absence)> licensesToUpsert = processLicences(bukLicenses, timeOffs, gvUsers, subTypes, gvTypes, companyConfiguration, session, employees);
            AddTimeOffs(buildTimeOffsToAdd(licensesToUpsert, gvUsers), session, companyConfiguration);

            BaseLog(session, "AGREGANDO VACACIONES A GEOVICTORIA");
            List<Vacation> processedVacations = processVacations(bukVacations, timeOffs, gvUsers);
            List<TimeOffToAdd> vacationsToUpsert = buildVacations(processedVacations, gvUsers);
            AddTimeOffs(vacationsToUpsert, session, companyConfiguration);
            #endregion

            InsightHelper.logMetric("SyncTimeOffs", DateTime.Now - startMetric, properties);
        }

        private void DeleteAssings(CompanyConfiguration companyConfiguration, SesionVM session, List<ItemToAssign> itemsToAssign)
        {
            foreach (ItemToAssign item in itemsToAssign)
            {
                List<AssignedItem> assigns = companyConfiguration.ItemBusiness.GetAssignsForUser(companyConfiguration, session, item.employee_id, item.start_date);
                AssignedItem assignedItem = assigns.FirstOrDefault(a => a.item.id == item.item_id);
                if (assignedItem != null)
                {
                    companyConfiguration.ItemBusiness.DeleteAssign(companyConfiguration,session, assignedItem.id);
                }
            }
        }

        private List<TimeOff> FilterTimeOffs(List<User> gvUsers, List<TimeOff> timeOffs, List<Licence> bukLicenses, List<Vacation> bukVacations)
        {
            List<TimeOff> filteredItems = new List<TimeOff>();
            DateTime startDate;
            User gvUser;
            bool hasLicense;
            bool hasVacation;

            foreach (TimeOff timeOff in timeOffs)
            {
                gvUser = gvUsers.FirstOrDefault(x => x.Identifier == timeOff.UserIdentifier);
                startDate = DateTimeHelper.parseFromGVFormat(timeOff.Starts);

                hasLicense = bukLicenses.Any(l => startDate >= DateTimeHelper.parseFromBUKFormat(l.start_date)
                       && startDate < DateTimeHelper.parseFromBUKFormat(l.end_date) && l.employee_id.ToString() == gvUser.integrationCode);
                            
                hasVacation = bukVacations.Any(v => startDate >= DateTimeHelper.parseFromBUKFormat(v.start_date)
                    && startDate < DateTimeHelper.parseFromBUKFormat(v.end_date) && v.employee_id.ToString() == gvUser.integrationCode);

                if (!hasLicense && !hasVacation )
                {
                    filteredItems.Add(timeOff);
                }
            }
            return filteredItems;
        }

        private (List<PermissionToAdd>, List<ItemToAssign>) ProcessTimeOffs(List<User> users, List<TimeOff> timeOffs, List<TimeOffType> gvTypes, List<PermissionType> bukPermissionTypes,
            Attendance attendance, SesionVM sesionActiva, CompanyConfiguration companyConfiguration)
        {
            List<PermissionToAdd> permissionsToAdd = new List<PermissionToAdd>();
            List<ItemToAssign> unionHoursToAssign =  new List<ItemToAssign>(); 
            TimeOffType gvTimeOffType;
            PermissionType bukPermissionType;
            User gvUser;
            bool isPayable;
            double unionHoursTotalDelay = 0;

            Item unionHoursBukItem = companyConfiguration.ItemBusiness.GetItem(companyConfiguration, sesionActiva, UNION_TIME_BUK_ITEM_CODE);

            foreach (TimeOff timeOff in timeOffs)
            {
                gvUser = users.FirstOrDefault(x => x.Identifier == timeOff.UserIdentifier);
                gvTimeOffType = gvTypes.FirstOrDefault(t => t.Id == timeOff.TimeOffTypeId);
                string timeOffNameDesc = gvTimeOffType.Description.ToUpper();
                if (timeOffNameDesc.Equals(UNION_TIME_GV_TIMEOFF_CODE,StringComparison.CurrentCultureIgnoreCase))
                {
                    List<TimeInterval> userIntervals = attendance.Users.FirstOrDefault(u => u.Identifier == gvUser.Identifier).PlannedInterval;
                    unionHoursTotalDelay = GetTotalDelays(userIntervals, timeOff);
                    int userItemIndex = unionHoursToAssign.FindIndex(i => i.employee_id.ToString().Equals(gvUser.integrationCode));
                   
                    if (userItemIndex < 0)
                    {
                        ItemToAssign itemToAssign = GetNewItemToAssign(gvUser, timeOff, gvTimeOffType, unionHoursBukItem);
                        itemToAssign.amount = unionHoursTotalDelay;
                        unionHoursToAssign.Add(itemToAssign);
                    }
                    else
                    {
                        unionHoursToAssign[userItemIndex].amount += unionHoursTotalDelay;
                    }
                }
                else if(!timeOffNameDesc.Contains("LICENCIA") && !timeOffNameDesc.Contains("VACACION"))
                {
                    isPayable = Convert.ToBoolean(gvTimeOffType.IsPayable);
                    bukPermissionType = bukPermissionTypes.FirstOrDefault(p => p.with_pay == isPayable);
                    permissionsToAdd.Add(ParsePermissionToBukFormat(timeOff, gvTimeOffType, bukPermissionType, gvUser));
                }
            }

            return (permissionsToAdd, unionHoursToAssign);
        }

        private double GetTotalDelays(List<TimeInterval> plannedIntervals, TimeOff timeOff)
        {
            double totalHours = 0;
            TimeInterval interval = plannedIntervals.FirstOrDefault(i => i.Date.Equals(timeOff.Starts));
            totalHours += TimeSpanHelper.HHmmToTimeSpan(interval.Delay).TotalHours;
            totalHours += TimeSpanHelper.HHmmToTimeSpan(interval.EarlyLeave).TotalHours;
            return totalHours;
        }

        private ItemToAssign GetNewItemToAssign(User gvUser, TimeOff timeOff, TimeOffType gvTimeOffType, Item unionHoursBukItem)
        {
            DateTime startDate = DateTimeHelper.parseFromGVFormat(timeOff.Starts);
            startDate = new DateTime(startDate.Year, startDate.Month, 1);
            return new ItemToAssign
            {
                employee_id = Int32.Parse(gvUser.integrationCode),
                item_id = unionHoursBukItem.id,
                start_date = DateTimeHelper.parseToBUKFormat(startDate),
                description = gvTimeOffType.Description,
            };
        }
        
        private PermissionToAdd ParsePermissionToBukFormat(TimeOff timeOff, TimeOffType timeOffType, PermissionType bukPermissionType, User gvUser)
        {
            bool isParcial = Convert.ToBoolean(timeOffType.IsParcial);

            return new PermissionToAdd
            {
                application_date = ParseDateToBUKFormat(timeOff.Starts),
                employee_id = Int32.Parse(gvUser.integrationCode),
                start_date = ParseDateToBUKFormat(timeOff.Starts),
                end_date = ParseDateToBUKFormat(timeOff.Ends),
                days_count = GetTotalDays(timeOff.Starts, timeOff.Ends),
                day_percent = isParcial ? BUK_PARTIAL_PERMISSION : BUK_TOTAL_PERMISSION,
                paid = bukPermissionType.with_pay,
                permission_type_id = bukPermissionType.id,
                justification = timeOff.Description,
            };
        }

        private List<LicenceToSend> GetLicencesToSend(List<User> gvUsers, List<TimeOff> timeOffs, List<TimeOffType> gvTypes, List<Licence> bukLicenses, List<LicenceType> bukLicenseTypes)
        {
            List<LicenceToSend> licensesToSend = new List<LicenceToSend>();
            TimeOffType gvTimeOffType;
            User gvUser;

            List<string> gvTypesIds = gvTypes.Where(gvT => gvT.Description.ToUpper().Contains("LICENCIA")).Select(gvT => gvT.Id).ToList();
            timeOffs = timeOffs.Where(t => gvTypesIds.Contains(t.TimeOffTypeId)).ToList();
            LicenceType bukLicenseType = bukLicenseTypes.FirstOrDefault(t => t.description.Equals(StandardTypes.BukLicencia));

            foreach (TimeOff timeOff in timeOffs)
            {
                gvTimeOffType = gvTypes.FirstOrDefault(t => t.Id == timeOff.TimeOffTypeId);
                gvUser = gvUsers.FirstOrDefault(x => x.Identifier == timeOff.UserIdentifier);
                LicenceToSend licenseToAdd = ParseLicenseToBukFormat(timeOff, gvTimeOffType, gvUser, bukLicenseType);
                licensesToSend.Add(licenseToAdd);
            }
            return licensesToSend;
        }

        private LicenceToSend ParseLicenseToBukFormat(TimeOff timeOff, TimeOffType gvTimeOffType, User gvUser, LicenceType bukLicenseType)
        {
            bool isParcial = Convert.ToBoolean(gvTimeOffType.IsParcial);

            return new LicenceToSend
            {
                licence_type_id = bukLicenseType.id,
                contribution_days = GetTotalDays(timeOff.Starts, timeOff.Ends), 
                format = "fisica",
                @type = "accidente_comun", 
                start_date = ParseDateToBUKFormat(timeOff.Starts),
                days_count = GetTotalDays(timeOff.Starts, timeOff.Ends),
                day_percent = isParcial ? BUK_PARTIAL_PERMISSION : BUK_TOTAL_PERMISSION,
                application_date = ParseDateToBUKFormat(timeOff.Starts),
                justification = timeOff.Description,
                employee_id = Int32.Parse(gvUser.integrationCode),        
            };
        }
        
        private void BaseLog(SesionVM session, string message)
        {
            FileLogHelper.log(LogConstants.general, LogConstants.get, "", message, null, session);
        }

        private int GetTotalDays(string start, string end)
        {
            DateTime startDate = DateTimeHelper.parseFromGVFormat(start);
            DateTime endDate = DateTimeHelper.parseFromGVFormat(end);
            int diff = (int)(endDate - startDate).TotalDays;
            return diff == 0 ? 1 : diff;
        }

        private string ParseDateToBUKFormat(string date)
        {
            DateTime dateToFormat = DateTimeHelper.parseFromGVFormat(date);
            return DateTimeHelper.parseToBUKFormat(dateToFormat);
        }
    }
}
