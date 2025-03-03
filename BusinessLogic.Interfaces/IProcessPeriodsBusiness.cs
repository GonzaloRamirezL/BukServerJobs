using API.BUK.DTO;
using API.Helpers.VM;
using BusinessLogic.Interfaces.VM;
using System;
using System.Collections.Generic;
using System.Text;

namespace BusinessLogic.Interfaces
{
    public interface IProcessPeriodsBusiness
    {
        /// <summary>
        /// Devuelve los periodos de procesamiento activos desde BUK en el periodo solicitado
        /// </summary>        
        /// <returns>
        ///     Listado de ProcessPeriod
        /// </returns>
        List<ProcessPeriod> GetActivePeriods(SesionVM sesionActiva, CompanyConfiguration companyConfiguration);

        /// <summary>
        /// Devuelve las configuraciones de fechas  desde BUK para el periodo de procesamiento solicitado
        /// </summary>        
        /// <returns>
        ///     Listado de PeriodConfiguration
        /// </returns>
        List<PeriodConfiguration> GetPeriodsConfiguration(SesionVM empresa, ProcessPeriod activePeriod, CompanyConfiguration companyConfiguration);
    }
}
