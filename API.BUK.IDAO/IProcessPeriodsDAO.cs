using API.BUK.DTO;
using API.BUK.DTO.Filters;
using API.Helpers.VM;
using System;
using System.Collections.Generic;
using System.Text;

namespace API.BUK.IDAO
{
    public interface IProcessPeriodsDAO : IBUKDAO
    {
        PaginatedResponse<ProcessPeriod> GetProcessPeriods(PaginatedFilter filter, SesionVM empresa);
        PeriodConfigurationResponse GetPeriodsConfiguration(SesionVM empresa);
    }
}
