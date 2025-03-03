using API.BUK.DTO;
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
    public class AttendanceMultiUrlsColombiaBusiness : AttendanceColombiaBusiness, IAttendanceBusiness
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
            FileLogHelper.log(LogConstants.general, LogConstants.get, "", "EMPIEZA SYNCATTENDANCE-MULTIURL", null, Empresa);
            List<DateTime> fechasOrdenadas = fechas.ToList();
            DateTime startDate = fechasOrdenadas[0];
            DateTime endDate = fechasOrdenadas.Last();
            #endregion

            #region Usuarios
            FileLogHelper.log(LogConstants.general, LogConstants.get, "", "PIDIENDO EMPLEADOS A BUK", null, Empresa);
            List<Employee> employeesURL1 = companyConfiguration.EmployeeBusiness.GetEmployeesForSync(Empresa, companyConfiguration, startDate, endDate);
            List<Employee> employeesURL2 = companyConfiguration.EmployeeBusiness.GetEmployeesForSync(new SesionVM { Empresa = Empresa.Empresa, Url = Empresa.Url2, BukKey = Empresa.BukKey2 }, companyConfiguration, startDate, endDate);
            List<Employee> employees = new List<Employee>();
            
            employees.AddRange(employeesURL1);
            employees.AddRange(employeesURL2);

            FileLogHelper.log(LogConstants.general, LogConstants.get, "", "PIDIENDO USUARIOS A GV", null, Empresa);
            var persons = companyConfiguration.UserBusiness.GetUsersForSync(Empresa, companyConfiguration, employees, Operacion.ASISTENCIA);
            List<User> users = persons.Item1;
            employees = persons.Item2;
            List<User> usersTemporales = users.FindAll(u => u.Custom1 != null && u.Custom1.ToLower() == UsersMultiUrlConts.Temporales);
            List<User> usersDirectos = users.FindAll(u => u.Custom1 == null ||(u.Custom1 != null && u.Custom1.ToLower() != UsersMultiUrlConts.Temporales));
            properties["GetUsersUpdated"] = users.Count.ToString();
            #endregion

            properties["EMPRESA"] = Empresa.Empresa;
            properties["InasistenciasStartDate"] = DateTimeHelper.parseToGVFormat(fechas.InasistenciasStartDate);
            properties["InasistenciasEndDate"] = DateTimeHelper.parseToGVFormat(fechas.InasistenciasEndDate);
            properties["HorasExtrasStartDate"] = DateTimeHelper.parseToGVFormat(fechas.HorasExtrasStartDate);
            properties["HorasExtrasEndDate"] = DateTimeHelper.parseToGVFormat(fechas.HorasExtrasEndDate);
            properties["HorasNoTrabajadasStartDate"] = DateTimeHelper.parseToGVFormat(fechas.HorasNoTrabajadasStartDate);
            properties["HorasNoTrabajadasEndDate"] = DateTimeHelper.parseToGVFormat(fechas.HorasNoTrabajadasEndDate);
            properties["users"] = users.Count.ToString();

            FileLogHelper.log(LogConstants.general, LogConstants.get, "", "PIDIENDO LIBRO DE ASISTENCIA A GV", null, Empresa);
            AttendanceColombia asistencia = (AttendanceColombia)GetAttendance(Empresa, users, startDate, endDate, companyConfiguration);

            properties["GetAttendance"] = (users.Count * (endDate - startDate).Days).ToString();
            List<Variables> processedDirectos = new List<Variables>();
            FileLogHelper.log(LogConstants.general, LogConstants.get, "", "PIDIENDO ESTADO DE ACTIVACION DE USUARIOS A GV", null, Empresa);
            var userStatusLogs = companyConfiguration.UserStatusLogBusiness.GetUserStatusLogs(users, Empresa, companyConfiguration);
            userStatusLogs = reCalculateActivePeriodStart(userStatusLogs, employees);

            FileLogHelper.log(LogConstants.general, LogConstants.get, "", "PROCESANDO OPERACIONES A REALIZAR CON LIBRO DE ASISTENCIA", null, Empresa);
            processedDirectos = this.processData(asistencia, usersDirectos, fechas, periodo, userStatusLogs, employeesURL1);

            FileLogHelper.log(LogConstants.general, LogConstants.get, "", "ENVIANDO OPERACIONES A BUK", null, Empresa);
            Dictionary<string, string> result = SendData(processedDirectos, Empresa, companyConfiguration);
            List<Variables>  processedTemporales = this.processData(asistencia, usersTemporales, fechas, periodo, userStatusLogs, employeesURL2);

            FileLogHelper.log(LogConstants.general, LogConstants.get, "", "ENVIANDO OPERACIONES A BUK URL 2", null, Empresa);
            Dictionary<string, string> resultTemporales = SendData(processedTemporales, new SesionVM { Empresa = Empresa.Empresa, Url = Empresa.Url2, BukKey = Empresa.BukKey2 }, companyConfiguration);
            
            foreach (KeyValuePair<string, string> res in result)
            {
                properties[res.Key] = res.Value;
            }

            foreach (KeyValuePair<string, string> res in resultTemporales)
            {
                properties[res.Key] = res.Value;
            }

            InsightHelper.logMetric("SyncAttendance", DateTime.Now - startMetric, properties);
        }
    }
}