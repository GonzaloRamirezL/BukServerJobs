using API.BUK.DTO;
using API.BUK.DTO.Filters;
using API.Helpers.VM;
using System;
using System.Collections.Generic;
using System.Text;

namespace API.BUK.IDAO
{
    public interface IEmployeeDAO : IBUKDAO
    {
        PaginatedResponse<Employee> Get(PaginatedEmployeeFilter filter, SesionVM empresa);
    }
}
