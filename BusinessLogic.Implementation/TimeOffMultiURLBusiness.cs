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

namespace BusinessLogic.Implementation
{
    public class TimeOffMultiURLBusiness: TimeOffColombiaBusiness, ITimeOffBusiness
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
            FileLogHelper.log(LogConstants.general, LogConstants.get, "", "EMPIEZA SYNCTIMEOFF-MULTIURL", null, Empresa);
            properties["EMPRESA"] = Empresa.Empresa;
            properties["startDate"] = DateTimeHelper.parseToGVFormat(startDate);
            properties["endDate"] = DateTimeHelper.parseToGVFormat(endDate);
            #endregion

            #region Usuarios
            FileLogHelper.log(LogConstants.general, LogConstants.get, "", "PIDIENDO EMPLEADOS A BUK", null, Empresa);
            List<Employee> employeesURL1 = companyConfiguration.EmployeeBusiness.GetEmployeesForSync(Empresa, companyConfiguration, startDate, endDate);
            List<Employee> employeesURL2 = companyConfiguration.EmployeeBusiness.GetEmployeesForSync(new SesionVM { Empresa = Empresa.Empresa, Url = Empresa.Url2, BukKey = Empresa.BukKey2 }, companyConfiguration, startDate, endDate);
            List<Employee> employees = new List<Employee>();
            employees.AddRange(employeesURL1);
            employees.AddRange(employeesURL2);

            FileLogHelper.log(LogConstants.general, LogConstants.get, "", "PIDIENDO USUARIOS A GV", null, Empresa);
            var persons = companyConfiguration.UserBusiness.GetUsersForSync(Empresa, companyConfiguration, employees, Operacion.PERMISOS);
            List<User> users = persons.Item1;
            employees = persons.Item2;
            properties["users"] = users.Count.ToString();
            #endregion

            #region PermisosBUK
            FileLogHelper.log(LogConstants.general, LogConstants.get, "", "PIDIENDO LICENCIAS A BUK", null, Empresa);
            List<Licence> licences = companyConfiguration.LicenceBusiness.GetLicences(startDate, endDate, Empresa, companyConfiguration);
            licences.AddRange(companyConfiguration.LicenceBusiness.GetLicences(startDate, endDate, new SesionVM { Empresa = Empresa.Empresa, Url = Empresa.Url2, BukKey = Empresa.BukKey2 }, companyConfiguration));
            properties["GetLICENCES"] = licences.Count.ToString();


            FileLogHelper.log(LogConstants.general, LogConstants.get, "", "PIDIENDO TIPO PERMISOS A BUK", null, Empresa);
            List<PermissionType> permissionTypesBuk = companyConfiguration.PermissionBusiness.GetPermissionTypes(Empresa, companyConfiguration);


            FileLogHelper.log(LogConstants.general, LogConstants.get, "", "PIDIENDO PERMISOS A BUK", null, Empresa);
            List<Permission> permissions = companyConfiguration.PermissionBusiness.GetPermissions(startDate, endDate, Empresa, companyConfiguration);
            permissions.AddRange(companyConfiguration.PermissionBusiness.GetPermissions(startDate, endDate, new SesionVM { Empresa = Empresa.Empresa, Url = Empresa.Url2, BukKey = Empresa.BukKey2 }, companyConfiguration));
            permissions = companyConfiguration.PermissionBusiness.GetPermissionWithMatch(permissions, permissionTypesBuk, Empresa, companyConfiguration);
            properties["GetPERMISSIONS"] = permissions.Count.ToString();


            FileLogHelper.log(LogConstants.general, LogConstants.get, "", "PIDIENDO VACACIONES A BUK", null, Empresa);
            List<Vacation> vacations = companyConfiguration.VacationBusiness.GetVacations(startDate, endDate, Empresa, companyConfiguration);
            vacations.AddRange(companyConfiguration.VacationBusiness.GetVacations(startDate, endDate, new SesionVM { Empresa = Empresa.Empresa, Url = Empresa.Url2, BukKey = Empresa.BukKey2 }, companyConfiguration));
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
            FileLogHelper.log(LogConstants.general, LogConstants.get, "", "PIDIENDO SUBTIPOS DE LICENCIAS", null, Empresa);
            subTypes.AddRange(companyConfiguration.AbsenceBusiness.GetSubTypes(Empresa, BUKMacroAbsenceTypes.Licencia, companyConfiguration));

            FileLogHelper.log(LogConstants.general, LogConstants.get, "", "PIDIENDO SUBTIPOS DE PERMISOS", null, Empresa);
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
    }
}
