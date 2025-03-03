using API.BUK.DTO;
using API.BUK.DTO.Filters;
using API.Helpers.VM;

namespace API.BUK.IDAO
{
    public interface IKpiDAO
    {
        /// <summary>
        /// Retorna todos los tipos de KPI desde BUK
        /// </summary>
        /// <param name="filter"></param>
        /// <param name="company"></param>
        /// <returns></returns>
        PaginatedResponse<KpiType> GetTypes(PaginatedKpiTypeFilter filter, SesionVM company);

        /// <summary>
        /// Retorna los datos de KPI desde BUK
        /// </summary>
        /// <param name="filter"></param>
        /// <param name="company"></param>
        /// <returns></returns>
        PaginatedResponse<KpiData> Get(PaginatedFilter filter, SesionVM company);

        /// <summary>
        /// Registra un nuevo dato de KPI en BUK
        /// </summary>
        /// <param name="kpiData"></param>
        /// <param name="company"></param>
        /// <returns></returns>
        void AddKpiData(KpiData kpiData, SesionVM company);

        /// <summary>
        /// Actualiza el dato de un KPI en BUK
        /// </summary>
        /// <param name="kpiData"></param>
        /// <param name="company"></param>
        /// <returns></returns>
        KpiData UpdateKpiData(KpiData kpiData, SesionVM company);
    }
}
