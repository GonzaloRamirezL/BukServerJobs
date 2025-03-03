using API.BUK.DTO;
using API.BUK.DTO.Filters;
using API.Helpers.VM;
using System;
using System.Collections.Generic;
using System.Text;

namespace API.BUK.IDAO
{
    public interface IOvertimeDAO : IBUKDAO
    {
        void AddOverTime(Overtime overtime, SesionVM empresa);

        PaginatedResponse<OvertimeType> GetOvertimeTypes(PaginatedFilter filter, SesionVM empresa);
    }
}
