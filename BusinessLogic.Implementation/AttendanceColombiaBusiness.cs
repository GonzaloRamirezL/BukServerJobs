using API.BUK.DTO;
using API.BUK.DTO.Consts;
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
using System.Threading.Tasks;

namespace BusinessLogic.Implementation
{
    public class AttendanceColombiaBusiness : AttendanceBusiness
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
            FileLogHelper.log(LogConstants.general, LogConstants.get, "", "EMPIEZA SYNCATTENDANCE-COLOMBIA", null, Empresa);
            List<DateTime> fechasOrdenadas = fechas.ToList();
            DateTime startDate = fechasOrdenadas[0];
            DateTime endDate = fechasOrdenadas.Last();
            #endregion

            #region Usuarios
            FileLogHelper.log(LogConstants.general, LogConstants.get, "", "PIDIENDO EMPLEADOS A BUK", null, Empresa);
            List<Employee> employees = companyConfiguration.EmployeeBusiness.GetEmployeesForSync(Empresa, companyConfiguration, startDate, endDate);

            FileLogHelper.log(LogConstants.general, LogConstants.get, "", "PIDIENDO USUARIOS A GV", null, Empresa);
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
            AttendanceColombia asistencia = (AttendanceColombia)GetAttendance(Empresa, users, startDate, endDate, companyConfiguration);
            properties["GetAttendance"] = (users.Count * (endDate - startDate).Days).ToString();

            List<Variables> processed = new List<Variables>();
            FileLogHelper.log(LogConstants.general, LogConstants.get, "", "PIDIENDO ESTADO DE ACTIVACION DE USUARIOS A GV", null, Empresa);
            var userStatusLogs = companyConfiguration.UserStatusLogBusiness.GetUserStatusLogs(users, Empresa, companyConfiguration);
            userStatusLogs = reCalculateActivePeriodStart(userStatusLogs, employees);

            FileLogHelper.log(LogConstants.general, LogConstants.get, "", "PROCESANDO OPERACIONES A REALIZAR CON LIBRO DE ASISTENCIA", null, Empresa);
            processed = this.processData(asistencia, users, fechas, periodo, userStatusLogs, employees);

            FileLogHelper.log(LogConstants.general, LogConstants.get, "", "ENVIANDO OPERACIONES A BUK", null, Empresa);
            Dictionary<string, string> result = SendData(processed, Empresa, companyConfiguration);

            foreach (KeyValuePair<string, string> res in result)
            {
                properties[res.Key] = res.Value;
            }

            InsightHelper.logMetric("SyncAttendance", DateTime.Now - startMetric, properties);
        }

        /// <summary>
        /// Procesa los libros de asistencia de GV y los transforma en las variables que se deben enviar a BUK
        /// </summary>
        /// <param name="attendance"></param>
        /// <param name="user"></param>
        /// <param name="fechas"></param>
        /// <param name="periodo"></param>
        /// <param name="userStatusLogs"></param>
        protected List<Variables> processData(AttendanceColombia attendance, List<User> users, FechasProcesamientoVM fechas, ProcessPeriod periodo, List<UserStatusLogCalculatedVM> userStatusLogs, List<Employee> employees)
        {
            List<Variables> processed = new List<Variables>();
            var commonAttendance = attendance;
            foreach (var user in users)
            {
                var calculated = commonAttendance.Users.FirstOrDefault(u => u.Identifier == user.Identifier);
                var employee = employees.FirstOrDefault(e => e.id == int.Parse(user.integrationCode));
                if (calculated != null && employee != null)
                {
                    var statusLog = userStatusLogs.FirstOrDefault(u => u.Identifier == user.Identifier);
                    DateTime? activeUntil = null;
                    if (employee.current_job != null && !String.IsNullOrWhiteSpace(employee.current_job.active_until))
                    {
                        activeUntil = DateTimeHelper.parseFromBUKFormat(employee.current_job.active_until);
                    }

                    processed.Add(processUserData(calculated, user, commonAttendance.ExtraTimeValues, fechas, periodo, statusLog, activeUntil));
                }
            }
            return processed;
        }

        public override Attendance GetAttendance(SesionVM Empresa, List<User> users, DateTime startDate, DateTime endDate, CompanyConfiguration companyConfiguration)
        {
            List<string> userIdentifiers = users.Select(u => u.Identifier).ToList();

            int paso = CommonHelper.calculateIterationIncrement(userIdentifiers.Count, (endDate - startDate).Days);
            FileLogHelper.log(LogConstants.general, LogConstants.get, "", "USUARIOS A BUSCAR EN LIBRO DE ASISTENCIA: " + userIdentifiers.Count, null, Empresa);
            FileLogHelper.log(LogConstants.general, LogConstants.get, "", "ITERACIONES CON USUARIOS Y LIBRO: " + (OperationalConsts.MAXIMUN_AMOUNT_OF_REGISTERS_TO_REQUEST / paso), null, Empresa);

            AttendanceColombia result = new AttendanceColombia();
            try
            {
                for (int i = 0; i <= userIdentifiers.Count; i += paso)
                {
                    List<string> iterUsers = userIdentifiers.Skip(i).Take(paso).ToList();
                    string concat = String.Join(',', iterUsers);
                    if (i == 0)
                    {
                        result = (AttendanceColombia)companyConfiguration.AttendanceDAO.Get(new API.GV.DTO.Filters.AttendanceFilter { UserIds = concat, StartDate = DateTimeHelper.parseToGVFormat(startDate), EndDate = DateTimeHelper.parseToGVFormat(endDate) }, Empresa);
                    }
                    else if (!CollectionsHelper.IsNullOrEmpty<string>(iterUsers))
                    {
                        var parcial = (AttendanceColombia)companyConfiguration.AttendanceDAO.Get(new API.GV.DTO.Filters.AttendanceFilter { UserIds = concat, StartDate = DateTimeHelper.parseToGVFormat(startDate), EndDate = DateTimeHelper.parseToGVFormat(endDate) }, Empresa);
                        result.Users.AddRange(parcial.Users);
                    }
                }
            }
            catch (Exception ex)
            {
                InsightHelper.logException(ex, Empresa.Empresa);
                FileLogHelper.log(LogConstants.general, LogConstants.get, "", "ERROR AL OBTENER LIBRO DE ASISTENCIA DESDE GV", null, Empresa);
                throw new Exception("Incomplete data from GV");
            }

            return result;
        }

        public Variables processUserData(CalculatedUserColombia cUser, User user, List<CompanyExtraTimeValues> ExtraTimeValues, FechasProcesamientoVM fechas, ProcessPeriod periodo, UserStatusLogCalculatedVM userStatusLogs, DateTime? activeUntil)
        {
            var colombiaCUser = (CalculatedUserColombia)cUser;

            Variables varData = new Variables();
            varData.overtimes = new Dictionary<int, double>();
            varData.ausencias = new List<AbsenceToAdd>();
            varData.atrasos = 0;
            varData.adelantos = 0;
            varData.descuentosColacion = 0;
            varData.identificador_interno_de_ellos = int.Parse(user.integrationCode);
            varData.rut = user.Identifier;
            DateTime fechaBase = DateTimeHelper.parseFromBUKFormat(periodo.month);
            varData.ano = fechaBase.Year;
            varData.mes = fechaBase.Month;
            varData.overtimes = iniatializeColombianConcepts();
            varData.ausenciasDesde = fechas.InasistenciasStartDate;
            varData.ausenciasHasta = fechas.InasistenciasEndDate;
            List<TimeIntervalColombia> inasistenciasIntervals = segmentDataForConcept(fechas.InasistenciasStartDate, fechas.InasistenciasEndDate, colombiaCUser.PlannedInterval, userStatusLogs);
            List<TimeIntervalColombia> horasextrasIntervals = segmentDataForConcept(fechas.HorasExtrasStartDate, fechas.HorasExtrasEndDate, colombiaCUser.PlannedInterval, userStatusLogs);
            List<TimeIntervalColombia> horasnotrabajadasIntervals = segmentDataForConcept(fechas.HorasNoTrabajadasStartDate, fechas.HorasNoTrabajadasEndDate, colombiaCUser.PlannedInterval, userStatusLogs);

            foreach (TimeIntervalColombia intervalo in inasistenciasIntervals)
            {
                if (bool.Parse(intervalo.Absent) && !bool.Parse(intervalo.Holiday) && intervalo.TimeOffs.IsNullOrEmpty())
                {
                    DateTime fecha = DateTimeHelper.parseFromGVFormat(intervalo.Date);
                    if (activeUntil.HasValue && fecha > activeUntil.Value)
                    {
                        continue;
                    }

                    AbsenceToAdd ausencia = new AbsenceToAdd();
                    ausencia.days_count = 1;
                    ausencia.employee_id = int.Parse(user.integrationCode);
                    ausencia.start_date = DateTimeHelper.parseToBUKFormat(fecha);
                    ausencia.application_date = DateTimeHelper.parseToBUKFormat(fecha);

                    ausencia.absence_type_id = 1;
                    varData.ausencias.Add(ausencia);
                }
            }

            foreach (TimeIntervalColombia intervalo in horasnotrabajadasIntervals)
            {
                string delayTimeAfter = string.IsNullOrEmpty(intervalo.DelayTimeAfterCompensation) ? intervalo.Delay : intervalo.DelayTimeAfterCompensation;
                string earlyLeaveAfter = string.IsNullOrEmpty(intervalo.EarlyLeaveTimeAfterCompensation) ? intervalo.EarlyLeave : intervalo.EarlyLeaveTimeAfterCompensation;
                varData.atrasos += TimeSpanHelper.HHmmToTimeSpan(delayTimeAfter).TotalHours;
                varData.adelantos += TimeSpanHelper.HHmmToTimeSpan(earlyLeaveAfter).TotalHours;
                varData.descuentosColacion += TimeSpanHelper.HHmmToTimeSpan(intervalo.BreakDelay).TotalHours;
            }
            varData.nwh = varData.adelantos + varData.atrasos + varData.descuentosColacion;

            foreach (TimeIntervalColombia intervalo in horasextrasIntervals)
            {
                #region Overtimes
                varData.overtimes[ColombiaSurChargeIdentifiers.HoraExtraDiurnaOrdinarias] += TimeSpanHelper.HHmmToTimeSpan(intervalo.OrdinaryDiurnalOvertime).TotalHours;
                varData.overtimes[ColombiaSurChargeIdentifiers.HorasExtraDiurnaDominical] += TimeSpanHelper.HHmmToTimeSpan(intervalo.SundayDiurnalOvertime).TotalHours;
                varData.overtimes[ColombiaSurChargeIdentifiers.HorasExtraDiurnaFestiva] += TimeSpanHelper.HHmmToTimeSpan(intervalo.HolidayDiurnalOvertime).TotalHours;
                varData.overtimes[ColombiaSurChargeIdentifiers.HorasExtraNocturnaDominical] += TimeSpanHelper.HHmmToTimeSpan(intervalo.SundayNocturnalOvertime).TotalHours;
                varData.overtimes[ColombiaSurChargeIdentifiers.HorasExtraNocturnaFestiva] += TimeSpanHelper.HHmmToTimeSpan(intervalo.HolidayNocturnalOvertime).TotalHours;
                varData.overtimes[ColombiaSurChargeIdentifiers.HorasExtraNocturnaOrdinarias] += TimeSpanHelper.HHmmToTimeSpan(intervalo.OrdinaryNocturnalOvertime).TotalHours;
                #endregion

                #region Surcharges
                if (intervalo.Surcharge != null)
                {
                    varData.overtimes[ColombiaSurChargeIdentifiers.RecargoDiurnoDominicalCompensado] += TimeSpanHelper.HHmmToTimeSpan(intervalo.Surcharge.CompensatedSundayDiurnalSurchargeHours).TotalHours;
                    varData.overtimes[ColombiaSurChargeIdentifiers.RecargoDiurnoDominicalNoCompensado] += TimeSpanHelper.HHmmToTimeSpan(intervalo.Surcharge.NonCompensatedSundayDiurnalSurchargeHours).TotalHours;
                    varData.overtimes[ColombiaSurChargeIdentifiers.RecargoDiurnoFestivoCompensado] += TimeSpanHelper.HHmmToTimeSpan(intervalo.Surcharge.CompensatedHolidayDiurnalSurchargeHours).TotalHours;
                    varData.overtimes[ColombiaSurChargeIdentifiers.RecargoDiurnoFestivoNoCompensado] += TimeSpanHelper.HHmmToTimeSpan(intervalo.Surcharge.NonCompensatedHolidayDiurnalSurchargeHours).TotalHours;
                    varData.overtimes[ColombiaSurChargeIdentifiers.RecargoNocturnoDominicalCompensado] += TimeSpanHelper.HHmmToTimeSpan(intervalo.Surcharge.CompensatedSundayNocturnalSurchargeHours).TotalHours;
                    varData.overtimes[ColombiaSurChargeIdentifiers.RecargoNocturnoDominicalNoCompensado] += TimeSpanHelper.HHmmToTimeSpan(intervalo.Surcharge.NonCompensatedSundayNocturnalSurchargeHours).TotalHours;
                    varData.overtimes[ColombiaSurChargeIdentifiers.RecargoNocturnoFestivoCompensado] += TimeSpanHelper.HHmmToTimeSpan(intervalo.Surcharge.CompensatedHolidayNocturnalSurchargeHours).TotalHours;
                    varData.overtimes[ColombiaSurChargeIdentifiers.RecargoNocturnoFestivoNoCompensado] += TimeSpanHelper.HHmmToTimeSpan(intervalo.Surcharge.NonCompensatedHolidayNocturnalSurchargeHours).TotalHours;
                    varData.overtimes[ColombiaSurChargeIdentifiers.RecargoNocturnoOrdinario] += TimeSpanHelper.HHmmToTimeSpan(intervalo.Surcharge.OrdinaryNocturnalSurchargeHours).TotalHours;
                }
                #endregion
            }
            return varData;
        }

        /// <summary>
        /// Devuelve los libros de asistencias a procesar de acuerdo a las fechas de cada concepto
        /// </summary>
        protected List<TimeIntervalColombia> segmentDataForConcept(DateTime startDate, DateTime endDate, List<TimeIntervalColombia> intervals, UserStatusLogCalculatedVM statusLog)
        {
            intervals = intervals.FindAll(i => DateTimeHelper.parseFromGVFormat(i.Date) >= startDate && DateTimeHelper.parseFromGVFormat(i.Date) <= endDate);
            List<TimeIntervalColombia> result = new List<TimeIntervalColombia>();
            foreach (var interval in intervals)
            {
                DateTime date = DateTimeHelper.parseFromGVFormat(interval.Date);
                if (isInActivePeriod(date, statusLog))
                {
                    result.Add(interval);
                }
            }
            return result;
        }

        

        /// <summary>
        /// Inicializa el diccionario de conceptos de Colombia
        /// </summary>
        /// <returns></returns>
        protected Dictionary<int, double> iniatializeColombianConcepts()
        {
            Dictionary<int, double> concepts = new Dictionary<int, double>();
            concepts[ColombiaSurChargeIdentifiers.HoraExtraDiurnaOrdinarias] = 0;
            concepts[ColombiaSurChargeIdentifiers.HorasExtraDiurnaDominical] = 0;
            concepts[ColombiaSurChargeIdentifiers.HorasExtraDiurnaFestiva] = 0;
            concepts[ColombiaSurChargeIdentifiers.HorasExtraNocturnaDominical] = 0;
            concepts[ColombiaSurChargeIdentifiers.HorasExtraNocturnaFestiva] = 0;
            concepts[ColombiaSurChargeIdentifiers.HorasExtraNocturnaOrdinarias] = 0;
            concepts[ColombiaSurChargeIdentifiers.RecargoDiurnoDominicalCompensado] = 0;
            concepts[ColombiaSurChargeIdentifiers.RecargoDiurnoDominicalNoCompensado] = 0;
            concepts[ColombiaSurChargeIdentifiers.RecargoDiurnoFestivoCompensado] = 0;
            concepts[ColombiaSurChargeIdentifiers.RecargoDiurnoFestivoNoCompensado] = 0;
            concepts[ColombiaSurChargeIdentifiers.RecargoNocturnoDominicalCompensado] = 0;
            concepts[ColombiaSurChargeIdentifiers.RecargoNocturnoDominicalNoCompensado] = 0;
            concepts[ColombiaSurChargeIdentifiers.RecargoNocturnoFestivoCompensado] = 0;
            concepts[ColombiaSurChargeIdentifiers.RecargoNocturnoFestivoNoCompensado] = 0;
            concepts[ColombiaSurChargeIdentifiers.RecargoNocturnoOrdinario] = 0;
            return concepts;
        }

        /// <summary>
        /// Obtiene los valores que no son ceros dentro de los libros de asistencia de Colombia
        /// </summary>
        /// <param name="intervalos"></param>
        /// <returns></returns>
        protected bool noZeros(List<TimeIntervalColombia> intervalos)
        {
            return intervalos.Any(i => noZeros(i));
        }

        /// <summary>
        /// Devuelve si todos los valores de un libro de asistencia Colombia son ceros o no
        /// </summary>
        /// <param name="intervalo"></param>
        /// <returns></returns>
        protected bool noZeros(TimeIntervalColombia intervalo)
        {
            return
             (intervalo.OrdinaryDiurnalOvertime != null && intervalo.OrdinaryDiurnalOvertime != "--:--" && intervalo.OrdinaryDiurnalOvertime != "00:00")
               || (intervalo.SundayDiurnalOvertime != null && intervalo.SundayDiurnalOvertime != "--:--" && intervalo.SundayDiurnalOvertime != "00:00")
               || (intervalo.HolidayDiurnalOvertime != null && intervalo.HolidayDiurnalOvertime != "--:--" && intervalo.HolidayDiurnalOvertime != "00:00")
               || (intervalo.SundayNocturnalOvertime != null && intervalo.SundayNocturnalOvertime != "--:--" && intervalo.SundayNocturnalOvertime != "00:00")
               || (intervalo.HolidayNocturnalOvertime != null && intervalo.HolidayNocturnalOvertime != "--:--" && intervalo.HolidayNocturnalOvertime != "00:00")
               || (intervalo.OrdinaryNocturnalOvertime != null && intervalo.OrdinaryNocturnalOvertime != "--:--" && intervalo.OrdinaryNocturnalOvertime != "00:00")
               || ((intervalo.Surcharge != null)
                    && (
                    (intervalo.Surcharge.CompensatedSundayDiurnalSurchargeHours != null && intervalo.Surcharge.CompensatedSundayDiurnalSurchargeHours != "--:--" && intervalo.Surcharge.CompensatedSundayDiurnalSurchargeHours != "00:00")
                    || (intervalo.Surcharge.NonCompensatedSundayDiurnalSurchargeHours != null && intervalo.Surcharge.NonCompensatedSundayDiurnalSurchargeHours != "--:--" && intervalo.Surcharge.NonCompensatedSundayDiurnalSurchargeHours != "00:00")
                    || (intervalo.Surcharge.CompensatedHolidayDiurnalSurchargeHours != null && intervalo.Surcharge.CompensatedHolidayDiurnalSurchargeHours != "--:--" && intervalo.Surcharge.CompensatedHolidayDiurnalSurchargeHours != "00:00")
                    || (intervalo.Surcharge.NonCompensatedHolidayDiurnalSurchargeHours != null && intervalo.Surcharge.NonCompensatedHolidayDiurnalSurchargeHours != "--:--" && intervalo.Surcharge.NonCompensatedHolidayDiurnalSurchargeHours != "00:00")
                    || (intervalo.Surcharge.CompensatedSundayNocturnalSurchargeHours != null && intervalo.Surcharge.CompensatedSundayNocturnalSurchargeHours != "--:--" && intervalo.Surcharge.CompensatedSundayNocturnalSurchargeHours != "00:00")
                    || (intervalo.Surcharge.NonCompensatedSundayNocturnalSurchargeHours != null && intervalo.Surcharge.NonCompensatedSundayNocturnalSurchargeHours != "--:--" && intervalo.Surcharge.NonCompensatedSundayNocturnalSurchargeHours != "00:00")
                    || (intervalo.Surcharge.CompensatedHolidayNocturnalSurchargeHours != null && intervalo.Surcharge.CompensatedHolidayNocturnalSurchargeHours != "--:--" && intervalo.Surcharge.CompensatedHolidayNocturnalSurchargeHours != "00:00")
                    || (intervalo.Surcharge.NonCompensatedHolidayNocturnalSurchargeHours != null && intervalo.Surcharge.NonCompensatedHolidayNocturnalSurchargeHours != "--:--" && intervalo.Surcharge.NonCompensatedHolidayNocturnalSurchargeHours != "00:00")
                    || (intervalo.Surcharge.OrdinaryNocturnalSurchargeHours != null && intervalo.Surcharge.OrdinaryNocturnalSurchargeHours != "--:--" && intervalo.Surcharge.OrdinaryNocturnalSurchargeHours != "00:00")
                    )
               );

        }

        /// <summary>
        ///Envia las variables procesadas a BUK
        /// </summary>
        /// <param name="Empresa"></param>
        /// <param name="variables"></param>
        /// <param name="hasNWHSeparadas"></param>
        /// <returns></returns>
        protected override Dictionary<string, string> SendData(List<Variables> variables, SesionVM Empresa, CompanyConfiguration companyConfiguration, bool hasNWHSeparadas = false)
        {

            int totalHNT = 0;
            int totalAusencias = 0;
            int totalHHEE = 0;
            int total = variables.Count;
            FileLogHelper.log(LogConstants.general, LogConstants.get, "", "ENVIANDO OPERACIONES A BUK PARA UN TOTAL DE " + total + " USUARIO(S)", null, Empresa);

            //int standardAbsenceId = companyConfiguration.AbsenceBusiness.FindStandardAbsenceId(Empresa, companyConfiguration);
            int current = 0;
            object lockCurrent = new object();
            ParallelOptions pOptions = new ParallelOptions();
            pOptions.MaxDegreeOfParallelism = 2;
            Parallel.ForEach(variables, pOptions, item =>
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
                if (Empresa.EnviaAusencia)
                {
                    FileLogHelper.log(LogConstants.general, LogConstants.get, item.rut, "Enviando AUSENCIAS para el usuario: " + item.rut, null, Empresa);
                    sendAbsences(Empresa, companyConfiguration, item, 2);
                }

                if (Empresa.EnviaHHEE)
                {
                    FileLogHelper.log(LogConstants.general, LogConstants.get, item.rut, "Enviando HHEE para el usuario: " + item.rut, null, Empresa);
                    totalHHEE += item.overtimes.Count;
                    sendOvertimes(Empresa, companyConfiguration, item);
                }
            });

            Dictionary<string, string> properties = new Dictionary<string, string>();

            properties["AddNonWorkedHours"] = totalHNT.ToString();
            properties["AddAbsences"] = totalAusencias.ToString();
            properties["AddOvertimes"] = totalHHEE.ToString();
            return properties;
        }

        /// <summary>
        /// Envia horas extras de un usuario en un periodo determinado a BUK
        /// </summary>
        protected override void sendOvertimes(SesionVM Empresa, CompanyConfiguration companyConfiguration, Variables item)
        {
            //List<OvertimeType> tipos = companyConfiguration.OvertimeBusiness.GetOvertimeTypes(Empresa, companyConfiguration).OrderBy(o => o.id).ToList();
            foreach (KeyValuePair<int, double> reg in item.overtimes)
            {
                FileLogHelper.log(LogConstants.general, LogConstants.get, item.rut, "Enviando HHEE (" + reg.Key + "%): " + reg.Value, null, Empresa);
                Overtime newOvertime = new Overtime();
                newOvertime.centro_costo = "";
                newOvertime.employee_id = item.identificador_interno_de_ellos;
                newOvertime.hours = reg.Value;
                newOvertime.month = item.mes;
                newOvertime.year = item.ano;
                newOvertime.type_id = reg.Key;

                try
                {
                    companyConfiguration.OvertimeBusiness.AddOverTime(newOvertime, Empresa, companyConfiguration);
                    FileLogHelper.log(LogConstants.hhee, LogConstants.add, item.rut, "", newOvertime, Empresa);
                }
                catch (Exception ex)
                {
                    InsightHelper.logException(ex, Empresa.Empresa);
                    FileLogHelper.log(LogConstants.hhee, LogConstants.error_add, "", ex.ToString(), newOvertime, Empresa);
                }
            }
        }

    }
}
