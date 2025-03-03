using API.BUK.DTO;
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
using System.Text;

namespace BusinessLogic.Implementation
{
    public class SuspensionBusiness :  ISuspensionBusiness
    {
        public List<Suspension> GetSuspensionsBUK(DateTime startDate, DateTime endDate, SesionVM sesionActiva, CompanyConfiguration companyConfiguration)
        {
            List<Suspension> suspensions = new List<Suspension>();
            try
            {
                var suspensionsResponse = companyConfiguration.SuspensionDAO.GetSuspensions(new PaginatedAbsenceFilter
                {
                    from = DateTimeHelper.parseToBUKFormat(startDate),
                    to = DateTimeHelper.parseToBUKFormat(endDate),
                    page_size = OperationalConsts.MAXIMUN_REGISTERS_PER_PAGE
                }, sesionActiva);
                if (!CollectionsHelper.IsNullOrEmpty<Suspension>(suspensionsResponse.data))
                {
                    suspensions.AddRange(suspensionsResponse.data);
                }
                while (suspensionsResponse.pagination != null && !string.IsNullOrWhiteSpace(suspensionsResponse.pagination.next))
                {
                    suspensionsResponse = companyConfiguration.SuspensionDAO.GetNext<Suspension>(suspensionsResponse.pagination.next, sesionActiva.Url, sesionActiva.BukKey, sesionActiva);
                    if (!CollectionsHelper.IsNullOrEmpty<Suspension>(suspensionsResponse.data))
                    {
                        suspensions.AddRange(suspensionsResponse.data);
                    }
                }
            }
            catch (Exception ex)
            {
                InsightHelper.logException(ex, sesionActiva.Empresa);
                FileLogHelper.log(LogConstants.general, LogConstants.get, "", "Error al obtener suspensiones " + ex.Message, null, sesionActiva);
                throw new Exception("Incomplete data from BUK");
            }

            return suspensions;
        }
    }
}
