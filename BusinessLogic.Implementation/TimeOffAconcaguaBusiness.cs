using API.BUK.DTO;
using API.BUK.DTO.Consts;
using API.GV.DTO;
using API.GV.DTO.Filters;
using API.Helpers.Commons;
using API.Helpers.VM;
using API.Helpers.VM.Consts;
using BusinessLogic.Implementation.Paises.Colombia;
using BusinessLogic.Interfaces;
using BusinessLogic.Interfaces.VM;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessLogic.Implementation
{
    public class TimeOffAconcaguaBusiness : TimeOffBusiness, ITimeOffBusiness
    {
        /// <summary>
        /// Override sync method for aconcagua company
        /// </summary>
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
            FileLogHelper.log(LogConstants.general, LogConstants.get, "", "PIDIENDO TIPO PERMISOS A BUK", null, Empresa);
            List<PermissionType> permissionTypesBuk = companyConfiguration.PermissionBusiness.GetPermissionTypes(Empresa, companyConfiguration);

            FileLogHelper.log(LogConstants.general, LogConstants.get, "", "PIDIENDO LICENCIAS A BUK", null, Empresa);
            List<Licence> licences = companyConfiguration.LicenceBusiness.GetLicences(startDate, endDate, Empresa, companyConfiguration);

            FileLogHelper.log(LogConstants.general, LogConstants.get, "", "PIDIENDO PERMISOS A BUK", null, Empresa);
            List<Permission> permissions = companyConfiguration.PermissionBusiness.GetPermissions(startDate, endDate, Empresa, companyConfiguration);

            FileLogHelper.log(LogConstants.general, LogConstants.get, "", "PIDIENDO VACACIONES A BUK", null, Empresa);
            List<Vacation> vacations = companyConfiguration.VacationBusiness.GetVacations(startDate, endDate, Empresa, companyConfiguration);

            FileLogHelper.log(LogConstants.general, LogConstants.get, "", "PIDIENDO SUSPENSIONES A BUK", null, Empresa);
            List<Suspension> suspensions = companyConfiguration.SuspensionBusiness.GetSuspensionsBUK(startDate, endDate, Empresa, companyConfiguration);

            licences = FilterLicencesStatus(licences, Empresa);
            permissions = FilterPermissionsStatus(permissions, Empresa);
            vacations = FilterVacationsStatus(vacations, Empresa);
            suspensions = FilterSuspensionsStatus(suspensions, Empresa);
            permissions = companyConfiguration.PermissionBusiness.GetPermissionWithMatch(permissions, permissionTypesBuk, Empresa, companyConfiguration);

            properties["GetSUSPENSIONS"] = suspensions.Count.ToString();
            properties["GetVacations"] = vacations.Count.ToString();
            properties["GetPERMISSIONS"] = permissions.Count.ToString();
            properties["GetLICENCES"] = licences.Count.ToString();
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
        /// Returns only "Approved" licences
        /// </summary>
        public List<Licence> FilterLicencesStatus(List<Licence> licences, SesionVM Empresa)
        {
            List<Licence> rejectedItems = licences.FindAll(l => l.status == BukStatusConsts.REJECTED);
            Parallel.ForEach(rejectedItems, rl => { FileLogHelper.log(LogConstants.timeOff, LogConstants.error_add, rl.employee_id.ToString(), "(Licence) - Cant process due it's rejected: " + rl.Stringify(), null, Empresa); });

            licences = licences.FindAll(l => l.status == BukStatusConsts.APPROVED);

            return licences;
        }

        /// <summary>
        /// Returns only "Approved" permissions
        /// </summary>
        public List<Permission> FilterPermissionsStatus(List<Permission> permissions, SesionVM Empresa)
        {
            List<Permission> rejectedItems = permissions.FindAll(p => p.status == BukStatusConsts.REJECTED);
            Parallel.ForEach(rejectedItems, rl => { FileLogHelper.log(LogConstants.timeOff, LogConstants.error_add, rl.employee_id.ToString(), "(TimeOff) - Cant process due it's rejected: " + rl.Stringify(), null, Empresa); });

            permissions = permissions.FindAll(p => p.status == BukStatusConsts.APPROVED);

            return permissions;
        }

        /// <summary>
        /// Returns only "Approved" suspensions
        /// </summary>
        public List<Suspension> FilterSuspensionsStatus(List<Suspension> suspensions, SesionVM Empresa)
        {
            List<Suspension> rejectedItems = suspensions.FindAll(v => v.status == BukStatusConsts.REJECTED);
            Parallel.ForEach(rejectedItems, rl => { FileLogHelper.log(LogConstants.timeOff, LogConstants.error_add, rl.employee_id.ToString(), "(Suspension) - Cant process due it's rejected: " + rl.Stringify(), null, Empresa); });
            suspensions = suspensions.FindAll(s => s.status == BukStatusConsts.APPROVED);

            return suspensions;
        }

        /// <summary>
        /// Returns only "Approved" vacations
        /// </summary>
        public List<Vacation> FilterVacationsStatus(List<Vacation> vacations, SesionVM Empresa)
        {
            List<Vacation> rejectedItems = vacations.FindAll(v => v.status == BukStatusConsts.REJECTED);
            Parallel.ForEach(rejectedItems, rl => { FileLogHelper.log(LogConstants.timeOff, LogConstants.error_add, rl.employee_id.ToString(), "(Vacation) - Cant process due it's rejected: " + rl.Stringify(), null, Empresa); });
            vacations = vacations.FindAll(v => v.status == BukStatusConsts.APPROVED);

            return vacations;
        }
    }
}
