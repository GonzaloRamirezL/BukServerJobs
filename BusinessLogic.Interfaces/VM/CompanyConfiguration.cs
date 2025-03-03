using API.BUK.IDAO;
using API.GV.IDAO;

namespace BusinessLogic.Interfaces.VM
{
    public class CompanyConfiguration
    {
        public IBUKDAO BUKDAO;
        public IProcessPeriodsDAO ProcessPeriodsDAO;
        public IProcessPeriodsBusiness ProcessPeriodsBusiness;
        public ICompanyBusiness CompanyBusiness;
        public ICompanyDAO CompanyDAO;

        #region Usuarios
        //Business
        public IUserBusiness UserBusiness;
        public IEmployeeBusiness EmployeeBusiness;
        public IGroupBusiness GroupBusiness;
        //DAO
        public IUserDAO UserDAO;
        public IEmployeeDAO EmployeeDAO;
        public IGroupDAO GroupDAO;
        #endregion

        #region Permisos
        //Business
        public ITimeOffBusiness TimeOffBusiness;
        public IAbsenceBusiness AbsenceBusiness;
        public ILicenceBusiness LicenceBusiness;
        public IPermissionBusiness PermissionBusiness;
        public IVacationBusiness VacationBusiness;
        public ISuspensionBusiness SuspensionBusiness;
        //DAO
        public ITimeOffDAO TimeOffDAO;
        public IAbsenceDAO AbsenceDAO;
        public ILicenceDAO LicenceDAO;
        public IPermissionDAO PermissionDAO;
        public IVacationDAO VacationDAO;
        public ISuspensionDAO SuspensionDAO;
        #endregion

        #region Asistencia
        //Business
        public IAttendanceBusiness AttendanceBusiness;

        public IUserStatusLogBusiness UserStatusLogBusiness;
        public IOvertimeBusiness OvertimeBusiness;
        public INonWorkedHoursBusiness NonWorkedHoursBusiness;
        //DAO
        public IAttendanceDAO AttendanceDAO;
        public IUserStatusLogDAO UserStatusLogDAO;
        public INonWorkedHoursDAO NonWorkedHoursDAO;
        public IOvertimeDAO OvertimeDAO;
        #endregion

        #region KPI
        //Business
        public IkpiBusiness KpiBusiness;
        //DAO
        public IKpiDAO KpiDAO;
        #endregion

        #region Item
        //Business
        public IItemBusiness ItemBusiness;
        //DAO
        public IItemDAO ItemDAO;
        #endregion
    }
}
