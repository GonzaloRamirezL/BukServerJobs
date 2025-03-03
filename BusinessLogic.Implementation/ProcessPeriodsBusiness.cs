using API.BUK.DTO;
using API.BUK.DTO.Consts;
using API.BUK.DTO.Filters;
using API.BUK.IDAO;
using API.Helpers;
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
    public class ProcessPeriodsBusiness :  IProcessPeriodsBusiness
    {
        /// <summary>
        /// Devuelve los periopdos de procesamiento desde BUK
        /// </summary>
        private List<ProcessPeriod> GetProcessPeriods(SesionVM sesionActiva, CompanyConfiguration companyConfiguration)
        {
            List<ProcessPeriod> processPeriods = new List<ProcessPeriod>();
            try
            {
                var processPeriodsResponse = companyConfiguration.ProcessPeriodsDAO.GetProcessPeriods(new PaginatedFilter
                {
                    page_size = OperationalConsts.MAXIMUN_REGISTERS_PER_PAGE
                }, sesionActiva);
                if (!CollectionsHelper.IsNullOrEmpty<ProcessPeriod>(processPeriodsResponse.data))
                {
                    processPeriods.AddRange(processPeriodsResponse.data);
                }
                while (processPeriodsResponse.pagination != null && !string.IsNullOrWhiteSpace(processPeriodsResponse.pagination.next))
                {
                    processPeriodsResponse = companyConfiguration.ProcessPeriodsDAO.GetNext<ProcessPeriod>(processPeriodsResponse.pagination.next, sesionActiva.Url, sesionActiva.BukKey, sesionActiva);
                    if (!CollectionsHelper.IsNullOrEmpty<ProcessPeriod>(processPeriodsResponse.data))
                    {
                        processPeriods.AddRange(processPeriodsResponse.data);
                    }
                }
            }
            catch (Exception ex)
            {
                InsightHelper.logException(ex, sesionActiva.Empresa);
                FileLogHelper.log(LogConstants.period, LogConstants.get, "", "Error al obtener periodos desde BUK " + ex.ToString(), null, sesionActiva);
                throw new Exception("Incomplete data from BUK");
            }

            return processPeriods;
        }

        public List<ProcessPeriod> GetActivePeriods(SesionVM sesionActiva, CompanyConfiguration companyConfiguration)
        {
            List<ProcessPeriod> actives = GetProcessPeriods(sesionActiva, companyConfiguration).FindAll(p => p.status == ProcessPeriodsStatus.Abierto);
            if (!actives.IsNullOrEmpty())
            {
                if (actives.Count > 1)
                {
                    actives = actives.OrderBy(a => DateTimeHelper.parseFromBUKFormat(a.month)).ToList();
                    ProcessPeriod beforeLast = actives[actives.Count - 2];
                    ProcessPeriod last = actives[actives.Count - 1];
                    if (DateTimeHelper.differenceInMonths(DateTimeHelper.parseFromBUKFormat(last.month), DateTimeHelper.parseFromBUKFormat(beforeLast.month)) == 1)
                    {
                        return new List<ProcessPeriod> { beforeLast, last };
                    }
                    return new List<ProcessPeriod> { last };
                }
                return actives;
            }
            return new List<ProcessPeriod>();
        }

        public List<PeriodConfiguration> GetPeriodsConfiguration(SesionVM empresa, ProcessPeriod activePeriod, CompanyConfiguration companyConfiguration)
        {
            List<PeriodConfiguration> periodConfigurations = new List<PeriodConfiguration>();
            var companies_ids = companyConfiguration.CompanyBusiness.GetCompanies(empresa, companyConfiguration).Select(x => x.id).ToList();

            PeriodConfigurationResponse response = companyConfiguration.ProcessPeriodsDAO.GetPeriodsConfiguration(empresa);
            if (response != null && !response.geo_victoria_v3_configs.IsNullOrEmpty())
            {
                var configs = response.geo_victoria_v3_configs.FindAll(p => companies_ids.Contains(p.company_id) && p.process_month == activePeriod.month);
                if (!configs.IsNullOrEmpty())
                {
                    periodConfigurations.Add(configs[0]);
                }
            }


            return periodConfigurations;
        }
    }
}
