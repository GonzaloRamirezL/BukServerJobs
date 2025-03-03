using API.BUK.DTO;
using API.BUK.DTO.Filters;
using API.GV.DTO;
using API.Helpers.VM;

namespace API.BUK.IDAO
{
    public interface ISuspensionDAO : IBUKDAO
    {
        PaginatedResponse<Suspension> GetSuspensions(PaginatedAbsenceFilter filter, SesionVM empresa);
    }
}
