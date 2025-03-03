using API.BUK.DTO;
using API.GV.DTO.Consts;
using API.Helpers.VM;
using API.Helpers.VM.Consts;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace API.Helpers.Commons
{
    public static class DateTimeHelper
    {
        public static DateTime parseFromGVFormat(string fecha)
        {
            DateTime insert = DateTime.ParseExact(fecha, "yyyyMMddHHmmss", CultureInfo.InvariantCulture);
            return insert;
        }
        public static string parseToGVFormat(DateTime fecha)
        {
            return fecha.ToString("yyyyMMddHHmmss");
        }


        public static string parseToBUKFormat(DateTime fecha)
        {
            return fecha.ToString("yyyy'-'MM'-'dd");
        }

        public static DateTime parseFromBUKFormat(string fecha, bool endDate = false)
        {
            DateTime insert = DateTime.ParseExact(fecha, "yyyy'-'MM'-'dd", CultureInfo.InvariantCulture);
            if (endDate)
            {
                insert = insert.AddDays(1).AddSeconds(-1);
            }
            return insert;
        }

        public static int differenceInMonths(DateTime end, DateTime start)
        {
            return 12 * (end.Year - start.Year) + (end.Month - start.Month);
        }

        /// <summary>
        /// Divide un rango de DateTime en sub rangos de tamaño maxItems.
        /// </summary>
        /// <param name="startDate">Fecha de inicio del rango general.</param>
        /// <param name="endDate">Fecha de fin del rango general.</param>
        /// <param name="maxItems">Tamaño máximo que deben tener los subrangos.</param>
        /// <returns>
        ///     Una lista con los subrangos
        /// </returns>
        public static List<DateTimeBucketVM> Batch(DateTime startDate, DateTime endDate,
                                                       int maxItems)
        {
            List<DateTimeBucketVM> buckets = new List<DateTimeBucketVM>();
            DateTime firstDate = startDate.Date;
            DateTime iterDate = firstDate.AddDays(maxItems);
            while (iterDate < endDate.Date)
            {
                buckets.Add(new DateTimeBucketVM { StartDate = firstDate, EndDate = iterDate, DaysCounted = maxItems });
                firstDate = iterDate.AddDays(1);
                iterDate = firstDate.AddDays(maxItems);
            }

            if (firstDate < endDate)
            {
                buckets.Add(new DateTimeBucketVM { StartDate = firstDate, EndDate = endDate, DaysCounted = (endDate - firstDate).Days });
            }

            return buckets;
        }

        public static FechasProcesamientoVM calculateProcessDate(List<PeriodConfiguration> configs, DateTime fechaBase, SesionVM Empresa)
        {
            FechasProcesamientoVM fechas = new FechasProcesamientoVM();
            if (!configs.IsNullOrEmpty())
            {
                if (!string.IsNullOrWhiteSpace(configs[0].absences_sync_start) && !string.IsNullOrWhiteSpace(configs[0].absences_sync_end))
                {
                    fechas.InasistenciasStartDate = !string.IsNullOrWhiteSpace(configs[0].absences_sync_start) ? DateTimeHelper.parseFromBUKFormat(configs[0].absences_sync_start) : default;
                    fechas.InasistenciasEndDate = DateTimeHelper.parseFromBUKFormat(configs[0].absences_sync_end);
                }
                else
                {
                    fechas.InasistenciasEndDate = calculateEndDate(fechaBase, Empresa.DesfaseInasistencias, Empresa.FechaCorte, Empresa.TipoDesfaseInasistencias);
                    fechas.InasistenciasStartDate = fechas.InasistenciasEndDate.AddMonths(-1).AddDays(1);
                    if (fechas.InasistenciasStartDate.Day <= Empresa.FechaCorte && fechas.InasistenciasStartDate.Month < fechas.InasistenciasEndDate.Month)
                    {
                        fechas.InasistenciasStartDate = fechas.InasistenciasStartDate.AddDays(Empresa.FechaCorte - fechas.InasistenciasStartDate.Day + 1);
                    }
                }

                if (!string.IsNullOrWhiteSpace(configs[0].non_worked_hours_sync_start) && !string.IsNullOrWhiteSpace(configs[0].non_worked_hours_sync_end))
                {
                    fechas.HorasNoTrabajadasStartDate = DateTimeHelper.parseFromBUKFormat(configs[0].non_worked_hours_sync_start);
                    fechas.HorasNoTrabajadasEndDate = DateTimeHelper.parseFromBUKFormat(configs[0].non_worked_hours_sync_end);
                }
                else
                {
                    fechas.HorasNoTrabajadasEndDate = calculateEndDate(fechaBase, Empresa.DesfaseHorasNoTrabajadas, Empresa.FechaCorte, Empresa.TipoDesfaseHorasNoTrabajadas);
                    fechas.HorasNoTrabajadasStartDate = fechas.HorasNoTrabajadasEndDate.AddMonths(-1).AddDays(1);
                    if (fechas.HorasNoTrabajadasStartDate.Day <= Empresa.FechaCorte && fechas.HorasNoTrabajadasStartDate.Month < fechas.HorasNoTrabajadasEndDate.Month)
                    {
                        fechas.HorasNoTrabajadasStartDate = fechas.HorasNoTrabajadasStartDate.AddDays(Empresa.FechaCorte - fechas.HorasNoTrabajadasStartDate.Day + 1);
                    }
                }

                if (!string.IsNullOrWhiteSpace(configs[0].overtime_sync_start) && !string.IsNullOrWhiteSpace(configs[0].overtime_sync_end))
                {
                    fechas.HorasExtrasStartDate = DateTimeHelper.parseFromBUKFormat(configs[0].overtime_sync_start);
                    fechas.HorasExtrasEndDate = DateTimeHelper.parseFromBUKFormat(configs[0].overtime_sync_end);
                }
                else
                {
                    fechas.HorasExtrasEndDate = calculateEndDate(fechaBase, Empresa.DesfaseHorasExtras, Empresa.FechaCorte, Empresa.TipoDesfaseHorasExtras);
                    fechas.HorasExtrasStartDate = fechas.HorasExtrasEndDate.AddMonths(-1).AddDays(1);
                    if (fechas.HorasExtrasStartDate.Day <= Empresa.FechaCorte && fechas.HorasExtrasStartDate.Month < fechas.HorasExtrasEndDate.Month)
                    {
                        fechas.HorasExtrasStartDate = fechas.HorasExtrasStartDate.AddDays(Empresa.FechaCorte - fechas.HorasExtrasStartDate.Day + 1);
                    }
                }

                if (!string.IsNullOrWhiteSpace(configs[0].permission_sync_start) && !string.IsNullOrWhiteSpace(configs[0].permission_sync_end))
                {
                    fechas.PermisosStartDate = DateTimeHelper.parseFromBUKFormat(configs[0].permission_sync_start);
                    fechas.PermisosEndDate = DateTimeHelper.parseFromBUKFormat(configs[0].permission_sync_end);
                }
                else
                {
                    int lastDay = DateTime.DaysInMonth(fechaBase.Year, fechaBase.Month);
                    int dayEndDate = (Empresa.FechaCorte > lastDay) ? lastDay : Empresa.FechaCorte;
                    DateTime endDate = new DateTime(fechaBase.Year, fechaBase.Month, dayEndDate);
                    DateTime startDate = endDate.AddMonths(-1).AddDays(1);
                    if (startDate.Day <= Empresa.FechaCorte && startDate.Month < endDate.Month)
                    {
                        startDate = startDate.AddDays(Empresa.FechaCorte - startDate.Day + 1);
                    }

                    fechas.PermisosStartDate = startDate;
                    fechas.PermisosEndDate = endDate;

                    if (endDate < DateTime.Today)
                    {
                        if (endDate.AddMonths(1) < DateTime.Today)
                        {
                            fechas.PermisosEndDate = endDate.AddMonths(1);
                        }
                        else
                        {
                            fechas.PermisosEndDate = DateTime.Today;
                        }
                    }

                }

            }
            else
            {
                fechas.InasistenciasEndDate = calculateEndDate(fechaBase, Empresa.DesfaseInasistencias, Empresa.FechaCorte, Empresa.TipoDesfaseInasistencias);
                fechas.InasistenciasStartDate = fechas.InasistenciasEndDate.AddMonths(-1).AddDays(1);
                if (fechas.InasistenciasStartDate.Day <= Empresa.FechaCorte && fechas.InasistenciasStartDate.Month < fechas.InasistenciasEndDate.Month && Empresa.TipoDesfaseInasistencias == FileReaderConsts.DelayInMonths)
                {
                    fechas.InasistenciasStartDate = fechas.InasistenciasStartDate.AddDays(Empresa.FechaCorte - fechas.InasistenciasStartDate.Day + 1);
                }
                fechas.HorasExtrasEndDate = calculateEndDate(fechaBase, Empresa.DesfaseHorasExtras, Empresa.FechaCorte, Empresa.TipoDesfaseHorasExtras);
                fechas.HorasExtrasStartDate = fechas.HorasExtrasEndDate.AddMonths(-1).AddDays(1);
                if (fechas.HorasExtrasStartDate.Day <= Empresa.FechaCorte && fechas.HorasExtrasStartDate.Month < fechas.HorasExtrasEndDate.Month && Empresa.TipoDesfaseHorasExtras == FileReaderConsts.DelayInMonths)
                {
                    fechas.HorasExtrasStartDate = fechas.HorasExtrasStartDate.AddDays(Empresa.FechaCorte - fechas.HorasExtrasStartDate.Day + 1);
                }
                fechas.HorasNoTrabajadasEndDate = calculateEndDate(fechaBase, Empresa.DesfaseHorasNoTrabajadas, Empresa.FechaCorte, Empresa.TipoDesfaseHorasNoTrabajadas);
                fechas.HorasNoTrabajadasStartDate = fechas.HorasNoTrabajadasEndDate.AddMonths(-1).AddDays(1);
                if (fechas.HorasNoTrabajadasStartDate.Day <= Empresa.FechaCorte && fechas.HorasNoTrabajadasStartDate.Month < fechas.HorasNoTrabajadasEndDate.Month && Empresa.TipoDesfaseHorasNoTrabajadas == FileReaderConsts.DelayInMonths)
                {
                    fechas.HorasNoTrabajadasStartDate = fechas.HorasNoTrabajadasStartDate.AddDays(Empresa.FechaCorte - fechas.HorasNoTrabajadasStartDate.Day + 1);
                }
                // Permisos
                int lastDay = DateTime.DaysInMonth(fechaBase.Year, fechaBase.Month);
                int dayEndDate = (Empresa.FechaCorte > lastDay) ? lastDay : Empresa.FechaCorte;
                DateTime endDate = new DateTime(fechaBase.Year, fechaBase.Month, dayEndDate);
                DateTime startDate = endDate.AddMonths(-1).AddDays(1);
                if (startDate.Day <= Empresa.FechaCorte && startDate.Month < endDate.Month)
                {
                    startDate = startDate.AddDays(Empresa.FechaCorte - startDate.Day + 1);
                }

                fechas.PermisosStartDate = startDate;
                fechas.PermisosEndDate = endDate;

                if (endDate < DateTime.Today)
                {
                    if (endDate.AddMonths(1) < DateTime.Today)
                    {
                        fechas.PermisosEndDate = endDate.AddMonths(1);
                    }
                    else
                    {
                        fechas.PermisosEndDate = DateTime.Today;
                    }
                }
            }
            return fechas;
        }

        public static DateTime calculateEndDate(DateTime fechaBase, int desfase, int fechaCorte, string tipoDesfase)
        {
            DateTime fechaBaseModificada = new DateTime();
            if (tipoDesfase == FileReaderConsts.DelayInDays)
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

        public static bool IsInTimeSpan(DateTime date, DateTime inferiorLimit, DateTime superiorLimit)
        {
            return (date >= inferiorLimit && date <= superiorLimit);
        }

        public static IEnumerable<DateTime> AllDatesInMonth(int year, int month)
        {
            int days = DateTime.DaysInMonth(year, month);
            for (int day = 1; day <= days; day++)
            {
                yield return new DateTime(year, month, day);
            }
        }
        ///<summary>
        /// Método que valida si el permiso corresponde a un día viernes
        /// </summary>
        /// <param name="permission"></param>
        /// <returns></returns>
        public static bool IsFridayTimeOff(Permission permission)
        {
            var start = DateTime.ParseExact(permission.start_date, "yyyy-MM-dd", CultureInfo.InvariantCulture);
            var end = DateTime.ParseExact(permission.end_date, "yyyy-MM-dd", CultureInfo.InvariantCulture);

            return start == end && start.DayOfWeek == DayOfWeek.Friday;
        }

        /// <summary>
        /// Valida y corrige la fecha para que sea el periodo de un mes exacto, en caso de que la fecha de corte sea menor al último día del mes
        /// utiliza el día de la fecha de corte, en caso contrario, utiliza el total de días del mes
        /// </summary>
        /// <param name="fecha"></param>
        /// <param name="fechaCorte"></param>
        /// <param name="lastDay"></param>
        /// <returns></returns>
        private static DateTime FixPeriodDate(DateTime fecha, DateTime fechaCorte, bool lastDay = false)
        {
            if (fechaCorte.Day < GetDaysOfMonth(fechaCorte))
            {
                if (lastDay)
                {
                    fecha = new DateTime(fechaCorte.Year, fechaCorte.Month, fechaCorte.Day);
                }
                else
                {
                    fecha = new DateTime(fechaCorte.Year, fechaCorte.Month - 1, fechaCorte.Day + 1);
                }
            }
            else
            {
                if (fecha.Month < fechaCorte.Month || fecha.Month > fechaCorte.Month)
                {
                    if (lastDay)
                    {
                        fecha = new DateTime(fechaCorte.Year, fechaCorte.Month, GetDaysOfMonth(fechaCorte));
                    }
                    else
                    {
                        fecha = new DateTime(fechaCorte.Year, fechaCorte.Month, 1);
                    }
                }
            }

            return fecha;
        }
        /// <summary>
        /// Obtiene el total de días que tiene el mes
        /// </summary>
        /// <param name="fecha"></param>
        /// <returns></returns>
        private static int GetDaysOfMonth(DateTime fecha)
        {
            return DateTime.DaysInMonth(fecha.Year, fecha.Month);
        }
        /// <summary>
        /// Corrije las fechas de las HHEE, HNT, Permisos y Ausencias, en caso de que la empresa solo quiera sincronizar el mes abierto
        /// (lo que comprende desde el primer día del mes hasta el último)
        /// debido a que la configuración anterior, las HHEE y HNT eran las del periodo anterior cerrado
        /// y los Permisos eran desde el inicio del perido abierto hasta el día que se realiza la sincronización
        /// </summary>
        /// <param name="oldProcessDate"></param>
        /// <param name="initialDate"></param>
        /// <param name="cutOffDay"></param>
        /// <returns></returns>
        public static FechasProcesamientoVM FixProcessDate(FechasProcesamientoVM oldProcessDate, DateTime initialDate, int cutOffDay)
        {
            int cutOffDayFixed = cutOffDay >= GetDaysOfMonth(initialDate) ? GetDaysOfMonth(initialDate) : cutOffDay;
            DateTime cutOffDate = new DateTime(initialDate.Year, initialDate.Month, cutOffDayFixed);
            oldProcessDate.HorasNoTrabajadasStartDate = FixPeriodDate(oldProcessDate.HorasNoTrabajadasStartDate, cutOffDate);
            oldProcessDate.HorasNoTrabajadasEndDate = FixPeriodDate(oldProcessDate.HorasNoTrabajadasEndDate, cutOffDate, true);
            oldProcessDate.InasistenciasStartDate = FixPeriodDate(oldProcessDate.HorasNoTrabajadasStartDate, cutOffDate);
            oldProcessDate.InasistenciasEndDate = FixPeriodDate(oldProcessDate.HorasNoTrabajadasEndDate, cutOffDate, true);
            oldProcessDate.HorasExtrasStartDate = FixPeriodDate(oldProcessDate.HorasExtrasStartDate, cutOffDate);
            oldProcessDate.HorasExtrasEndDate = FixPeriodDate(oldProcessDate.HorasExtrasEndDate, cutOffDate, true);
            oldProcessDate.PermisosStartDate = FixPeriodDate(oldProcessDate.PermisosStartDate, cutOffDate);
            oldProcessDate.PermisosEndDate = FixPeriodDate(oldProcessDate.PermisosEndDate, cutOffDate, true);
            return oldProcessDate;
        }

        public static (DateTime, DateTime) calculateGroupDates(DateTime startDate, DateTime endDate, string grupo, int grupo2StartDay, int grupo2EndDay)
        {
            if(grupo == GestionIntegralGroups.GESTION_GROUP1)
            {
                startDate = DateTime.Today.AddDays(-DateTime.Now.Day + 1);
                endDate = startDate.AddDays(29);
            }
            else if (grupo == GestionIntegralGroups.GESTION_GROUP2)
            {
                startDate = DateTime.Today.AddMonths(-1).AddDays(-DateTime.Now.Day + grupo2StartDay);
                endDate = DateTime.Today.AddDays(-DateTime.Now.Day + grupo2EndDay);
            }

            return (startDate, endDate);
        }
    }
}
