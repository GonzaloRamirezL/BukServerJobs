using API.BUK.DTO;
using API.BUK.DTO.Consts;
using API.GV.DAO;
using API.GV.DTO;
using API.GV.DTO.Filters;
using API.GV.DTO.Personalizados.TEISA;
using API.Helpers.Commons;
using API.Helpers.VM;
using API.Helpers.VM.Consts;
using BusinessLogic.Interfaces;
using BusinessLogic.Interfaces.VM;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BusinessLogic.Implementation
{
    public class UserTeisaBusiness : UserBusiness, IUserBusiness
    {
        private const int PAGE_SIZE = 100;
        
        public override void Sync(SesionVM company, CompanyConfiguration companyConfiguration, ProcessPeriod period)
        {
            FileLogHelper.log(LogConstants.period, LogConstants.get, "", string.Empty, period, company);
            Console.WriteLine("PROCESANDO PERIODO: " + period.month);

            DateTime startMetric = DateTime.Now;
            Dictionary<string, string> properties = new Dictionary<string, string>();

            #region Fechas
            DateTime fechaBase = DateTimeHelper.parseFromBUKFormat(period.month);
            int lastDay = DateTime.DaysInMonth(fechaBase.Year, fechaBase.Month);
            int dayEndDate = (company.FechaCorte > lastDay) ? lastDay : company.FechaCorte;
            DateTime endDate = new DateTime(fechaBase.Year, fechaBase.Month, dayEndDate);
            DateTime startDate = endDate.AddMonths(-1).AddDays(1);

            if (startDate.Day <= company.FechaCorte && startDate.Month < endDate.Month)
            {
                startDate = startDate.AddDays(company.FechaCorte - startDate.Day + 1);
            }

            properties["EMPRESA"] = company.Empresa;
            properties["startDate"] = DateTimeHelper.parseToGVFormat(startDate);
            properties["endDate"] = DateTimeHelper.parseToGVFormat(endDate);
            #endregion

            FileLogHelper.log(LogConstants.general, LogConstants.get, "", "PIDIENDO EMPLEADOS A BUK", null, company);
            List<Employee> employees = companyConfiguration.EmployeeBusiness.GetEmployeesForSync(company, companyConfiguration, startDate, endDate);
            properties["EMPRESA"] = company.Empresa;
            properties["GetEmployees"] = employees.Count.ToString();

            FileLogHelper.log(LogConstants.general, LogConstants.get, "", "PIDIENDO USUARIOS A GV", null, company);
            var persons = GetUsersForSync(company, companyConfiguration, employees, Operacion.USUARIOS);
            List<User> users = persons.Item1;
            employees = persons.Item2;
            properties["GetUsers"] = users.Count.ToString();

            FileLogHelper.log(LogConstants.general, LogConstants.get, "", "PROCESANDO OPERACIONES A REALIZAR CON USUARIOS", null, company);
            var process = ProcessUsers(employees, users, company);
            properties["AddUsers"] = process.toAdd.Count.ToString();
            properties["DisableUsers"] = process.toDeactivate.Count.ToString();
            properties["EnableUsers"] = process.toActivate.Count.ToString();
            properties["EditUsers"] = process.toEdit.Count.ToString();

            FileLogHelper.log(LogConstants.general, LogConstants.get, "", "AÑADIENDO USUARIOS A GV", null, company);
            AddUsers(process.toAdd, company, companyConfiguration);

            FileLogHelper.log(LogConstants.general, LogConstants.get, "", "DESACTIVANDO USUARIOS EN GV", null, company);
            DeactivateUsers(process.toDeactivate, company, companyConfiguration);

            FileLogHelper.log(LogConstants.general, LogConstants.get, "", "ACTIVANDO USUARIOS EN GV", null, company);
            ActivateUsers(process.toActivate, company, companyConfiguration);

            FileLogHelper.log(LogConstants.general, LogConstants.get, "", "EDITANDO USUARIOS EN GV", null, company);
            EditUsers(process.toEdit, company, companyConfiguration);

            #region KPIS de Usuarios
            DateTime today = DateTime.Today;
            bool isSpecialMonth = (fechaBase.Month == TeisaDates.February || fechaBase.Month == TeisaDates.September);
            if ((isSpecialMonth && today.Day == TeisaDates.KpiSpecialMonthExecutionDay) || (!isSpecialMonth && today.Day == TeisaDates.KpiExecutionDay))
            {
                DateTime kpiStartDate = new DateTime(fechaBase.AddMonths(-1).Year, fechaBase.AddMonths(-1).Month, TeisaDates.CustomReportStartDay).Date;
                DateTime kpiEndDate = new DateTime(fechaBase.Year, fechaBase.Month, isSpecialMonth ? TeisaDates.CustomReportSpecialMonthEndDay : TeisaDates.CustomReportEndDay).AddDays(1).AddSeconds(-1);
                FileLogHelper.log(LogConstants.general, LogConstants.get, "", "PROCESANDO OPERACIONES A REALIZAR CON LOS KPIS de LOS USUARIOS", null, company);
                KpiProcessVM kpiProcess = ProcessUsersKpis(kpiStartDate, kpiEndDate, employees, company, companyConfiguration);

                FileLogHelper.log(LogConstants.general, LogConstants.get, "", "AÑADIENDO NUEVOS DATOS DE KPIS DE USUARIOS A BUK", null, company);
                companyConfiguration.KpiBusiness.AddKpiData(kpiProcess.toAdd, company, companyConfiguration);

                FileLogHelper.log(LogConstants.general, LogConstants.get, "", "EDITANDO DATOS DE KPIS DE USUARIOS EN BUK", null, company);
                companyConfiguration.KpiBusiness.UpdateKpiData(kpiProcess.toEdit, company, companyConfiguration);
            }            
            #endregion

            InsightHelper.logMetric("SyncUsers", startMetric - DateTime.Now, properties);
        }

        /// <summary>
        /// Genera los datos de KPIs a insertar o actualizar para todos los empleados
        /// </summary>
        /// <param name="startDate"></param>
        /// <param name="endDate"></param>
        /// <param name="employees"></param>
        /// <param name="company"></param>
        /// <param name="companyConfiguration"></param>
        /// <returns>
        ///     Objeto con los datos de KPIs a insertar y actualizar
        /// </returns>
        private KpiProcessVM ProcessUsersKpis(DateTime startDate, DateTime endDate, List<Employee> employees, SesionVM company, CompanyConfiguration companyConfiguration)
        {
            KpiProcessVM result = new KpiProcessVM { toAdd = new List<KpiData>(), toEdit = new List<KpiData>() };

            //Se obtienen los usuarios de GV actualizados
            FileLogHelper.log(LogConstants.general, LogConstants.get, "", "PIDIENDO USUARIOS ACTUALIZADOS A GV", null, company);
            List<User> users = GetUsers(company, companyConfiguration);

            //Se obtienen los tipos de Kpi relacionados con empleados desde BUK
            FileLogHelper.log(LogConstants.general, LogConstants.get, "", "PIDIENDO LOS TIPOS DE KPI A BUK", null, company);
            List<KpiType> kpiTypes = companyConfiguration.KpiBusiness.GetKpiTypes(KpiTypeRelatedTo.Employee, company, companyConfiguration);

            //Se obtienen los datos de KPIs desde BUK
            FileLogHelper.log(LogConstants.general, LogConstants.get, "", "PIDIENDO LOS DATOS DE KPI A BUK", null, company);
            List<KpiData> kpis = companyConfiguration.KpiBusiness.Get(company, companyConfiguration);

            //Se obtiene el reporte a medida desde GV para los usuarios especificados
            FileLogHelper.log(LogConstants.general, LogConstants.get, "", "PIDIENDO EL REPORTE A MEDIDA GENERAL DE TEISA A GV", null, company);
            TeisaDataObject customReport = GetGeneralReport(startDate, endDate, users, company);

            foreach (var employee in employees.Where(e => e.status == EmployeeStatus.Activo))
            {
                var position = employee.current_job?.role?.code ?? String.Empty;
                User user = users.FirstOrDefault(u => (u.integrationCode != null && long.Parse(u.integrationCode) == employee.id) || (u.Identifier != null && (String.Equals(CommonHelper.rutToGVFormat(employee.rut), u.Identifier, StringComparison.OrdinalIgnoreCase))));
                if (user != null && !string.IsNullOrWhiteSpace(user.userCompanyIdentifier))
                {
                    var userKpis = kpis.Where(x => x.employee_id.HasValue && x.employee_id == employee.id);
                    var userReport = customReport.Usuarios.FirstOrDefault(x => !string.IsNullOrWhiteSpace(x.Rut) && String.Equals(CommonHelper.rutToGVFormat(x.Rut), user.Identifier, StringComparison.OrdinalIgnoreCase));
                    if (userReport != null)
                    {
                        List<string> kpiTypeCodes = new List<string> { TeisaKpiCodes.LunchDays, TeisaKpiCodes.MobilizationDays, TeisaKpiCodes.AirportMobilizationDays, TeisaKpiCodes.ManagersMobilizationDays, TeisaKpiCodes.HomeOfficeDays, TeisaKpiCodes.Overtime100 };
                        bool isManagerOrChief = false;
                        switch (CommonHelper.rutToGVFormat(user.userCompanyIdentifier))
                        {
                            case TeisaTradeNameIdentifier.Teisa: //Reporte General a Medida TEISA
                                isManagerOrChief = (position == TeisaPositionCode.SalesChief || position == TeisaPositionCode.OperationsChief || position.Contains(TeisaPositionCode.Manager, StringComparison.InvariantCultureIgnoreCase));
                                kpiTypeCodes.AddRange(new List<string>
                                {
                                    TeisaKpiCodes.ExtensionLunchDays,
                                    TeisaKpiCodes.CallShiftDays,
                                    TeisaKpiCodes.Overtime50,
                                    TeisaKpiCodes.NightBonus
                                });
                                break;                            
                            case TeisaTradeNameIdentifier.AirlineServicesAndLogistics: //Reporte General a Medida UASL
                                isManagerOrChief = (position == TeisaPositionCode.SalesChief || position == TeisaPositionCode.AtlasAccountChief || position == TeisaPositionCode.SecurityChief || position == TeisaPositionCode.OperationsChief);
                                kpiTypeCodes.AddRange(new List<string>
                                {
                                    TeisaKpiCodes.DinnerDays,
                                    TeisaKpiCodes.Overtime50,
                                    TeisaKpiCodes.NightBonus,
                                });
                                break;
                            case TeisaTradeNameIdentifier.AirLogisticsInvestments: //Reporte General a Medida ILASA
                                isManagerOrChief = (position == TeisaPositionCode.TiProyectsChief || position == TeisaPositionCode.TalentCultureChief || position == TeisaPositionCode.AccountantChief || position == TeisaPositionCode.ManagementControlChief || position.Contains(TeisaPositionCode.Manager, StringComparison.InvariantCultureIgnoreCase));
                                kpiTypeCodes.Add(TeisaKpiCodes.Overtime50);
                                break;
                            case TeisaTradeNameIdentifier.Depocargo: //Reporte General a Medida DEPOCARGO
                                isManagerOrChief = (position == TeisaPositionCode.SalesChief || position == TeisaPositionCode.AtlasAccountChief || position == TeisaPositionCode.SecurityChief || position == TeisaPositionCode.OperationsChief || position.Contains(TeisaPositionCode.Manager, StringComparison.InvariantCultureIgnoreCase));
                                kpiTypeCodes.AddRange(new List<string>
                                {
                                    TeisaKpiCodes.DinnerDays,
                                    TeisaKpiCodes.Overtime75,
                                    TeisaKpiCodes.NightBonus,
                                });
                                break;
                            default:
                                continue;
                        }

                        KpiProcessVM userKpisToProcess = ProcessKpi(kpiTypeCodes, kpiTypes, employee, userKpis, userReport, isManagerOrChief);
                        if (userKpisToProcess.toAdd.Count > 0)
                        {
                            result.toAdd.AddRange(userKpisToProcess.toAdd);
                        }
                        if (userKpisToProcess.toEdit.Count > 0)
                        {
                            result.toEdit.AddRange(userKpisToProcess.toEdit);
                        }
                    }                    
                }
            }

            return result;
        }

        /// <summary>
        /// Genera los datos de KPIs a insertar o actualizar para un empleado comparando los datos de KPI de BUK con el reporte a medida de TEISA
        /// </summary>
        /// <param name="kpiCodes"></param>
        /// <param name="kpiTypes"></param>
        /// <param name="employee"></param>
        /// <param name="userKpis"></param>
        /// <param name="userReport"></param>
        /// <returns>
        ///     Objeto con los datos de KPIs a insertar y actualizar para un empleado
        /// </returns>
        private KpiProcessVM ProcessKpi(List<string> kpiCodes, List<KpiType> kpiTypes, Employee employee, IEnumerable<KpiData> userKpis, TeisaUserGeneralReport userReport, bool isManagerOrChief)
        {
            KpiProcessVM result = new KpiProcessVM { toAdd = new List<KpiData>(), toEdit = new List<KpiData>() };

            foreach (var kpiCode in kpiCodes)
            {
                decimal value = 0;
                switch (kpiCode)
                {
                    case TeisaKpiCodes.LunchDays:
                        value = userReport.Colacion;
                        break;
                    case TeisaKpiCodes.ExtensionLunchDays:
                        value = userReport.ColacionAlargue;
                        break;
                    case TeisaKpiCodes.DinnerDays:
                        value = userReport.Cena;
                        break;
                    case TeisaKpiCodes.MobilizationDays:
                        value = userReport.Movilizacion;
                        break;
                    case TeisaKpiCodes.AirportMobilizationDays:
                        value = userReport.Movilizacion;
                        break;
                    case TeisaKpiCodes.ManagersMobilizationDays:
                        if (isManagerOrChief)
                        {
                            value = userReport.Movilizacion;
                        }                        
                        break;
                    case TeisaKpiCodes.HomeOfficeDays:
                        value = userReport.Teletrabajo;
                        break;
                    case TeisaKpiCodes.CallShiftDays:
                        value = userReport.TurnoLlamado;
                        break;
                    case TeisaKpiCodes.NightBonus:
                        value = userReport.BonoNocturno;
                        break;
                    case TeisaKpiCodes.Overtime50:
                        value = (decimal)TimeSpanHelper.TimeSpanToDouble(userReport.TotalHHEE50, false);
                        break;
                    case TeisaKpiCodes.Overtime75:
                        value = (decimal)TimeSpanHelper.TimeSpanToDouble(userReport.TotalHHEE75, false);
                        break;
                    case TeisaKpiCodes.Overtime100:
                        value = (decimal)TimeSpanHelper.TimeSpanToDouble(userReport.TotalHHEE100, false);
                        break;
                    default:
                        break;
                }

                int? kpiId = kpiTypes.FirstOrDefault(x => x.code == kpiCode)?.id;
                if (kpiId.HasValue && (kpiCode != TeisaKpiCodes.ManagersMobilizationDays || isManagerOrChief))
                {
                    KpiData kpi = userKpis?.OrderByDescending(x => x.id).FirstOrDefault(x => x.kpi_id == kpiId);
                    if (kpi == null)
                    {
                        result.toAdd.Add(new KpiData
                        {
                            kpi_id = kpiId.Value,
                            employee_id = (int)employee.id,
                            value = value
                        });
                    }
                    else if (kpi.value != value)
                    {
                        kpi.value = value;
                        result.toEdit.Add(kpi);
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Obtiene el reporte a medida general de Teisa
        /// </summary>
        /// <param name="startDate"></param>
        /// <param name="endDate"></param>
        /// <param name="users"></param>
        /// <param name="company"></param>
        /// <returns>
        ///     Objeto con los datos del reporte a medida de Teisa
        /// </returns>
        private TeisaDataObject GetGeneralReport(DateTime startDate, DateTime endDate, List<User> users, SesionVM company)
        {
            TeisaDataObject customReport = new TeisaDataObject { Usuarios = new List<TeisaUserGeneralReport>() };

            IEnumerable<string> userIdentifiers = users.Where(x => !String.IsNullOrWhiteSpace(x.Identifier) && x.Enabled == 1).Select(x => x.Identifier).Distinct();
            int usersCount = userIdentifiers.Count();
            int pages = usersCount / PAGE_SIZE;
            if (usersCount % PAGE_SIZE != 0)
            {
                pages++;
            }
            for (int i = 0; i < pages; i++)
            {
                IEnumerable<string> pagedUserIdentifiers = userIdentifiers.Skip(i * PAGE_SIZE).Take(PAGE_SIZE);
                TeisaDataObject pagedCustomReport = new CustomReportDAO<TeisaDataObject>().Get(new CustomReportFilter
                {
                    StartDate = DateTimeHelper.parseToGVFormat(startDate),
                    EndDate = DateTimeHelper.parseToGVFormat(endDate),
                    ReportUrl = "PersonalizadoUltramar/GetGeneral",
                    UserIds = String.Join(",", pagedUserIdentifiers)
                }, company);

                if (pagedCustomReport != null && pagedCustomReport.Usuarios != null && pagedCustomReport.Usuarios.Count > 0)
                {
                    customReport.Usuarios.AddRange(pagedCustomReport.Usuarios);
                }
            }

            return customReport;
        }
    }
}
