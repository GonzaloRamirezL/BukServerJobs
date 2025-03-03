using API.BUK.DTO;
using API.BUK.DTO.Filters;
using API.Helpers.Commons;
using API.Helpers.VM;
using API.Helpers.VM.Consts;
using BusinessLogic.Interfaces;
using BusinessLogic.Interfaces.VM;
using System;
using System.Collections.Generic;
using System.Text;

namespace BusinessLogic.Implementation
{
    public class OvertimeBusiness : IOvertimeBusiness
    {
        public List<OvertimeType> GetOvertimeTypes(SesionVM sesionActiva, CompanyConfiguration companyConfiguration)
        {
            List<OvertimeType> overtimeTypes = new List<OvertimeType>();
            try
            {
                var overtimeTypesResponse = companyConfiguration.OvertimeDAO.GetOvertimeTypes(new PaginatedFilter
                {
                    page_size = OperationalConsts.MAXIMUN_REGISTERS_PER_PAGE
                }, sesionActiva);
                if (!CollectionsHelper.IsNullOrEmpty<OvertimeType>(overtimeTypesResponse.data))
                {
                    overtimeTypes.AddRange(overtimeTypesResponse.data);
                }
                while (overtimeTypesResponse.pagination != null && !string.IsNullOrWhiteSpace(overtimeTypesResponse.pagination.next))
                {
                    overtimeTypesResponse = companyConfiguration.OvertimeDAO.GetNext<OvertimeType>(overtimeTypesResponse.pagination.next, sesionActiva.Url, sesionActiva.BukKey, sesionActiva);
                    if (!CollectionsHelper.IsNullOrEmpty<OvertimeType>(overtimeTypesResponse.data))
                    {
                        overtimeTypes.AddRange(overtimeTypesResponse.data);
                    }
                }
            }
            catch (Exception ex)
            {

                InsightHelper.logException(ex, sesionActiva.Empresa);
                FileLogHelper.log(LogConstants.general, LogConstants.get, "", "ERROR AL OBTENER TIPO DE HHEE DESDE BUK", null, sesionActiva);
                throw new Exception("Incomplete data from BUK");
            }

            return overtimeTypes;
        }

        public void AddOverTime(Overtime overtime, SesionVM empresa, CompanyConfiguration companyConfiguration)
        {
            companyConfiguration.OvertimeDAO.AddOverTime(overtime, empresa);
        }
    }
}
