using API.BUK.DTO;
using API.Helpers.VM;
using BusinessLogic.Interfaces.VM;
using System;
using System.Collections.Generic;
using System.Text;

namespace BusinessLogic.Interfaces
{
    public interface ITimeOffBusiness
    {
        /// <summary>
        /// Realiza la sincronización del módulo de permisos
        /// </summary>        
        /// <returns>
        ///     No devuelve nada
        /// </returns>
        void Sync(SesionVM Empresa, ProcessPeriod periodo, List<PeriodConfiguration> configs, CompanyConfiguration companyConfiguration);
    }
}
