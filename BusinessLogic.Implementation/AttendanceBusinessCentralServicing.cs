using API.BUK.DTO;
using API.BUK.DTO.Consts;
using API.GV.DTO;
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
    public class AttendanceBusinessCentralServicing : AttendanceBusiness
    {
        private const string BUK_PARTIAL_PERMISSION = "0.5";
        private const string BUK_TOTAL_PERMISSION = "1";
        private const string EXTRATIME_INITIALS = "HHEE";

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

            FileLogHelper.log(LogConstants.general, LogConstants.get, "", "EMPIEZA SYNCATTENDANCE LIBROASISTENCIA A GV", null, Empresa);
            List<DateTime> fechasOrdenadas = fechas.ToList();
            DateTime startDate = fechasOrdenadas[0];
            DateTime endDate = fechasOrdenadas.Last();
            #endregion

            #region Usuarios
            FileLogHelper.log(LogConstants.general, LogConstants.get, "", "PIDIENDO EMPLEADOS A BUK", null, Empresa);
            List<Employee> employees = companyConfiguration.EmployeeBusiness.GetEmployeesForSync(Empresa, companyConfiguration, startDate, endDate);

            FileLogHelper.log(LogConstants.general, LogConstants.get, "", "PIDIENDO USUARIOS A BUK", null, Empresa);
            var persons = companyConfiguration.UserBusiness.GetUsersForSync(Empresa, companyConfiguration, employees, Operacion.ASISTENCIA);
            List<User> users = persons.Item1;
            employees = persons.Item2;
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
            var asistencia = GetAttendance(Empresa, users, startDate, endDate, companyConfiguration);
            properties["GetAttendance"] = (users.Count * (endDate - startDate).Days).ToString();

            List<Variables> processed = new List<Variables>();

            FileLogHelper.log(LogConstants.general, LogConstants.get, "", "PIDIENDO ESTADO DE ACTIVACION DE USUARIOS A GV", null, Empresa);
            var userStatusLogs = companyConfiguration.UserStatusLogBusiness.GetUserStatusLogs(users, Empresa, companyConfiguration);
            userStatusLogs = reCalculateActivePeriodStart(userStatusLogs, employees);

            FileLogHelper.log(LogConstants.general, LogConstants.get, "", "PROCESANDO OPERACIONES A REALIZAR CON LIBRO DE ASISTENCIA", null, Empresa);
            processed = processData(asistencia, users, fechas, periodo, userStatusLogs, employees);

            FileLogHelper.log(LogConstants.general, LogConstants.get, "", "ENVIANDO OPERACIONES A BUK", null, Empresa);
            Dictionary<string, string> result = SendData(users, processed, Empresa, companyConfiguration);

            foreach (KeyValuePair<string, string> res in result)
            {
                properties[res.Key] = res.Value;
            }

            InsightHelper.logMetric("SyncAttendance", DateTime.Now - startMetric, properties);
        }

        protected  Dictionary<string, string> SendData(List<User> users, List<Variables> variables, SesionVM Empresa, CompanyConfiguration companyConfiguration, bool hasNWHSeparadas = false)
        {
            int totalHNT = 0;
            int totalAusencias = 0;
            int totalHHEE = 0;
            int total = variables.Count;
            FileLogHelper.log(LogConstants.general, LogConstants.get, "", "ENVIANDO OPERACIONES A BUK PARA UN TOTAL DE " + total + " USUARIO(S)", null, Empresa);

            int standardAbsenceId = companyConfiguration.AbsenceBusiness.FindStandardAbsenceId(Empresa, companyConfiguration);
            int current = 0;
            object lockCurrent = new object();
            foreach (var item in variables)
            {
                FileLogHelper.log(LogConstants.general, LogConstants.get, item.rut, "Enviando operaciones para el usuario: " + item.rut, null, Empresa);
                lock (lockCurrent)
                {
                    current++;
                }

                if (Empresa.EnviaHNT)
                {
                    FileLogHelper.log(LogConstants.general, LogConstants.get, item.rut, "Enviando HNT para el usuario: " + item.rut, null, Empresa);
                    sendHNT(Empresa, companyConfiguration, item);
                    totalHNT++;
                }

                totalAusencias += item.ausencias.Count;
                if (Empresa.EnviaAusencia && standardAbsenceId > 0)
                {
                    FileLogHelper.log(LogConstants.general, LogConstants.get, item.rut, "Enviando AUSENCIAS para el usuario: " + item.rut, null, Empresa);
                    sendAbsences(Empresa, companyConfiguration, item, standardAbsenceId);
                }

                if (Empresa.EnviaHHEE)
                {
                    User user = users.FirstOrDefault(u => u.integrationCode == item.identificador_interno_de_ellos.ToString());
                    bool sendExtraTime = string.Equals(user.Custom1, EXTRATIME_INITIALS, StringComparison.CurrentCultureIgnoreCase);
                    if (user != null && sendExtraTime)
                    {
                        FileLogHelper.log(LogConstants.general, LogConstants.get, item.rut, "Enviando HHEE para el usuario: " + item.rut, null, Empresa);
                        totalHHEE += item.overtimes.Count;
                        sendOvertimes(Empresa, companyConfiguration, item);
                    }
                }
            }

            Dictionary<string, string> properties = new Dictionary<string, string>();

            properties["AddNonWorkedHours"] = totalHNT.ToString();
            properties["AddAbsences"] = totalAusencias.ToString();
            properties["AddOvertimes"] = totalHHEE.ToString();
            return properties;
        }

        protected override void sendAbsences(SesionVM Empresa, CompanyConfiguration companyConfiguration, Variables item, int standardAbsenceId)
        {
            FileLogHelper.log(LogConstants.general, LogConstants.get, item.rut, "Eliminando AUSENCIAS para el usuario: " + item.rut + " desde " + DateTimeHelper.parseToBUKFormat(item.ausenciasDesde) + " hasta " + DateTimeHelper.parseToBUKFormat(item.ausenciasHasta), null, Empresa);
            try
            {
                bool succeedElimination = companyConfiguration.AbsenceBusiness.DeleteAbsence(new AbsencesToDelete { employees_id = new List<int> { item.identificador_interno_de_ellos }, start_date = DateTimeHelper.parseToBUKFormat(item.ausenciasDesde), end_date = DateTimeHelper.parseToBUKFormat(item.ausenciasHasta) }, Empresa, companyConfiguration);
                if (succeedElimination)
                {
                    FileLogHelper.log(LogConstants.general, LogConstants.get, item.rut, "Eliminadas AUSENCIAS para el usuario: " + item.rut + " desde " + DateTimeHelper.parseToBUKFormat(item.ausenciasDesde) + " hasta " + DateTimeHelper.parseToBUKFormat(item.ausenciasHasta), null, Empresa);
                }
                else
                {
                    FileLogHelper.log(LogConstants.general, LogConstants.get, item.rut, "No se encontraron AUSENCIAS para el usuario: " + item.rut + " desde " + DateTimeHelper.parseToBUKFormat(item.ausenciasDesde) + " hasta " + DateTimeHelper.parseToBUKFormat(item.ausenciasHasta), null, Empresa);
                }
            }
            catch (Exception e)
            {
                FileLogHelper.log(LogConstants.general, LogConstants.get, item.rut, "Error al eliminar AUSENCIAS para el usuario: " + item.rut + " desde " + DateTimeHelper.parseToBUKFormat(item.ausenciasDesde) + " hasta " + DateTimeHelper.parseToBUKFormat(item.ausenciasHasta) + "ERROR: " + e.Message, null, Empresa);
            }

            FileLogHelper.log(LogConstants.general, LogConstants.get, item.rut, "Enviando AUSENCIAS para el usuario: " + item.rut, null, Empresa);

            List<AbsenceToAdd> absencesToSend = new List<AbsenceToAdd>();
            try
            {
                foreach (AbsenceToAdd ausencia in item.ausencias)
                {
                    int cantAusencias = item.ausencias.Count(a => a.application_date == ausencia.application_date);
                    ausencia.day_percent = cantAusencias > 1 ? BUK_TOTAL_PERMISSION : BUK_PARTIAL_PERMISSION;
                    ausencia.absence_type_id = standardAbsenceId;

                    if (!absencesToSend.Exists(a => (a.application_date == ausencia.application_date) 
                            && (a.day_percent == BUK_TOTAL_PERMISSION) && (a.employee_id == ausencia.employee_id)))
                    {
                        FileLogHelper.log(LogConstants.absences, LogConstants.add, item.rut, string.Empty, ausencia, Empresa);
                        absencesToSend.Add(ausencia);
                    }
                }
    
                companyConfiguration.AbsenceBusiness.AddAbsences(absencesToSend, Empresa, companyConfiguration);
            }
            catch (Exception ex)
            {
                InsightHelper.logException(ex, Empresa.Empresa);
                FileLogHelper.log(LogConstants.absences, LogConstants.error_add, item.rut, "ERROR AL ENVIAR AUSENCIAS " + ex.Message, null, Empresa);
            }
        }

        protected override void sendHNT(SesionVM Empresa, CompanyConfiguration companyConfiguration, Variables item, bool hasNWHSeparadas = false)
        {
            FileLogHelper.log(LogConstants.general, LogConstants.get, item.rut, "Enviando HNT para el usuario: " + item.rut, null, Empresa);
            NonWorkedHours nonWorkedHours = new NonWorkedHours();
            nonWorkedHours.employee_id = item.identificador_interno_de_ellos;
            nonWorkedHours.hours = item.nwh;
            nonWorkedHours.month = item.mes;
            nonWorkedHours.year = item.ano;
            nonWorkedHours.type_id = NonWorkedHoursType.Adelantos;
            try
            {
                companyConfiguration.NonWorkedHoursBusiness.AddNonWorkedHours(nonWorkedHours, Empresa, companyConfiguration);
                FileLogHelper.log(LogConstants.hnt, LogConstants.add, item.rut, string.Empty, nonWorkedHours, Empresa);
            }
            catch (Exception ex)
            {
                InsightHelper.logException(ex, Empresa.Empresa);
                FileLogHelper.log(LogConstants.hnt, LogConstants.error_add, item.rut, "ERROR AL ENVIAR HNT " + ex.Message, nonWorkedHours, Empresa);
            }
        }
    }
}
