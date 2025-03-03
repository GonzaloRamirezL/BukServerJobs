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
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace BusinessLogic.Implementation
{
    public class AttendanceBusiness : IAttendanceBusiness
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

            FileLogHelper.log(LogConstants.general, LogConstants.get, "", "EMPIEZA SYNCATTENDANCE LIBROASISTENCIA A GV", null, Empresa);
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
            var asistencia = GetAttendance(Empresa, users, startDate, endDate, companyConfiguration);
            properties["GetAttendance"] = (users.Count * (endDate - startDate).Days).ToString();

            List<Variables> processed = new List<Variables>();

            FileLogHelper.log(LogConstants.general, LogConstants.get, "", "PIDIENDO ESTADO DE ACTIVACION DE USUARIOS A GV", null, Empresa);
            var userStatusLogs = companyConfiguration.UserStatusLogBusiness.GetUserStatusLogs(users, Empresa, companyConfiguration);
            userStatusLogs = reCalculateActivePeriodStart(userStatusLogs, employees);

            FileLogHelper.log(LogConstants.general, LogConstants.get, "", "PROCESANDO OPERACIONES A REALIZAR CON LIBRO DE ASISTENCIA", null, Empresa);
            processed = processData(asistencia, users, fechas, periodo, userStatusLogs, employees);

            FileLogHelper.log(LogConstants.general, LogConstants.get, "", "ENVIANDO OPERACIONES A BUK", null, Empresa);
            Dictionary<string, string> result = SendData(processed, Empresa, companyConfiguration);

            foreach (KeyValuePair<string, string> res in result)
            {
                properties[res.Key] = res.Value;
            }

            InsightHelper.logMetric("SyncAttendance", DateTime.Now - startMetric, properties);
        }

        public virtual Attendance GetAttendance(SesionVM Empresa, List<User> users, DateTime startDate, DateTime endDate, CompanyConfiguration companyConfiguration)
        {
            List<string> userIdentifiers = users.Select(u => u.Identifier).ToList();

            int paso = CommonHelper.calculateIterationIncrement(userIdentifiers.Count, (endDate - startDate).Days);
            FileLogHelper.log(LogConstants.general, LogConstants.get, "", "USUARIOS A BUSCAR EN LIBRO DE ASISTENCIA: " + userIdentifiers.Count, null, Empresa);
            FileLogHelper.log(LogConstants.general, LogConstants.get, "", "ITERACIONES CON USUARIOS Y LIBRO: " + (OperationalConsts.MAXIMUN_AMOUNT_OF_REGISTERS_TO_REQUEST / paso), null, Empresa);

            Attendance result = new Attendance();
            try
            {
                for (int i = 0; i <= userIdentifiers.Count; i += paso)
                {
                    List<string> iterUsers = userIdentifiers.Skip(i).Take(paso).ToList();
                    string concat = String.Join(',', iterUsers);
                    if (i == 0)
                    {
                        result = companyConfiguration.AttendanceDAO.Get(new API.GV.DTO.Filters.AttendanceFilter { UserIds = concat, StartDate = DateTimeHelper.parseToGVFormat(startDate), EndDate = DateTimeHelper.parseToGVFormat(endDate) }, Empresa);
                    }
                    else if (!CollectionsHelper.IsNullOrEmpty<string>(iterUsers))
                    {

                        result.Users.AddRange(companyConfiguration.AttendanceDAO.Get(new API.GV.DTO.Filters.AttendanceFilter { UserIds = concat, StartDate = DateTimeHelper.parseToGVFormat(startDate), EndDate = DateTimeHelper.parseToGVFormat(endDate) }, Empresa).Users);
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

        /// <summary>
        /// Procesa los libros de asistencia de GV y los transforma en las variables que se deben enviar a BUK
        /// </summary>
        /// <param name="attendance"></param>
        /// <param name="user"></param>
        /// <param name="fechas"></param>
        /// <param name="periodo"></param>
        /// <param name="userStatusLogs"></param>
        protected virtual List<Variables> processData(Attendance attendance, List<User> users, FechasProcesamientoVM fechas, ProcessPeriod periodo, List<UserStatusLogCalculatedVM> userStatusLogs, List<Employee> employees)
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

        /// <summary>
        /// Construye las inasistencias que se deben enviar a BUK
        /// </summary>       
        protected List<AbsenceToAdd> buildAbsencesToAdd(List<TimeInterval> inasistenciasIntervals, User user, DateTime? activeUntil, DateTime baseDate)
        {
            List<AbsenceToAdd> absencesToAdd = new List<AbsenceToAdd>();
            var availableApplicationDates = DateTimeHelper.AllDatesInMonth(baseDate.Year, baseDate.Month);
            foreach (TimeInterval intervalo in inasistenciasIntervals)
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
                    if (baseDate.Month == fecha.Month)
                    {
                        ausencia.applicationDateTime = fecha;
                        
                        availableApplicationDates = availableApplicationDates.Where(d => d != fecha);
                    }
                    else
                    {
                        ausencia.applicationDateTime = baseDate.AddDays(-1);
                        
                    }

                    ausencia.application_date = DateTimeHelper.parseToBUKFormat(ausencia.applicationDateTime);
                    ausencia.absence_type_id = 1;
                    absencesToAdd.Add(ausencia);
                }
            }
            foreach (var absence in absencesToAdd)
            {
                if (absence.applicationDateTime.Month != baseDate.Month)
                {
                    if (!availableApplicationDates.IsNullOrEmpty())
                    {
                        absence.applicationDateTime = availableApplicationDates.Min();
                       
                        availableApplicationDates = availableApplicationDates.Where(d => d != absence.applicationDateTime);
                    }
                    else
                    {
                        absence.applicationDateTime = baseDate;
                        
                    }
                    absence.application_date = DateTimeHelper.parseToBUKFormat(absence.applicationDateTime);
                }
            }
            return absencesToAdd;
        }

        public virtual Variables processUserData(CalculatedUser cUser, User user, List<CompanyExtraTimeValues> ExtraTimeValues, FechasProcesamientoVM fechas, ProcessPeriod periodo, UserStatusLogCalculatedVM userStatusLogs, DateTime? activeUntil)
        {
            Variables varData = new Variables();
            varData.overtimes = new Dictionary<int, double>();
            varData.ausencias = new List<AbsenceToAdd>();
            varData.atrasos = 0;
            varData.adelantos = 0;
            varData.descuentosColacion = 0;
            varData.rut = user.Identifier;
            varData.identificador_interno_de_ellos = (user.integrationCode.IsNullOrEmpty()) ? 0 : int.Parse(user.integrationCode);

            DateTime fechaBase = DateTimeHelper.parseFromBUKFormat(periodo.month);
            varData.ano = fechaBase.Year;
            varData.mes = fechaBase.Month;

            if (ExtraTimeValues != null)
            {
                foreach (CompanyExtraTimeValues value in ExtraTimeValues)
                {
                    varData.overtimes[int.Parse(value.Value)] = 0;
                }
            }
            
            List<TimeInterval> inasistenciasIntervals = segmentDataForConcept(fechas.InasistenciasStartDate, fechas.InasistenciasEndDate, cUser.PlannedInterval, userStatusLogs);
            List<TimeInterval> horasextrasIntervals = segmentDataForConcept(fechas.HorasExtrasStartDate, fechas.HorasExtrasEndDate, cUser.PlannedInterval, userStatusLogs);
            List<TimeInterval> horasnotrabajadasIntervals = segmentDataForConcept(fechas.HorasNoTrabajadasStartDate, fechas.HorasNoTrabajadasEndDate, cUser.PlannedInterval, userStatusLogs);
            varData.ausenciasDesde = fechas.InasistenciasStartDate;
            varData.ausenciasHasta = fechas.InasistenciasEndDate;
            varData.ausencias = buildAbsencesToAdd(inasistenciasIntervals, user, activeUntil, fechaBase);

            foreach (TimeInterval intervalo in horasnotrabajadasIntervals)
            {
                varData.nwh += TimeSpanHelper.HHmmToTimeSpan(intervalo.NonWorkedHours).TotalHours;
                varData.atrasos += TimeSpanHelper.HHmmToTimeSpan(intervalo.Delay).TotalHours;
                varData.adelantos += TimeSpanHelper.HHmmToTimeSpan(intervalo.EarlyLeave).TotalHours;
                varData.descuentosColacion += TimeSpanHelper.HHmmToTimeSpan(intervalo.BreakDelay).TotalHours;
            }

            foreach (TimeInterval intervalo in horasextrasIntervals)
            {
                foreach (KeyValuePair<string, string> reg in intervalo.AccomplishedExtraTime)
                {
                    if (!varData.overtimes.ContainsKey(int.Parse(reg.Key)))
                    {
                        varData.overtimes[int.Parse(reg.Key)] = 0;
                    }
                    varData.overtimes[int.Parse(reg.Key)] += TimeSpanHelper.HHmmToTimeSpan(reg.Value).TotalHours;
                }
            }

            return varData;
        }

        /// <summary>
        /// Devuelve los libros de asistencias a procesar de acuerdo a las fechas de cada concepto
        /// </summary> 
        protected List<TimeInterval> segmentDataForConcept(DateTime startDate, DateTime endDate, List<TimeInterval> intervals, UserStatusLogCalculatedVM statusLog)
        {
            intervals = intervals.FindAll(i => DateTimeHelper.parseFromGVFormat(i.Date) >= startDate && DateTimeHelper.parseFromGVFormat(i.Date) <= endDate);
            List<TimeInterval> result = new List<TimeInterval>();
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
        /// Devuelve si una fecha esta dentro del periodo activo de un usuario
        /// </summary> 
        protected bool isInActivePeriod(DateTime date, UserStatusLogCalculatedVM statusLog)
        {
            if (statusLog == null)
            {
                return true;
            }
            return statusLog.ActivePeriods.Exists(period => period.Starts <= date && date <= period.Ends);

        }

        /// <summary>
        ///Envia las variables procesadas a BUK
        /// </summary>
        /// <param name="Empresa"></param>
        /// <param name="variables"></param>
        /// <param name="hasNWHSeparadas"></param>
        /// <returns></returns>
        protected virtual Dictionary<string, string> SendData(List<Variables> variables, SesionVM Empresa, CompanyConfiguration companyConfiguration, bool hasNWHSeparadas = false)
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
                    FileLogHelper.log(LogConstants.general, LogConstants.get, item.rut, "Enviando HHEE para el usuario: " + item.rut, null, Empresa);
                    totalHHEE += item.overtimes.Count;
                    sendOvertimes(Empresa, companyConfiguration, item);
                }
            }

            Dictionary<string, string> properties = new Dictionary<string, string>();

            properties["AddNonWorkedHours"] = totalHNT.ToString();
            properties["AddAbsences"] = totalAusencias.ToString();
            properties["AddOvertimes"] = totalHHEE.ToString();
            return properties;
        }

        /// <summary>
        /// Envia horas notrabajadas de un usuario en un periodo determinado a BUK
        /// </summary> 
        protected virtual void sendHNT(SesionVM Empresa, CompanyConfiguration companyConfiguration, Variables item, bool hasNWHSeparadas = false)
        {
            if (hasNWHSeparadas)
            {
                FileLogHelper.log(LogConstants.general, LogConstants.get, item.rut, "Enviando HNT(ATRASOS) para el usuario: " + item.rut, null, Empresa);
                NonWorkedHours atrasos = new NonWorkedHours();
                atrasos.employee_id = item.identificador_interno_de_ellos;
                atrasos.hours = item.atrasos;
                atrasos.month = item.mes;
                atrasos.year = item.ano;
                atrasos.type_id = NonWorkedHoursType.General;
                try
                {
                    companyConfiguration.NonWorkedHoursBusiness.AddNonWorkedHours(atrasos, Empresa, companyConfiguration);
                    FileLogHelper.log(LogConstants.hnt, LogConstants.add, item.rut, " (ATRASOS) ", atrasos, Empresa);
                }
                catch (Exception ex)
                {
                    InsightHelper.logException(ex, Empresa.Empresa);
                    FileLogHelper.log(LogConstants.hnt, LogConstants.error_add, item.rut, "ERROR AL ENVIAR HNT(ATRASOS) " + ex.Message, atrasos, Empresa);
                }

                FileLogHelper.log(LogConstants.general, LogConstants.get, item.rut, "Enviando HNT(ADELANTOS) para el usuario: " + item.rut, null, Empresa);
                NonWorkedHours adelantos = new NonWorkedHours();
                adelantos.employee_id = item.identificador_interno_de_ellos;
                adelantos.hours = item.adelantos;
                adelantos.month = item.mes;
                adelantos.year = item.ano;
                adelantos.type_id = NonWorkedHoursType.Adelantos;
                try
                {
                    companyConfiguration.NonWorkedHoursBusiness.AddNonWorkedHours(adelantos, Empresa, companyConfiguration);
                    FileLogHelper.log(LogConstants.hnt, LogConstants.add, item.rut, " (ADELANTOS) ", adelantos, Empresa);
                }
                catch (Exception ex)
                {
                    InsightHelper.logException(ex, Empresa.Empresa);
                    FileLogHelper.log(LogConstants.hnt, LogConstants.error_add, item.rut, "ERROR AL ENVIAR HNT(ADELANTOS) " + ex.Message, adelantos, Empresa);
                }
            }
            else
            {
                FileLogHelper.log(LogConstants.general, LogConstants.get, item.rut, "Enviando HNT para el usuario: " + item.rut, null, Empresa);
                NonWorkedHours atrasos = new NonWorkedHours();
                atrasos.employee_id = item.identificador_interno_de_ellos;
                atrasos.hours = item.nwh;
                atrasos.month = item.mes;
                atrasos.year = item.ano;
                atrasos.type_id = NonWorkedHoursType.General;
                try
                {
                    companyConfiguration.NonWorkedHoursBusiness.AddNonWorkedHours(atrasos, Empresa, companyConfiguration);
                    FileLogHelper.log(LogConstants.hnt, LogConstants.add, item.rut, string.Empty, atrasos, Empresa);
                }
                catch (Exception ex)
                {
                    InsightHelper.logException(ex, Empresa.Empresa);
                    FileLogHelper.log(LogConstants.hnt, LogConstants.error_add, item.rut, "ERROR AL ENVIAR HNT " + ex.Message, atrasos, Empresa);
                }
            }
        }

        /// <summary>
        /// Envia inasistencias de un usuario en un periodo determinado a BUK
        /// </summary>
        protected virtual void sendAbsences(SesionVM Empresa, CompanyConfiguration companyConfiguration, Variables item, int standardAbsenceId)
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
            foreach (AbsenceToAdd ausencia in item.ausencias)
            {
                try
                {
                    ausencia.absence_type_id = standardAbsenceId;
                    companyConfiguration.AbsenceBusiness.AddAbsence(ausencia, Empresa, companyConfiguration);
                    FileLogHelper.log(LogConstants.absences, LogConstants.add, item.rut, string.Empty, ausencia, Empresa);
                }
                catch (Exception ex)
                {
                    InsightHelper.logException(ex, Empresa.Empresa);
                    FileLogHelper.log(LogConstants.absences, LogConstants.error_add, item.rut, "ERROR AL ENVIAR AUSENCIAS " + ex.Message, ausencia, Empresa);
                }
            }
        }      

        /// <summary>
        /// Envia horas extras de un usuario en un periodo determinado a BUK
        /// </summary>
        protected virtual void sendOvertimes(SesionVM Empresa, CompanyConfiguration companyConfiguration, Variables item)
        {
            List<OvertimeType> tipos = companyConfiguration.OvertimeBusiness.GetOvertimeTypes(Empresa, companyConfiguration).OrderBy(o => o.id).ToList();
            foreach (KeyValuePair<int, double> reg in item.overtimes)
            {
                OvertimeType tipo = tipos.FirstOrDefault(t => t.proporcion * 100 == reg.Key);
                if (tipo != null)
                {
                    FileLogHelper.log(LogConstants.general, LogConstants.get, item.rut, "Enviando HHEE (" + reg.Key + "%): " + reg.Value, null, Empresa);
                    Overtime newOvertime = new Overtime();
                    newOvertime.centro_costo = "";
                    newOvertime.employee_id = item.identificador_interno_de_ellos;
                    newOvertime.hours = reg.Value;
                    newOvertime.month = item.mes;
                    newOvertime.year = item.ano;
                    newOvertime.type_id = tipo.id;
                    OvertimeTypeHelper.getType(reg.Key);
                    try
                    {
                        companyConfiguration.OvertimeBusiness.AddOverTime(newOvertime, Empresa, companyConfiguration);
                        FileLogHelper.log(LogConstants.hhee, LogConstants.add, item.rut, "", newOvertime, Empresa);
                    }
                    catch (Exception ex)
                    {
                        InsightHelper.logException(ex, Empresa.Empresa);
                        FileLogHelper.log(LogConstants.hhee, LogConstants.error_add, item.rut, ex.ToString(), newOvertime, Empresa);

                    }
                }
                else
                {
                    FileLogHelper.log(LogConstants.hhee, LogConstants.error_add, item.rut, "Porcentaje de Horas Extras no encontrado en BUK: " + reg.Key, null, Empresa);
                }


            }
        }

        protected virtual List<UserStatusLogCalculatedVM> reCalculateActivePeriodStart(List<UserStatusLogCalculatedVM> statusLogs, List<Employee> employees)
        {
            List<UserStatusLogCalculatedVM> userStatusLogs = new List<UserStatusLogCalculatedVM>();
            foreach (var statusLog in statusLogs)
            {
                Employee employee = employees.FirstOrDefault(e => String.Equals(CommonHelper.rutToGVFormat(e.rut), statusLog.Identifier, StringComparison.OrdinalIgnoreCase));
                if (employee != null && !string.IsNullOrWhiteSpace(employee.active_since) && !isInActivePeriod(DateTimeHelper.parseFromBUKFormat(employee.active_since), statusLog))
                {
                    var activePeriod = findNextActivePeriod(DateTimeHelper.parseFromBUKFormat(employee.active_since), statusLog.ActivePeriods);
                    if (activePeriod != null)
                    {
                        UserStatusLogCalculatedVM statusLogModified = new UserStatusLogCalculatedVM();
                        statusLogModified.Identifier = statusLog.Identifier;
                        statusLogModified.ActivePeriods = new List<ActivePeriodCalculatedVM>();
                        foreach (var ap in statusLog.ActivePeriods)
                        {
                            if (ap.Starts == activePeriod.Starts && ap.Ends == activePeriod.Ends)
                            {
                                ap.Starts = DateTimeHelper.parseFromBUKFormat(employee.active_since);
                            }
                            statusLogModified.ActivePeriods.Add(ap);
                        }
                        userStatusLogs.Add(statusLogModified);
                    }
                    else
                    {
                        userStatusLogs.Add(statusLog);
                    }
                }
                else
                {
                    userStatusLogs.Add(statusLog);
                }
            }
            return userStatusLogs;
        }

        protected ActivePeriodCalculatedVM findNextActivePeriod(DateTime activeSince, List<ActivePeriodCalculatedVM> activePeriods)
        {
            TimeSpan difference = TimeSpan.Zero;
            ActivePeriodCalculatedVM result = null;
            foreach (var activePeriod in activePeriods)
            {
                TimeSpan iterDiff = activePeriod.Starts - activeSince;
                if (iterDiff > TimeSpan.Zero)
                {
                    if (difference > TimeSpan.Zero && iterDiff < difference)
                    {
                        difference = iterDiff;
                        result = activePeriod;
                    }
                    else if (difference == TimeSpan.Zero)
                    {
                        difference = iterDiff;
                        result = activePeriod;
                    }

                }
            }
            return result;
        }
    }
}
