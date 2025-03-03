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

namespace BusinessLogic.Implementation
{
    public class AttendanceMultipleSheetBusiness : AttendanceBusiness
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
            List<Employee> employees = new List<Employee>();
            employees = companyConfiguration.EmployeeBusiness.GetEmployeesForSync(Empresa, companyConfiguration, startDate, endDate);

            FileLogHelper.log(LogConstants.general, LogConstants.get, "", "PIDIENDO USUARIOS A GV", null, Empresa);
            var persons = companyConfiguration.UserBusiness.GetUsersForSync(Empresa, companyConfiguration, employees, Operacion.ASISTENCIA);
            List<User> users = persons.Item1;
            employees = persons.Item2;
            properties["users"] = users.Count.ToString();
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
            processed = processData(asistencia, users, fechas, periodo, userStatusLogs, employees, companyConfiguration, Empresa);

            FileLogHelper.log(LogConstants.general, LogConstants.get, "", "ENVIANDO OPERACIONES A BUK", null, Empresa);
            Dictionary<string, string> result = SendData(processed, Empresa, companyConfiguration);

            foreach (KeyValuePair<string, string> res in result)
            {
                properties[res.Key] = res.Value;
            }

            InsightHelper.logMetric("SyncAttendance", DateTime.Now - startMetric, properties);
        }

        private DateTime calculateEndDate(DateTime fechaBase, int desfase, int fechaCorte, string tipoDesfase)
        {
            DateTime fechaBaseModificada = new DateTime();
            if (tipoDesfase == "DIAS")
            {
                int days = DateTime.DaysInMonth(fechaBase.Year, fechaBase.Month);
                if (days < fechaCorte)
                {
                    fechaBaseModificada = new DateTime(fechaBase.Year, fechaBase.Month, days);
                }
                else
                {
                    fechaBaseModificada = new DateTime(fechaBase.Year, fechaBase.Month, fechaCorte);
                }
                return fechaBaseModificada.AddDays(desfase);
            }

            fechaBaseModificada = new DateTime(fechaBase.AddMonths(-desfase).Year, fechaBase.AddMonths(-desfase).Month, 1);

            if (fechaBaseModificada.AddMonths(1).AddDays(-1).Day < fechaCorte)
            {
                return fechaBaseModificada.AddMonths(1).AddDays(-1);
            }

            return new DateTime(fechaBaseModificada.Year, fechaBaseModificada.Month, fechaCorte);
        }

        private List<Variables> processData(Attendance attendance, List<User> users, FechasProcesamientoVM fechas, ProcessPeriod periodo, List<UserStatusLogCalculatedVM> userStatusLogs, List<Employee> employees, CompanyConfiguration companyConfiguration, SesionVM empresa)
        {
            List<Variables> processed = new List<Variables>();
            var commonAttendance = attendance;
            foreach (var user in users)
            {
                var calculated = commonAttendance.Users.FirstOrDefault(u => u.Identifier == user.Identifier);
                if (!string.IsNullOrWhiteSpace(user.integrationCode))
                {
                    var idsIntegrated = user.integrationCode.Split(',');
                    var sheets = employees.FindAll(e => idsIntegrated.Contains(e.id.ToString()));
                    if (calculated != null && !sheets.IsNullOrEmpty())
                    {
                        var statusLog = userStatusLogs.FirstOrDefault(u => u.Identifier == user.Identifier);
                        processed.Add(processUserData(calculated, user, commonAttendance.ExtraTimeValues, fechas, periodo, statusLog, sheets, companyConfiguration, empresa));
                    }
                }
            }

            return processed;
        }

        public Variables processUserData(CalculatedUser cUser, User user, List<CompanyExtraTimeValues> ExtraTimeValues, FechasProcesamientoVM fechas, ProcessPeriod periodo, UserStatusLogCalculatedVM userStatusLogs, List<Employee> sheets, CompanyConfiguration companyConfiguration, SesionVM Empresa)
        {
            Variables varData = new Variables();
            varData.overtimes = new Dictionary<int, double>();
            varData.ausencias = new List<AbsenceToAdd>();
            varData.atrasos = 0;
            varData.adelantos = 0;
            varData.descuentosColacion = 0;
            varData.rut = user.Identifier;
            List<OvertimeType> tipos = companyConfiguration.OvertimeBusiness.GetOvertimeTypes(Empresa, companyConfiguration).OrderBy(o => o.id).ToList();
            DateTime fechaBase = DateTimeHelper.parseFromBUKFormat(periodo.month);
            varData.ano = fechaBase.Year;
            varData.mes = fechaBase.Month;
            varData.nonWorkedHoursByEmployees = new List<NonWorkedHours>();
            varData.overtimesByEmployees = new List<Overtime>();

            foreach (CompanyExtraTimeValues value in ExtraTimeValues)
            {
                varData.overtimes[int.Parse(value.Value)] = 0;
            }
            List<TimeInterval> inasistenciasIntervals = segmentDataForConcept(fechas.InasistenciasStartDate, fechas.InasistenciasEndDate, cUser.PlannedInterval, userStatusLogs);
            List<TimeInterval> horasextrasIntervals = segmentDataForConcept(fechas.HorasExtrasStartDate, fechas.HorasExtrasEndDate, cUser.PlannedInterval, userStatusLogs);
            List<TimeInterval> horasnotrabajadasIntervals = segmentDataForConcept(fechas.HorasNoTrabajadasStartDate, fechas.HorasNoTrabajadasEndDate, cUser.PlannedInterval, userStatusLogs);
            varData.ausenciasDesde = fechas.InasistenciasStartDate;
            varData.ausenciasHasta = fechas.InasistenciasEndDate;
            varData.absenceses_sheets_id = new List<int>();
            var availableApplicationDates = DateTimeHelper.AllDatesInMonth(fechaBase.Year, fechaBase.Month);
            foreach (TimeInterval intervalo in inasistenciasIntervals)
            {

                if (bool.Parse(intervalo.Absent) && !bool.Parse(intervalo.Holiday) && intervalo.TimeOffs.IsNullOrEmpty())
                {
                    DateTime fecha = DateTimeHelper.parseFromGVFormat(intervalo.Date);
                    Employee employee = getSheetForDate(sheets, fecha);
                    if (employee != null && employee.current_job != null && !String.IsNullOrWhiteSpace(employee.current_job.active_until))
                    {
                        if (fecha > DateTimeHelper.parseFromBUKFormat(employee.current_job.active_until))
                        {
                            continue;
                        }
                    }

                    if (employee != null)
                    {
                        AbsenceToAdd ausencia = new AbsenceToAdd();
                        ausencia.days_count = 1;
                        ausencia.employee_id = (int)employee.id;
                        ausencia.start_date = DateTimeHelper.parseToBUKFormat(fecha);
                        if (fechaBase.Month == fecha.Month)
                        {
                            ausencia.applicationDateTime = fecha;

                            availableApplicationDates = availableApplicationDates.Where(d => d != fecha);
                        }
                        else
                        {
                            ausencia.applicationDateTime = fechaBase.AddDays(-1);

                        }

                        ausencia.application_date = DateTimeHelper.parseToBUKFormat(ausencia.applicationDateTime);

                        ausencia.absence_type_id = 1;
                        varData.ausencias.Add(ausencia);
                        varData.absenceses_sheets_id.Add((int)ausencia.employee_id);
                    }
                }
            }
            foreach (var absence in varData.ausencias)
            {
                if (absence.applicationDateTime.Month != fechaBase.Month)
                {
                    if (!availableApplicationDates.IsNullOrEmpty())
                    {
                        absence.applicationDateTime = availableApplicationDates.Min();

                        availableApplicationDates = availableApplicationDates.Where(d => d != absence.applicationDateTime);
                    }
                    else
                    {
                        absence.applicationDateTime = fechaBase;

                    }
                    absence.application_date = DateTimeHelper.parseToBUKFormat(absence.applicationDateTime);
                }
            }

            if (varData.ausencias.IsNullOrEmpty() && !inasistenciasIntervals.IsNullOrEmpty())
            {
                foreach (var item in inasistenciasIntervals)
                {
                    DateTime fecha = DateTimeHelper.parseFromGVFormat(item.Date);
                    Employee employee = getSheetForDate(sheets, fecha);

                    if (employee != null)
                    {
                        if (!varData.absenceses_sheets_id.Contains((int)employee.id))
                        {
                            varData.absenceses_sheets_id.Add((int)employee.id);
                        }

                    }
                }
                if (varData.absenceses_sheets_id.IsNullOrEmpty())
                {
                    varData.identificador_interno_de_ellos = -1;
                }
            }

            foreach (TimeInterval intervalo in horasnotrabajadasIntervals)
            {
                DateTime fecha = DateTimeHelper.parseFromGVFormat(intervalo.Date);
                Employee employee = getSheetForDate(sheets, fecha);
                if (employee == null)
                {
                    continue;
                }
                NonWorkedHours nwh = varData.nonWorkedHoursByEmployees.FirstOrDefault(n => n.employee_id == employee.id);
                if (nwh != null)
                {
                    nwh.hours += TimeSpanHelper.HHmmToTimeSpan(intervalo.NonWorkedHours).TotalHours;
                }
                else
                {
                    nwh = new NonWorkedHours
                    {
                        employee_id = (int)employee.id,
                        year = fecha.Year,
                        month = fechaBase.Month,
                        hours = TimeSpanHelper.HHmmToTimeSpan(intervalo.NonWorkedHours).TotalHours,
                        type_id = NonWorkedHoursType.General
                    };
                    varData.nonWorkedHoursByEmployees.Add(nwh);
                }
                varData.nwh += TimeSpanHelper.HHmmToTimeSpan(intervalo.NonWorkedHours).TotalHours;
                varData.atrasos += TimeSpanHelper.HHmmToTimeSpan(intervalo.Delay).TotalHours;
                varData.adelantos += TimeSpanHelper.HHmmToTimeSpan(intervalo.EarlyLeave).TotalHours;
                varData.descuentosColacion += TimeSpanHelper.HHmmToTimeSpan(intervalo.BreakDelay).TotalHours;
            }
            foreach (TimeInterval intervalo in horasextrasIntervals)
            {
                DateTime fecha = DateTimeHelper.parseFromGVFormat(intervalo.Date);
                Employee employee = getSheetForDate(sheets, fecha);
                if (employee == null)
                {
                    continue;
                }
                foreach (KeyValuePair<string, string> reg in intervalo.AccomplishedExtraTime)
                {
                    OvertimeType tipo = tipos.FirstOrDefault(t => t.proporcion * 100 == int.Parse(reg.Key));
                    if (tipo != null)
                    {
                        Overtime ot = varData.overtimesByEmployees.FirstOrDefault(o => o.employee_id == employee.id && o.type_id == tipo.id);
                        if (ot != null)
                        {
                            ot.hours += TimeSpanHelper.HHmmToTimeSpan(reg.Value).TotalHours;
                        }
                        else
                        {
                            ot = new Overtime
                            {
                                employee_id = (int)employee.id,
                                year = fecha.Year,
                                month = fechaBase.Month,
                                hours = TimeSpanHelper.HHmmToTimeSpan(reg.Value).TotalHours,
                                type_id = tipo.id
                            };
                            varData.overtimesByEmployees.Add(ot);
                        }
                    }
                    else
                    {
                        FileLogHelper.log(LogConstants.hhee, LogConstants.get, "", "Porcentaje de Horas Extras no encontrado en BUK: (" + reg.Key + "%)", null, Empresa);
                    }

                    if (!varData.overtimes.ContainsKey(int.Parse(reg.Key)))
                    {
                        varData.overtimes[int.Parse(reg.Key)] = 0;
                    }
                    varData.overtimes[int.Parse(reg.Key)] += TimeSpanHelper.HHmmToTimeSpan(reg.Value).TotalHours;
                }
            }

            return varData;
        }

        private Employee? getSheetForDate(List<Employee> sheets, DateTime date)
        {
            var sheetsReadable = makeSheetsReadable(sheets);
            (long, DateTime?, DateTime?) candidateSheet = (0, null, null);
            foreach (var sheet in sheetsReadable)
            {
                if (sheet.Item2.HasValue && sheet.Item2.Value <= date)
                {
                    if (sheet.Item3.HasValue && sheet.Item3.Value >= date)
                    {
                        candidateSheet = sheet;
                    }
                    else if (!sheet.Item3.HasValue)
                    {
                        candidateSheet = sheet;
                    }
                }
            }
            if (candidateSheet.Item1 == 0)
            {
                return null;
            }
            return sheets.FirstOrDefault(s => s.id == candidateSheet.Item1);

        }

        private List<(long, DateTime?, DateTime?)> makeSheetsReadable(List<Employee> sheets)
        {
            Dictionary<long, (DateTime?, DateTime?)> sheetsReadableDict = new Dictionary<long, (DateTime?, DateTime?)>();

            var last_sheet = 0;
            var null_endDate = false;
            var last_startDate = new DateTime?();
            foreach (var sheet in sheets)
            {
                DateTime? startDate = new DateTime();
                DateTime? endDate = new DateTime();
                if (!string.IsNullOrWhiteSpace(sheet.active_since))
                {
                    startDate = DateTimeHelper.parseFromBUKFormat(sheet.active_since);
                }
                else
                {
                    startDate = null;
                }

                if (sheet.current_job != null && !string.IsNullOrWhiteSpace(sheet.current_job.active_until))
                {
                    endDate = DateTimeHelper.parseFromBUKFormat(sheet.current_job.active_until);
                }
                else if (sheet.current_job.contract_term != null && !string.IsNullOrWhiteSpace(sheet.current_job.contract_term))
                {
                    endDate = DateTimeHelper.parseFromBUKFormat(sheet.current_job.contract_term);
                }
                else
                {
                    endDate = null;
                }

                if (sheet.id > last_sheet && String.IsNullOrWhiteSpace(sheet.current_job.active_until))
                {
                    null_endDate = true;
                    last_sheet = (int)sheet.id;
                    last_startDate = startDate;
                }

                sheetsReadableDict[sheet.id] = (startDate, endDate);
            }

            if (null_endDate)
            {
                sheetsReadableDict[last_sheet] = (last_startDate, null);
            }
            
            List<(long, DateTime?, DateTime?)> sheetsReadable = new List<(long, DateTime?, DateTime?)>();
            foreach (KeyValuePair<long, (DateTime?, DateTime?)> sheet in sheetsReadableDict.OrderBy(sR => sR.Value.Item1))
            {
                sheetsReadable.Add((sheet.Key, sheet.Value.Item1, sheet.Value.Item2));
            }

            return sheetsReadable;
        }

        protected override Dictionary<string, string> SendData(List<Variables> variables, SesionVM Empresa, CompanyConfiguration companyConfiguration, bool hasNWHSeparadas = false)
        {
            int totalAtrasos = 0;
            int totalAdelantos = 0;
            int totalAusencias = 0;
            int totalHHEE = 0;
            int total = variables.Count;

            FileLogHelper.log(LogConstants.general, LogConstants.get, "", "ENVIANDO OPERACIONES A BUK PARA UN TOTAL DE " + total + " USUARIO(S)", null, Empresa);
            List<OvertimeType> tipos = companyConfiguration.OvertimeBusiness.GetOvertimeTypes(Empresa, companyConfiguration).OrderBy(o => o.id).ToList();
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
                    foreach (var nwh in item.nonWorkedHoursByEmployees)
                    {
                        try
                        {
                            companyConfiguration.NonWorkedHoursBusiness.AddNonWorkedHours(nwh, Empresa, companyConfiguration);
                            FileLogHelper.log(LogConstants.hnt, LogConstants.add, item.rut, "" + item.rut, nwh, Empresa);
                        }
                        catch (Exception ex)
                        {
                            InsightHelper.logException(ex, Empresa.Empresa);
                            FileLogHelper.log(LogConstants.hnt, LogConstants.error_add, "", ex.ToString(), nwh, Empresa);
                        }
                    }
                }

                totalAusencias += item.ausencias.Count;
                if (Empresa.EnviaAusencia && standardAbsenceId > 0)
                {
                    List<int> employeesAbsencesIds = item.absenceses_sheets_id.Count > 0 ? item.absenceses_sheets_id : new List<int> { item.identificador_interno_de_ellos };

                    bool do_delete = true;
                    if (item.absenceses_sheets_id.IsNullOrEmpty()) do_delete = false;

                    if (do_delete)
                    {
                        FileLogHelper.log(LogConstants.general, LogConstants.get, item.rut, "ELIMINANDO AUSENCIAS EN BUK" + " RUT: " + item.rut + " DESDE: " + DateTimeHelper.parseToBUKFormat(item.ausenciasDesde) + " HASTA: " + DateTimeHelper.parseToBUKFormat(item.ausenciasHasta), null, Empresa);
                        try
                        {
                            bool succeedElimination = companyConfiguration.AbsenceBusiness.DeleteAbsence(new AbsencesToDelete { employees_id = employeesAbsencesIds, start_date = DateTimeHelper.parseToBUKFormat(item.ausenciasDesde), end_date = DateTimeHelper.parseToBUKFormat(item.ausenciasHasta) }, Empresa, companyConfiguration);
                            if (succeedElimination)
                            {
                                FileLogHelper.log(LogConstants.general, LogConstants.get, item.rut, "AUSENCIAS ELIMINADAS EN BUK" + " RUT: " + item.rut + " DESDE: " + DateTimeHelper.parseToBUKFormat(item.ausenciasDesde) + " HASTA: " + DateTimeHelper.parseToBUKFormat(item.ausenciasHasta), null, Empresa);
                            }
                            else
                            {
                                FileLogHelper.log(LogConstants.general, LogConstants.get, item.rut, "NO SE ENCONTRARON AUSENCIAS A ELIMINAR EN BUK" + " RUT: " + item.rut + " DESDE: " + DateTimeHelper.parseToBUKFormat(item.ausenciasDesde) + " HASTA: " + DateTimeHelper.parseToBUKFormat(item.ausenciasHasta), null, Empresa);
                            }
                        }
                        catch (Exception e)
                        {
                            FileLogHelper.log(LogConstants.general, LogConstants.get, item.rut, "ERROR AL ELIMINAR AUSENCIAS A ELIMINAR EN BUK" + " RUT: " + item.rut + " DESDE: " + DateTimeHelper.parseToBUKFormat(item.ausenciasDesde) + " HASTA: " + DateTimeHelper.parseToBUKFormat(item.ausenciasHasta), null, Empresa);
                        }
                    }

                    FileLogHelper.log(LogConstants.absences, LogConstants.get, item.rut, "ENVIANDO OPERACIONES (AUSENCIAS) A BUK PARA EL USUARIO, UN TOTAL DE " + total, null, Empresa);
                    foreach (AbsenceToAdd ausencia in item.ausencias)
                    {
                        try
                        {
                            ausencia.absence_type_id = standardAbsenceId;
                            companyConfiguration.AbsenceBusiness.AddAbsence(ausencia, Empresa, companyConfiguration);
                            FileLogHelper.log(LogConstants.absences, LogConstants.add, item.rut, "", ausencia, Empresa);
                        }
                        catch (Exception ex)
                        {
                            InsightHelper.logException(ex, Empresa.Empresa);
                            FileLogHelper.log(LogConstants.absences, LogConstants.error_add, item.rut, "", ausencia, Empresa);
                        }
                    }
                }

                if (Empresa.EnviaHHEE)
                {
                    totalHHEE += item.overtimes.Count;
                    FileLogHelper.log(LogConstants.hhee, LogConstants.get, item.rut, "ENVIANDO OPERACIONES (HHEE) A BUK PARA EL USUARIO, UN TOTAL DE " + total, null, Empresa);
                    foreach (var ot in item.overtimesByEmployees)
                    {
                        try
                        {
                            companyConfiguration.OvertimeBusiness.AddOverTime(ot, Empresa, companyConfiguration);
                            FileLogHelper.log(LogConstants.hhee, LogConstants.add, item.rut, "", ot, Empresa);
                        }
                        catch (Exception ex)
                        {
                            InsightHelper.logException(ex, Empresa.Empresa);
                            FileLogHelper.log(LogConstants.hhee, LogConstants.error_add, item.rut, "", ot, Empresa);
                        }
                    }
                }
            }

            Dictionary<string, string> properties = new Dictionary<string, string>();
            properties["AddAtrasos"] = totalAtrasos.ToString();
            properties["AddAdelantos"] = totalAdelantos.ToString();
            properties["AddNonWorkedHours"] = (totalAtrasos + totalAdelantos).ToString();
            properties["AddAbsences"] = totalAusencias.ToString();
            properties["AddOvertimes"] = totalHHEE.ToString();

            return properties;
        }


        protected override List<Variables> processData(Attendance attendance, List<User> users, FechasProcesamientoVM fechas, ProcessPeriod periodo, List<UserStatusLogCalculatedVM> userStatusLogs, List<Employee> employees)
        {
            List<Variables> processed = new List<Variables>();
            var commonAttendance = attendance;
            foreach (var user in users)
            {
                var calculated = commonAttendance.Users.FirstOrDefault(u => u.Identifier == user.Identifier);
                var employee = employees.FirstOrDefault(e => user.integrationCode.Split(',').Contains(e.id.ToString()));
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

        protected override List<UserStatusLogCalculatedVM> reCalculateActivePeriodStart(List<UserStatusLogCalculatedVM> statusLogs, List<Employee> employees)
        {
            List<UserStatusLogCalculatedVM> userStatusLogs = new List<UserStatusLogCalculatedVM>();
            foreach (var statusLog in statusLogs)
            {
                List<Employee> sheets = employees.FindAll(e => String.Equals(CommonHelper.rutToGVFormat(e.rut), statusLog.Identifier, StringComparison.OrdinalIgnoreCase));
                List<ActivePeriodCalculatedVM> employeesStatusLogs = PeriodsHelper.parseEmployeesToActivePeriods(sheets);
                var mergedActivePeriods = PeriodsHelper.MergeActivePeriods(statusLog.ActivePeriods, employeesStatusLogs);
                userStatusLogs.Add(new UserStatusLogCalculatedVM { Identifier = statusLog.Identifier, ActivePeriods = mergedActivePeriods });
                //foreach (var employee in sheets)
                //{
                //    if (employee != null && !string.IsNullOrWhiteSpace(employee.active_since) && !isInActivePeriod(DateTimeHelper.parseFromBUKFormat(employee.active_since), statusLog))
                //    {
                //        var activePeriod = findNextActivePeriod(DateTimeHelper.parseFromBUKFormat(employee.active_since), statusLog.ActivePeriods);
                //        if (activePeriod != null)
                //        {
                //            UserStatusLogCalculatedVM statusLogModified = new UserStatusLogCalculatedVM();
                //            statusLogModified.Identifier = statusLog.Identifier;
                //            statusLogModified.ActivePeriods = new List<ActivePeriodCalculatedVM>();
                //            foreach (var ap in statusLog.ActivePeriods)
                //            {
                //                if (ap.Starts == activePeriod.Starts && ap.Ends == activePeriod.Ends)
                //                {
                //                    ap.Starts = DateTimeHelper.parseFromBUKFormat(employee.active_since);
                //                }
                //                statusLogModified.ActivePeriods.Add(ap);
                //            }
                //            userStatusLogs.Add(statusLogModified);
                //        }
                //        else
                //        {
                //            userStatusLogs.Add(statusLog);
                //        }
                //    }
                //}
            }

            return userStatusLogs;
        }
    }
}