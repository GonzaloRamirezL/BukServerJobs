using API.BUK.DTO;
using API.BUK.DTO.Filters;
using API.GV.DTO;
using API.Helpers.VM;

namespace API.BUK.IDAO
{
    public interface IVacationDAO : IBUKDAO
    {
        /// <summary>
        /// Obtiene las vacaciones de todos los usuarios para un rango de fechas
        /// </summary>
        PaginatedResponse<Vacation> Get(PaginatedVacationFilter filter, string UrlBase, string Key, SesionVM empresa);

        /// <summary>
        /// Eliminas las vacaciones un usuario en un rango de fechas
        /// </summary>
        ApiResponse DeleteVacation(SesionVM session, Vacation vacation);

        /// <summary>
        /// Envia vacaciones para su alta en Buk
        /// </summary>
        ApiResponse SendVacation(SesionVM session, VacationToSend vacation);
    }
}
