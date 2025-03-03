using API.BUK.DAO;
using API.BUK.DAO.Paises.Peru;
using API.GV.DAO;
using API.Helpers.VM;
using API.Helpers.VM.Consts;
using BusinessLogic.Implementation;
using BusinessLogic.Implementation.Paises.Colombia;
using BusinessLogic.Implementation.Paises.Peru;
using BusinessLogic.Interfaces.VM;

namespace ModuleBuilder
{
    public static class CompanyBuilder
    {
        public static CompanyConfiguration GetCompanyConfiguration(SesionVM sesionVM)
        {
            string empresa = sesionVM.Empresa.Split('-')[0];
            if (empresa == SpecialCompany.LOGYTECHCO)
            {
                return getUsersDirectColombiaCompanyConfiguration(sesionVM);
            }
            else if (empresa == SpecialCompany.CENTRALSERVICING)
            {
                return getCompanyConfigurationForCentralServicing(sesionVM);
            }
            else if (sesionVM.Pais == CountryPreffix.COLOMBIA)
            {
                return getColombiaCompanyConfiguration(sesionVM);
            }
            else if (sesionVM.Pais == CountryPreffix.PERU)
            {
                return getPeruCompanyConfiguration(sesionVM);
            }
            else if (empresa == SpecialCompany.CABILFRUT || empresa == SpecialCompany.FOURTHANE)
            {
                return getMultipleSheetCompanyConfiguration(sesionVM);
            }
            else if (empresa == SpecialCompany.DemoGV || empresa == SpecialCompany.Sausalito || empresa == SpecialCompany.Seducete || empresa == SpecialCompany.NailOutlet || empresa == SpecialCompany.KDMEnergia || empresa == SpecialCompany.KDMRESAM || empresa == SpecialCompany.KDMTratamiento)
            {
                return getCompanyConfigurationByCompaniesSubSet(sesionVM);
            }
            else if (empresa == SpecialCompany.REDBANC)
            {
                return getFridayHalfDayCompanyConfiguration(sesionVM);
            }
            else if (empresa == SpecialCompany.TEISA)
            {
                return getTEISACompanyConfiguration(sesionVM);
            }
            else if (empresa.Contains(SpecialCompany.PICHARA))
            {
                return getPicharaCompanyConfiguration(sesionVM);
            }
            else if (empresa == SpecialCompany.Aconcagua)
            {
                return getAconcaguaCompanyConfiguration(sesionVM);
            }
            else if (empresa == SpecialCompany.CASAIDEAS)
            {
                return getCasaIdeasCompanyConfiguration(sesionVM);
            }
            else if (empresa == SpecialCompany.GESTIONINTEGRAL)
            {
                return getGestionIntegralCompanyConfiguration(sesionVM);
            }
            else
            {
                return getStandardCompanyConfiguration(sesionVM);
            }

        }

        private static CompanyConfiguration getStandardCompanyConfiguration(SesionVM sesionVM)
        {
            return new CompanyConfiguration
            {
                AbsenceBusiness = new AbsenceBusiness(),
                AbsenceDAO = new AbsenceDAO(),
                AttendanceBusiness = new AttendanceBusiness(),
                AttendanceDAO = new AttendanceDAO(),
                BUKDAO = new BUKDAO(),
                EmployeeBusiness = new EmployeeArt22Business(),
                EmployeeDAO = new EmployeeDAO(),
                LicenceBusiness = new LicenceBusiness(),
                LicenceDAO = new LicenceDAO(),
                NonWorkedHoursBusiness = new NonWorkedHoursBusiness(),
                NonWorkedHoursDAO = new NonWorkedHoursDAO(),
                OvertimeBusiness = new OvertimeBusiness(),
                OvertimeDAO = new OvertimeDAO(),
                PermissionBusiness = new PermissionBusiness(),
                PermissionDAO = new PermissionDAO(),
                ProcessPeriodsBusiness = new ProcessPeriodsBusiness(),
                ProcessPeriodsDAO = new ProcessPeriodsDAO(),
                SuspensionBusiness = new SuspensionBusiness(),
                SuspensionDAO = new SuspensionDAO(),
                TimeOffBusiness = new TimeOffBusiness(),
                TimeOffDAO = new TimeOffDAO(),
                UserBusiness = new UserArt22Business(),
                UserDAO = new UserDAO(),
                UserStatusLogBusiness = new UserStatusLogBusiness(),
                UserStatusLogDAO = new UserStatusLogDAO(),
                VacationBusiness = new VacationBusiness(),
                VacationDAO = new VacationDAO(),
                CompanyBusiness = new CompanyBusiness(),
                CompanyDAO = new CompanyDAO(),
                KpiBusiness = new KpiBusiness(),
                KpiDAO = new KpiDAO(),
                GroupBusiness = new GroupBusiness()
            };
        }

        private static CompanyConfiguration getCompanyConfigurationForCentralServicing(SesionVM sesionVM)
        {
            return new CompanyConfiguration
            {
                AbsenceBusiness = new AbsenceBusiness(),
                AbsenceDAO = new AbsenceDAO(),
                AttendanceBusiness = new AttendanceBusinessCentralServicing(),
                AttendanceDAO = new AttendanceDAO(),
                BUKDAO = new BUKDAO(),
                EmployeeBusiness = new EmployeeArt22Business(),
                EmployeeDAO = new EmployeeDAO(),
                ItemBusiness = new ItemBusiness(),
                ItemDAO = new ItemDAO(),
                LicenceBusiness = new LicenceBusiness(),
                LicenceDAO = new LicenceDAO(),
                NonWorkedHoursBusiness = new NonWorkedHoursBusiness(),
                NonWorkedHoursDAO = new NonWorkedHoursDAO(),
                OvertimeBusiness = new OvertimeBusiness(),
                OvertimeDAO = new OvertimeDAO(),
                PermissionBusiness = new PermissionBusiness(),
                PermissionDAO = new PermissionDAO(),
                ProcessPeriodsBusiness = new ProcessPeriodsBusiness(),
                ProcessPeriodsDAO = new ProcessPeriodsDAO(),
                SuspensionBusiness = new SuspensionBusiness(),
                SuspensionDAO = new SuspensionDAO(),
                TimeOffBusiness = new TimeOffBusinessCentralServicing(),
                TimeOffDAO = new TimeOffDAO(),
                UserBusiness = new UserArt22Business(),
                UserDAO = new UserDAO(),
                UserStatusLogBusiness = new UserStatusLogBusiness(),
                UserStatusLogDAO = new UserStatusLogDAO(),
                VacationBusiness = new VacationBusiness(),
                VacationDAO = new VacationDAO(),
                CompanyBusiness = new CompanyBusiness(),
                CompanyDAO = new CompanyDAO(),
                GroupBusiness = new GroupBusiness()
            };
        }

        #region RazonesSociales
        private static CompanyConfiguration getCompanyConfigurationByCompaniesSubSet(SesionVM sesionVM)
        {
            return new CompanyConfiguration
            {
                AbsenceBusiness = new AbsenceBusiness(),
                AbsenceDAO = new AbsenceDAO(),
                AttendanceBusiness = new AttendanceBusiness(),
                AttendanceDAO = new AttendanceDAO(),
                BUKDAO = new BUKDAO(),
                EmployeeBusiness = new EmployeeArt22Business(),
                EmployeeDAO = new EmployeeDAO(),
                LicenceBusiness = new LicenceBusiness(),
                LicenceDAO = new LicenceDAO(),
                NonWorkedHoursBusiness = new NonWorkedHoursBusiness(),
                NonWorkedHoursDAO = new NonWorkedHoursDAO(),
                OvertimeBusiness = new OvertimeBusiness(),
                OvertimeDAO = new OvertimeDAO(),
                PermissionBusiness = new PermissionBusiness(),
                PermissionDAO = new PermissionDAO(),
                ProcessPeriodsBusiness = new ProcessPeriodsBusiness(),
                ProcessPeriodsDAO = new ProcessPeriodsDAO(),
                SuspensionBusiness = new SuspensionBusiness(),
                SuspensionDAO = new SuspensionDAO(),
                TimeOffBusiness = new TimeOffBusiness(),
                TimeOffDAO = new TimeOffDAO(),
                UserBusiness = new UserByCompaniesBusiness(),
                UserDAO = new UserDAO(),
                UserStatusLogBusiness = new UserStatusLogBusiness(),
                UserStatusLogDAO = new UserStatusLogDAO(),
                VacationBusiness = new VacationBusiness(),
                VacationDAO = new VacationDAO(),
                CompanyBusiness = new CompanyBusiness(),
                CompanyDAO = new CompanyDAO(),
                KpiBusiness = new KpiBusiness(),
                KpiDAO = new KpiDAO(),
                GroupBusiness = new GroupBusiness()
            };
        }
        #endregion

        #region Colombia
        private static CompanyConfiguration getColombiaCompanyConfiguration(SesionVM sesionVM)
        {
            return new CompanyConfiguration
            {
                AbsenceBusiness = new AbsenceBusiness(),
                AbsenceDAO = new AbsenceDAO(),
                AttendanceBusiness = new AttendanceColombiaBusiness(),
                AttendanceDAO = new AttendanceColombiaDAO(),
                BUKDAO = new BUKDAO(),
                EmployeeBusiness = new EmployeeArt22Business(),
                EmployeeDAO = new EmployeeDAO(),
                LicenceBusiness = new LicenceBusiness(),
                LicenceDAO = new LicenceDAO(),
                NonWorkedHoursBusiness = new NonWorkedHoursBusiness(),
                NonWorkedHoursDAO = new NonWorkedHoursDAO(),
                OvertimeBusiness = new OvertimeBusiness(),
                OvertimeDAO = new OvertimeDAO(),
                PermissionBusiness = new PermissionBusiness(),
                PermissionDAO = new PermissionDAO(),
                ProcessPeriodsBusiness = new ProcessPeriodsBusiness(),
                ProcessPeriodsDAO = new ProcessPeriodsDAO(),
                SuspensionBusiness = new SuspensionBusiness(),
                SuspensionDAO = new SuspensionDAO(),
                TimeOffBusiness = new TimeOffColombiaBusiness(),
                TimeOffDAO = new TimeOffDAO(),
                UserBusiness = new UserArt22Business(),
                UserDAO = new UserDAO(),
                UserStatusLogBusiness = new UserStatusLogBusiness(),
                UserStatusLogDAO = new UserStatusLogDAO(),
                VacationBusiness = new VacationBusiness(),
                VacationDAO = new VacationDAO(),
                CompanyBusiness = new CompanyBusiness(),
                CompanyDAO = new CompanyDAO(),
                KpiBusiness = new KpiBusiness(),
                KpiDAO = new KpiDAO(),
                GroupBusiness = new GroupBusiness()
            };
        }

        #endregion

        #region Perú
        private static CompanyConfiguration getPeruCompanyConfiguration(SesionVM sesionVM)
        {
            return new CompanyConfiguration
            {
                AbsenceBusiness = new AbsenceBusiness(),
                AbsenceDAO = new AbsenceDAO(),
                AttendanceBusiness = new AttendanceBusiness(),
                AttendanceDAO = new AttendanceDAO(),
                BUKDAO = new BUKDAO(),
                EmployeeBusiness = new EmployeeArt22Business(),
                EmployeeDAO = new EmployeeDAO(),
                LicenceBusiness = new LicenceBusiness(),
                LicenceDAO = new LicenceDAO(),
                NonWorkedHoursBusiness = new NonWorkedHoursBusiness(),
                NonWorkedHoursDAO = new NonWorkedHoursDAO(),
                OvertimeBusiness = new OvertimeBusiness(),
                OvertimeDAO = new OvertimeDAO(),
                PermissionBusiness = new PermissionBusiness(),
                PermissionDAO = new PermissionDAO(),
                ProcessPeriodsBusiness = new ProcessPeriodsBusiness(),
                ProcessPeriodsDAO = new ProcessPeriodsDAO(),
                SuspensionBusiness = new SuspensionBusiness(),
                SuspensionDAO = new SuspensionDAO(),
                TimeOffBusiness = new TimeOffPeruBusiness(),
                TimeOffDAO = new TimeOffDAO(),
                UserBusiness = new UserArt22Business(),
                UserDAO = new UserDAO(),
                UserStatusLogBusiness = new UserStatusLogBusiness(),
                UserStatusLogDAO = new UserStatusLogDAO(),
                VacationBusiness = new VacationBusiness(),
                VacationDAO = new VacationDAO(),
                CompanyBusiness = new CompanyBusiness(),
                CompanyDAO = new CompanyPeruDAO(),
                KpiBusiness = new KpiBusiness(),
                KpiDAO = new KpiDAO(),
                GroupBusiness = new GroupBusiness()
            };
        }

        #endregion

        #region MultipleFichas
        private static CompanyConfiguration getMultipleSheetCompanyConfiguration(SesionVM sesionVM)
        {
            return new CompanyConfiguration
            {
                AbsenceBusiness = new AbsenceBusiness(),
                AbsenceDAO = new AbsenceDAO(),
                AttendanceBusiness = new AttendanceMultipleSheetBusiness(),
                AttendanceDAO = new AttendanceDAO(),
                BUKDAO = new BUKDAO(),
                EmployeeBusiness = new EmployeeMultipleSheetBusiness(),
                EmployeeDAO = new EmployeeDAO(),
                LicenceBusiness = new LicenceBusiness(),
                LicenceDAO = new LicenceDAO(),
                NonWorkedHoursBusiness = new NonWorkedHoursBusiness(),
                NonWorkedHoursDAO = new NonWorkedHoursDAO(),
                OvertimeBusiness = new OvertimeBusiness(),
                OvertimeDAO = new OvertimeDAO(),
                PermissionBusiness = new PermissionBusiness(),
                PermissionDAO = new PermissionDAO(),
                ProcessPeriodsBusiness = new ProcessPeriodsBusiness(),
                ProcessPeriodsDAO = new ProcessPeriodsDAO(),
                SuspensionBusiness = new SuspensionBusiness(),
                SuspensionDAO = new SuspensionDAO(),
                TimeOffBusiness = new TimeOffMultipleSheetBusiness(),
                TimeOffDAO = new TimeOffDAO(),
                UserBusiness = new UserMultipleSheetBusiness(),
                UserDAO = new UserDAO(),
                UserStatusLogBusiness = new UserStatusLogBusiness(),
                UserStatusLogDAO = new UserStatusLogDAO(),
                VacationBusiness = new VacationBusiness(),
                VacationDAO = new VacationDAO(),
                CompanyBusiness = new CompanyBusiness(),
                CompanyDAO = new CompanyDAO(),
                KpiBusiness = new KpiBusiness(),
                KpiDAO = new KpiDAO(),
                GroupBusiness = new GroupBusiness()
            };
        }

        #endregion

        #region MultiURLs
        private static CompanyConfiguration getMultiURLsColombiaCompanyConfiguration(SesionVM sesionVM)
        {
            return new CompanyConfiguration
            {
                AbsenceBusiness = new AbsenceBusiness(),
                AbsenceDAO = new AbsenceDAO(),
                AttendanceBusiness = new AttendanceMultiUrlsColombiaBusiness(),
                AttendanceDAO = new AttendanceColombiaDAO(),
                BUKDAO = new BUKDAO(),
                EmployeeBusiness = new EmployeeBusiness(),
                EmployeeDAO = new EmployeeDAO(),
                LicenceBusiness = new LicenceBusiness(),
                LicenceDAO = new LicenceDAO(),
                NonWorkedHoursBusiness = new NonWorkedHoursBusiness(),
                NonWorkedHoursDAO = new NonWorkedHoursDAO(),
                OvertimeBusiness = new OvertimeBusiness(),
                OvertimeDAO = new OvertimeDAO(),
                PermissionBusiness = new PermissionBusiness(),
                PermissionDAO = new PermissionDAO(),
                ProcessPeriodsBusiness = new ProcessPeriodsBusiness(),
                ProcessPeriodsDAO = new ProcessPeriodsDAO(),
                SuspensionBusiness = new SuspensionBusiness(),
                SuspensionDAO = new SuspensionDAO(),
                TimeOffBusiness = new TimeOffMultiURLBusiness(),
                TimeOffDAO = new TimeOffDAO(),
                UserBusiness = new UserMultiURLBusiness(),
                UserDAO = new UserDAO(),
                UserStatusLogBusiness = new UserStatusLogBusiness(),
                UserStatusLogDAO = new UserStatusLogDAO(),
                VacationBusiness = new VacationBusiness(),
                VacationDAO = new VacationDAO(),
                CompanyBusiness = new CompanyBusiness(),
                CompanyDAO = new CompanyDAO(),
                KpiBusiness = new KpiBusiness(),
                KpiDAO = new KpiDAO(),
                GroupBusiness = new GroupBusiness()
            };
        }

        #endregion

        #region ViernesMedioDia
        private static CompanyConfiguration getFridayHalfDayCompanyConfiguration(SesionVM sesionVM)
        {
            return new CompanyConfiguration
            {
                AbsenceBusiness = new AbsenceBusiness(),
                AbsenceDAO = new AbsenceDAO(),
                AttendanceBusiness = new AttendanceBusiness(),
                AttendanceDAO = new AttendanceDAO(),
                BUKDAO = new BUKDAO(),
                EmployeeBusiness = new EmployeeArt22Business(),
                EmployeeDAO = new EmployeeDAO(),
                LicenceBusiness = new LicenceBusiness(),
                LicenceDAO = new LicenceDAO(),
                NonWorkedHoursBusiness = new NonWorkedHoursBusiness(),
                NonWorkedHoursDAO = new NonWorkedHoursDAO(),
                OvertimeBusiness = new OvertimeBusiness(),
                OvertimeDAO = new OvertimeDAO(),
                PermissionBusiness = new PermissionFridayHalfDayBusiness(),
                PermissionDAO = new PermissionDAO(),
                ProcessPeriodsBusiness = new ProcessPeriodsBusiness(),
                ProcessPeriodsDAO = new ProcessPeriodsDAO(),
                SuspensionBusiness = new SuspensionBusiness(),
                SuspensionDAO = new SuspensionDAO(),
                TimeOffBusiness = new TimeOffFridayHalfDayBusiness(),
                TimeOffDAO = new TimeOffDAO(),
                UserBusiness = new UserArt22Business(),
                UserDAO = new UserDAO(),
                UserStatusLogBusiness = new UserStatusLogBusiness(),
                UserStatusLogDAO = new UserStatusLogDAO(),
                VacationBusiness = new VacationBusiness(),
                VacationDAO = new VacationDAO(),
                CompanyBusiness = new CompanyBusiness(),
                CompanyDAO = new CompanyDAO(),
                KpiBusiness = new KpiBusiness(),
                KpiDAO = new KpiDAO(),
                GroupBusiness = new GroupBusiness()
            };
        }

        #endregion

        #region Teisa
        private static CompanyConfiguration getTEISACompanyConfiguration(SesionVM sesionVM)
        {
            return new CompanyConfiguration
            {
                AbsenceBusiness = new AbsenceBusiness(),
                AbsenceDAO = new AbsenceDAO(),
                AttendanceBusiness = new AttendanceBusiness(),
                AttendanceDAO = new AttendanceDAO(),
                BUKDAO = new BUKDAO(),
                EmployeeBusiness = new EmployeeArt22Business(),
                EmployeeDAO = new EmployeeDAO(),
                LicenceBusiness = new LicenceBusiness(),
                LicenceDAO = new LicenceDAO(),
                NonWorkedHoursBusiness = new NonWorkedHoursBusiness(),
                NonWorkedHoursDAO = new NonWorkedHoursDAO(),
                OvertimeBusiness = new OvertimeBusiness(),
                OvertimeDAO = new OvertimeDAO(),
                PermissionBusiness = new PermissionBusiness(),
                PermissionDAO = new PermissionDAO(),
                ProcessPeriodsBusiness = new ProcessPeriodsBusiness(),
                ProcessPeriodsDAO = new ProcessPeriodsDAO(),
                SuspensionBusiness = new SuspensionBusiness(),
                SuspensionDAO = new SuspensionDAO(),
                TimeOffBusiness = new TimeOffBusiness(),
                TimeOffDAO = new TimeOffDAO(),
                UserBusiness = new UserTeisaBusiness(),
                UserDAO = new UserDAO(),
                UserStatusLogBusiness = new UserStatusLogBusiness(),
                UserStatusLogDAO = new UserStatusLogDAO(),
                VacationBusiness = new VacationBusiness(),
                VacationDAO = new VacationDAO(),
                CompanyBusiness = new CompanyBusiness(),
                CompanyDAO = new CompanyDAO(),
                KpiBusiness = new KpiBusiness(),
                KpiDAO = new KpiDAO(),
                GroupBusiness = new GroupBusiness()
            };
        }
        #endregion

        #region UsuariosDirectosColombia
        private static CompanyConfiguration getUsersDirectColombiaCompanyConfiguration(SesionVM sesionVM)
        {
            return new CompanyConfiguration
            {
                AbsenceBusiness = new AbsenceBusiness(),
                AbsenceDAO = new AbsenceDAO(),
                AttendanceBusiness = new AttendanceDirectBusiness(),
                AttendanceDAO = new AttendanceColombiaDAO(),
                BUKDAO = new BUKDAO(),
                EmployeeBusiness = new EmployeeBusiness(),
                EmployeeDAO = new EmployeeDAO(),
                LicenceBusiness = new LicenceBusiness(),
                LicenceDAO = new LicenceDAO(),
                NonWorkedHoursBusiness = new NonWorkedHoursBusiness(),
                NonWorkedHoursDAO = new NonWorkedHoursDAO(),
                OvertimeBusiness = new OvertimeBusiness(),
                OvertimeDAO = new OvertimeDAO(),
                PermissionBusiness = new PermissionBusiness(),
                PermissionDAO = new PermissionDAO(),
                ProcessPeriodsBusiness = new ProcessPeriodsBusiness(),
                ProcessPeriodsDAO = new ProcessPeriodsDAO(),
                SuspensionBusiness = new SuspensionBusiness(),
                SuspensionDAO = new SuspensionDAO(),
                TimeOffBusiness = new TimeOffColombiaBusiness(),
                TimeOffDAO = new TimeOffDAO(),
                UserBusiness = new UserDirectBusiness(),
                UserDAO = new UserDAO(),
                UserStatusLogBusiness = new UserStatusLogBusiness(),
                UserStatusLogDAO = new UserStatusLogDAO(),
                VacationBusiness = new VacationBusiness(),
                VacationDAO = new VacationDAO(),
                CompanyBusiness = new CompanyBusiness(),
                CompanyDAO = new CompanyDAO(),
                GroupBusiness = new GroupBusiness()

            };
        }

        #endregion

        #region Pichara
        private static CompanyConfiguration getPicharaCompanyConfiguration(SesionVM sesionVM)
        {
            return new CompanyConfiguration
            {
                AbsenceBusiness = new AbsenceBusiness(),
                AbsenceDAO = new AbsenceDAO(),
                AttendanceBusiness = new PicharaAttendanceBusiness(),
                AttendanceDAO = new AttendanceDAO(),
                BUKDAO = new BUKDAO(),
                EmployeeBusiness = new PicharaEmployeeFilterBusiness(),
                EmployeeDAO = new EmployeeDAO(),
                LicenceBusiness = new LicenceBusiness(),
                LicenceDAO = new LicenceDAO(),
                NonWorkedHoursBusiness = new NonWorkedHoursBusiness(),
                NonWorkedHoursDAO = new NonWorkedHoursDAO(),
                OvertimeBusiness = new OvertimeBusiness(),
                OvertimeDAO = new OvertimeDAO(),
                PermissionBusiness = new PermissionBusiness(),
                PermissionDAO = new PermissionDAO(),
                ProcessPeriodsBusiness = new ProcessPeriodsBusiness(),
                ProcessPeriodsDAO = new ProcessPeriodsDAO(),
                SuspensionBusiness = new SuspensionBusiness(),
                SuspensionDAO = new SuspensionDAO(),
                TimeOffBusiness = new PicharaTimeOffPartialBusiness(),
                TimeOffDAO = new TimeOffDAO(),
                UserBusiness = new PicharaUserLogFilterBusiness(),
                UserDAO = new UserDAO(),
                UserStatusLogBusiness = new UserStatusLogBusiness(),
                UserStatusLogDAO = new UserStatusLogDAO(),
                VacationBusiness = new VacationBusiness(),
                VacationDAO = new VacationDAO(),
                CompanyBusiness = new CompanyBusiness(),
                CompanyDAO = new CompanyDAO(),
                KpiBusiness = new KpiBusiness(),
                KpiDAO = new KpiDAO(),
                GroupBusiness = new GroupBusiness()
            };
        }
        #endregion

        #region Aconcagua
        private static CompanyConfiguration getAconcaguaCompanyConfiguration(SesionVM sesionVM)
        {
            return new CompanyConfiguration
            {
                AbsenceBusiness = new AbsenceBusiness(),
                AbsenceDAO = new AbsenceDAO(),
                AttendanceBusiness = new AttendanceAconcaguaBusiness(),
                AttendanceDAO = new AttendanceDAO(),
                BUKDAO = new BUKDAO(),
                EmployeeBusiness = new EmployeeArt22Business(),
                EmployeeDAO = new EmployeeDAO(),
                LicenceBusiness = new LicenceBusiness(),
                LicenceDAO = new LicenceDAO(),
                NonWorkedHoursBusiness = new NonWorkedHoursBusiness(),
                NonWorkedHoursDAO = new NonWorkedHoursDAO(),
                OvertimeBusiness = new OvertimeBusiness(),
                OvertimeDAO = new OvertimeDAO(),
                PermissionBusiness = new PermissionBusiness(),
                PermissionDAO = new PermissionDAO(),
                ProcessPeriodsBusiness = new ProcessPeriodsBusiness(),
                ProcessPeriodsDAO = new ProcessPeriodsDAO(),
                SuspensionBusiness = new SuspensionBusiness(),
                SuspensionDAO = new SuspensionDAO(),
                TimeOffBusiness = new TimeOffAconcaguaBusiness(),
                TimeOffDAO = new TimeOffDAO(),
                UserBusiness = new UserAconcaguaBusiness(),
                UserDAO = new UserDAO(),
                UserStatusLogBusiness = new UserStatusLogBusiness(),
                UserStatusLogDAO = new UserStatusLogDAO(),
                VacationBusiness = new VacationBusiness(),
                VacationDAO = new VacationDAO(),
                CompanyBusiness = new CompanyBusiness(),
                CompanyDAO = new CompanyDAO(),
                GroupBusiness = new GroupBusiness()

            };
        }

        #endregion

        #region CASAIDEAS
        private static CompanyConfiguration getCasaIdeasCompanyConfiguration(SesionVM sesionVM)
        {
            return new CompanyConfiguration
            {
                AbsenceBusiness = new AbsenceBusiness(),
                AbsenceDAO = new AbsenceDAO(),
                AttendanceBusiness = new AttendanceBusiness(),
                AttendanceDAO = new AttendanceDAO(),
                BUKDAO = new BUKDAO(),
                EmployeeBusiness = new EmployeeArt22Business(),
                EmployeeDAO = new EmployeeDAO(),
                LicenceBusiness = new LicenceBusiness(),
                LicenceDAO = new LicenceDAO(),
                NonWorkedHoursBusiness = new NonWorkedHoursBusiness(),
                NonWorkedHoursDAO = new NonWorkedHoursDAO(),
                OvertimeBusiness = new OvertimeBusiness(),
                OvertimeDAO = new OvertimeDAO(),
                PermissionBusiness = new PermissionBusiness(),
                PermissionDAO = new PermissionDAO(),
                ProcessPeriodsBusiness = new ProcessPeriodsBusiness(),
                ProcessPeriodsDAO = new ProcessPeriodsDAO(),
                SuspensionBusiness = new SuspensionBusiness(),
                SuspensionDAO = new SuspensionDAO(),
                TimeOffBusiness = new CasaIdeasTimeOffPartialBusiness(),
                TimeOffDAO = new TimeOffDAO(),
                UserBusiness = new CasaIdeasUserBusiness(),
                UserDAO = new UserDAO(),
                UserStatusLogBusiness = new UserStatusLogBusiness(),
                UserStatusLogDAO = new UserStatusLogDAO(),
                VacationBusiness = new VacationBusiness(),
                VacationDAO = new VacationDAO(),
                CompanyBusiness = new CompanyBusiness(),
                CompanyDAO = new CompanyDAO(),
                KpiBusiness = new KpiBusiness(),
                KpiDAO = new KpiDAO(),
                GroupBusiness = new GroupBusiness()
            };
        }
        #endregion

        #region Gestion Integral
        private static CompanyConfiguration getGestionIntegralCompanyConfiguration(SesionVM sesionVM)
        {
            return new CompanyConfiguration
            {
                AbsenceBusiness = new AbsenceBusiness(),
                AbsenceDAO = new AbsenceDAO(),
                AttendanceBusiness = new AttendanceGestionIntegralBusiness(),
                AttendanceDAO = new AttendanceDAO(),
                BUKDAO = new BUKDAO(),
                EmployeeBusiness = new EmployeeArt22Business(),
                EmployeeDAO = new EmployeeDAO(),
                LicenceBusiness = new LicenceBusiness(),
                LicenceDAO = new LicenceDAO(),
                NonWorkedHoursBusiness = new NonWorkedHoursBusiness(),
                NonWorkedHoursDAO = new NonWorkedHoursDAO(),
                OvertimeBusiness = new OvertimeBusiness(),
                OvertimeDAO = new OvertimeDAO(),
                PermissionBusiness = new PermissionBusiness(),
                PermissionDAO = new PermissionDAO(),
                ProcessPeriodsBusiness = new ProcessPeriodsBusiness(),
                ProcessPeriodsDAO = new ProcessPeriodsDAO(),
                SuspensionBusiness = new SuspensionBusiness(),
                SuspensionDAO = new SuspensionDAO(),
                TimeOffBusiness = new TimeOffBusiness(),
                TimeOffDAO = new TimeOffDAO(),
                UserBusiness = new UserBusiness(),
                UserDAO = new UserDAO(),
                UserStatusLogBusiness = new UserStatusLogBusiness(),
                UserStatusLogDAO = new UserStatusLogDAO(),
                VacationBusiness = new VacationBusiness(),
                VacationDAO = new VacationDAO(),
                CompanyBusiness = new CompanyBusiness(),
                CompanyDAO = new CompanyDAO(),
                KpiBusiness = new KpiBusiness(),
                KpiDAO = new KpiDAO(),
                GroupBusiness = new GroupBusiness()
            };
        }
        #endregion

    }
}
