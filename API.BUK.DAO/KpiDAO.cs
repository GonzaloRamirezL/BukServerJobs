using API.BUK.DTO;
using API.BUK.DTO.Filters;
using API.BUK.IDAO;
using API.Helpers;
using API.Helpers.VM;
using System;

namespace API.BUK.DAO
{
    public class KpiDAO : IKpiDAO
    {
        public PaginatedResponse<KpiType> GetTypes(PaginatedKpiTypeFilter filter, SesionVM company)
        {
            string uri = "kpis";

            if (filter.page_size > 0 || !string.IsNullOrWhiteSpace(filter.related_to))
            {
                uri += "?";

                if (!string.IsNullOrWhiteSpace(filter.related_to))
                {
                    uri += "related_to=" + filter.related_to;
                    if (filter.page_size > 0)
                    {
                        uri += "&";
                    }
                }

                if (filter.page_size > 0)
                {
                    uri += "page_size=" + filter.page_size;
                }
            }

            var result = new RestConsumer(BaseAPI.BUK, company.Url, company.BukKey, company).GetResponse<PaginatedResponse<KpiType>, object>(uri, new object { });
            if (result == null)
            {
                throw new Exception("No response from BUK");
            }

            return result;
        }

        public PaginatedResponse<KpiData> Get(PaginatedFilter filter, SesionVM company)
        {
            string uri = "kpi_data";

            if (filter.page_size > 0)
            {
                uri += "?page_size=" + filter.page_size;
            }
            var result = new RestConsumer(BaseAPI.BUK, company.Url, company.BukKey, company).GetResponse<PaginatedResponse<KpiData>, object>(uri, new object { });
            if (result == null)
            {
                throw new Exception("No response from BUK");
            }
            
            return result;
        }        

        public void AddKpiData(KpiData kpiData, SesionVM company)
        {
            string uri = "kpi_data";
            new RestConsumer(BaseAPI.BUK, company.Url, company.BukKey, company).PostResponse<object, KpiData>(uri, kpiData);
        }

        public KpiData UpdateKpiData(KpiData kpiData, SesionVM company)
        {
            string uri = "kpi_data/" + kpiData.id;
            return new RestConsumer(BaseAPI.BUK, company.Url, company.BukKey, company).PutResponse<KpiData, KpiData>(uri, kpiData);
        }
    }
}
