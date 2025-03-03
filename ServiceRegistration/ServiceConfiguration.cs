using BusinessLogic.Interfaces;
using BusinessLogic.Implementation;
using Microsoft.Extensions.DependencyInjection;
using System;
using API.BUK.IDAO;
using API.BUK.DAO;
using API.GV.IDAO;
using API.GV.DAO;

namespace ServiceRegistration
{
    public static class ServiceConfiguration
    {
        public static void ConfigureServices(IServiceCollection services)
        {
            services.Add(ServiceDescriptor.Transient<IBUKDAO, BUKDAO>());
            services.Add(ServiceDescriptor.Transient<IProcessPeriodsDAO, ProcessPeriodsDAO>());
            services.Add(ServiceDescriptor.Transient<IProcessPeriodsReader, ProcessPeriodsReader>());

            #region Usuarios
            //Business
            services.Add(ServiceDescriptor.Transient<IUserSync, UserSync>());
            services.Add(ServiceDescriptor.Transient<IUserProcess, UserProcess>());
            services.Add(ServiceDescriptor.Transient<IUserReader, UserReader>());
            services.Add(ServiceDescriptor.Transient<IUserWriter, UserWriter>());
            services.Add(ServiceDescriptor.Transient<IEmployeeReader, EmployeeReader>());
            //DAO
            services.Add(ServiceDescriptor.Transient<IUserDAO, UserDAO>());
            services.Add(ServiceDescriptor.Transient<IEmployeeDAO, EmployeeDAO>());
            #endregion

            #region Permisos
            //Business
            services.Add(ServiceDescriptor.Transient<ITimeOffSync, TimeOffSync>());
            services.Add(ServiceDescriptor.Transient<ITimeOffProcess, TimeOffProcess>());
            services.Add(ServiceDescriptor.Transient<ITimeOffReader, TimeOffReader>());
            services.Add(ServiceDescriptor.Transient<ITimeOffWriter, TimeOffWriter>());
            services.Add(ServiceDescriptor.Transient<IAbsenceReader, AbsenceReader>());
            services.Add(ServiceDescriptor.Transient<IAbsenceWriter, AbsenceWriter>());
            services.Add(ServiceDescriptor.Transient<ILicenceReader, LicenceReader>());
            services.Add(ServiceDescriptor.Transient<IPermissionReader, PermissionReader>());
            services.Add(ServiceDescriptor.Transient<IVacationReader, VacationReader>());
            services.Add(ServiceDescriptor.Transient<ISuspensionReader, SuspensionReader>());
            //DAO
            services.Add(ServiceDescriptor.Transient<ITimeOffDAO, TimeOffDAO>());
            services.Add(ServiceDescriptor.Transient<IAbsenceDAO, AbsenceDAO>());
            services.Add(ServiceDescriptor.Transient<ILicenceDAO, LicenceDAO>());
            services.Add(ServiceDescriptor.Transient<IPermissionDAO, PermissionDAO>());
            services.Add(ServiceDescriptor.Transient<IVacationDAO, VacationDAO>());
            services.Add(ServiceDescriptor.Transient<ISuspensionDAO, SuspensionDAO>());
            #endregion

            #region Asistencia
            //Business
            services.Add(ServiceDescriptor.Transient<IAttendanceSync, AttendanceSync>());
            services.Add(ServiceDescriptor.Transient<ICommonAttendanceProcess, CommonAttendanceProcess>());
            services.Add(ServiceDescriptor.Transient<ICommonAttendanceReader, CommonAttendanceReader>());
            services.Add(ServiceDescriptor.Transient<ICommonAttendanceWriter, CommonAttendanceWriter>());
            services.Add(ServiceDescriptor.Transient<IColombiaAttendanceProcess, ColombiaAttendanceProcess>());
            services.Add(ServiceDescriptor.Transient<IColombiaAttendanceReader, ColombiaAttendanceReader>());
            services.Add(ServiceDescriptor.Transient<IColombiaAttendanceWriter, ColombiaAttendanceWriter>());
            services.Add(ServiceDescriptor.Transient<IUserStatusLogReader, UserStatusLogReader>());
            services.Add(ServiceDescriptor.Transient<IUserStatusLogProcess, UserStatusLogProcess>());
            services.Add(ServiceDescriptor.Transient<IOvertimeReader, OvertimeReader>());
            services.Add(ServiceDescriptor.Transient<IOvertimeWriter, OvertimeWriter>());
            services.Add(ServiceDescriptor.Transient<INonWorkedHoursWriter, NonWorkedHoursWriter>());
            //DAO
            services.Add(ServiceDescriptor.Transient<IAttendanceDAO, AttendanceDAO>());
            services.Add(ServiceDescriptor.Transient<IUserStatusLogDAO, UserStatusLogDAO>());
            services.Add(ServiceDescriptor.Transient<INonWorkedHoursDAO, NonWorkedHoursDAO>());
            services.Add(ServiceDescriptor.Transient<IOvertimeDAO, OvertimeDAO>());
            #endregion
        }
    }
}
