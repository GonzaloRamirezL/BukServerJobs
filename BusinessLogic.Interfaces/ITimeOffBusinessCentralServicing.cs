using API.BUK.DTO;
using API.Helpers.VM;
using BusinessLogic.Interfaces.VM;
using System;
using System.Collections.Generic;
using System.Text;

namespace BusinessLogic.Interfaces
{
    public  interface ITimeOffBusinessCentralServicing
    {
        /// <summary>
        /// Realiza la sincronización del módulo de permisos
        /// </summary>
        void Sync(SesionVM Empresa, ProcessPeriod periodo, List<PeriodConfiguration> configs, CompanyConfiguration companyConfiguration);
    }
}
