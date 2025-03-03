using API.BUK.DTO;
using API.Helpers.VM;
using API.Helpers.VM.Consts;
using BusinessLogic.Interfaces.VM;
using System.Collections.Generic;

namespace BusinessLogic.Interfaces
{
    public interface IkpiBusiness
    {
        /// <summary>
        /// Devuelve los tipos de Kpis desde BUK
        /// </summary>        
        /// <returns>
        ///     Listado de tipos de Kpis
        /// </returns>
        List<KpiType> GetKpiTypes(string relatedTo, SesionVM company, CompanyConfiguration companyConfiguration, int registersPerPage = OperationalConsts.MAXIMUN_REGISTERS_PER_PAGE);

        /// <summary>
        /// Devuelve los datos de Kpis desde BUK
        /// </summary>        
        /// <returns>
        ///     Listado de datos de Kpis
        /// </returns>
        List<KpiData> Get(SesionVM company, CompanyConfiguration companyConfiguration, int registersPerPage = OperationalConsts.MAXIMUN_REGISTERS_PER_PAGE);

        /// <summary>
        /// Inserta una lista de datos de KPIs a través de la API de BUK
        /// </summary>
        /// <param name="kpiData"></param>
        /// <param name="company"></param>
        /// <returns></returns>
        void AddKpiData(List<KpiData> kpiData, SesionVM company, CompanyConfiguration companyConfiguration);

        /// <summary>
        /// Actualiza los datos de un listado de KPIs a través de la API de BUK
        /// </summary>
        /// <param name="kpiData"></param>
        /// <param name="company"></param>
        /// <returns></returns>
        void UpdateKpiData(List<KpiData> kpiData, SesionVM company, CompanyConfiguration companyConfiguration);
    }
}
