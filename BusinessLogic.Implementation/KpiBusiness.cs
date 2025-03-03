using API.BUK.DTO;
using API.BUK.DTO.Filters;
using API.Helpers.Commons;
using API.Helpers.VM;
using API.Helpers.VM.Consts;
using BusinessLogic.Interfaces;
using BusinessLogic.Interfaces.VM;
using System;
using System.Collections.Generic;

namespace BusinessLogic.Implementation
{
    public class KpiBusiness : IkpiBusiness
    {
        public List<KpiType> GetKpiTypes(string relatedTo, SesionVM company, CompanyConfiguration companyConfiguration, int registersPerPage = OperationalConsts.MAXIMUN_REGISTERS_PER_PAGE)
        {
            List<KpiType> kpiTypes = new List<KpiType>();
            try
            {
                var kpiTypesResponse = companyConfiguration.KpiDAO.GetTypes(new PaginatedKpiTypeFilter
                {
                    related_to = relatedTo,
                    page_size = registersPerPage
                }, company);

                if (!CollectionsHelper.IsNullOrEmpty<KpiType>(kpiTypesResponse.data))
                {
                    kpiTypes.AddRange(kpiTypesResponse.data);
                }

                while (kpiTypesResponse.pagination != null && !string.IsNullOrWhiteSpace(kpiTypesResponse.pagination.next))
                {
                    kpiTypesResponse = companyConfiguration.VacationDAO.GetNext<KpiType>(kpiTypesResponse.pagination.next, company.Url, company.BukKey, company);
                    if (!CollectionsHelper.IsNullOrEmpty<KpiType>(kpiTypesResponse.data))
                    {
                        kpiTypes.AddRange(kpiTypesResponse.data);
                    }
                }
            }
            catch (Exception ex)
            {
                InsightHelper.logException(ex, company.Empresa);
                FileLogHelper.log(LogConstants.general, LogConstants.get, "", "ERROR AL OBTENER LOS TIPOS DE KPI DESDE BUK", null, company);
                throw new Exception("Incomplete data from BUK");
            }

            return kpiTypes;
        }

        public List<KpiData> Get(SesionVM company, CompanyConfiguration companyConfiguration, int registersPerPage = OperationalConsts.MAXIMUN_REGISTERS_PER_PAGE)
        {
            List<KpiData> kpis = new List<KpiData>();
            try
            {
                var kpisResponse = companyConfiguration.KpiDAO.Get(new PaginatedFilter
                {
                    page_size = registersPerPage
                }, company);

                if (!CollectionsHelper.IsNullOrEmpty<KpiData>(kpisResponse.data))
                {
                    kpis.AddRange(kpisResponse.data);
                }

                while (kpisResponse.pagination != null && !string.IsNullOrWhiteSpace(kpisResponse.pagination.next))
                {
                    kpisResponse = companyConfiguration.VacationDAO.GetNext<KpiData>(kpisResponse.pagination.next, company.Url, company.BukKey, company);
                    if (!CollectionsHelper.IsNullOrEmpty<KpiData>(kpisResponse.data))
                    {
                        kpis.AddRange(kpisResponse.data);
                    }
                }
            }
            catch (Exception ex)
            {
                InsightHelper.logException(ex, company.Empresa);
                FileLogHelper.log(LogConstants.general, LogConstants.get, "", "ERROR AL OBTENER LOS DATOS DE KPI DESDE BUK", null, company);
                throw new Exception("Incomplete data from BUK");
            }

            return kpis;
        }

        public void AddKpiData(List<KpiData> kpisToAdd, SesionVM company, CompanyConfiguration companyConfiguration)
        {
            FileLogHelper.log(LogConstants.general, LogConstants.get, "", "Se intentará(n) adicionar " + kpisToAdd.Count + " datos(s) de KPI", null, company);
            foreach (KpiData kpiData in kpisToAdd)
            {
                try
                {
                    companyConfiguration.KpiDAO.AddKpiData(kpiData, company);
                }
                catch (Exception ex)
                {
                    InsightHelper.logException(ex, company.Empresa);
                }
            }
        }

        public void UpdateKpiData(List<KpiData> kpisToEdit, SesionVM company, CompanyConfiguration companyConfiguration)
        {
            FileLogHelper.log(LogConstants.general, LogConstants.get, "", "Se intentará(n) actualizar " + kpisToEdit.Count + " datos(s) de KPI", null, company);
            foreach (KpiData kpiData in kpisToEdit)
            {
                try
                {
                    KpiData updatedKpi = companyConfiguration.KpiDAO.UpdateKpiData(kpiData, company);
                    //Si retorna null es que no pudo editar el dato de KPI porque está cerrado, se inserta uno nuevo
                    if (updatedKpi == null)
                    {
                        companyConfiguration.KpiDAO.AddKpiData(kpiData, company);
                    }
                }
                catch (Exception ex)
                {
                    InsightHelper.logException(ex, company.Empresa);
                }
            }
        }
    }
}
