using API.BUK.DTO;
using API.BUK.DTO.Filters;
using API.BUK.IDAO;
using API.GV.DTO;
using API.Helpers;
using API.Helpers.Commons;
using API.Helpers.VM;
using System;

namespace API.BUK.DAO
{
    public class VacationDAO : BUKDAO, IVacationDAO
    {
        public PaginatedResponse<Vacation> Get(PaginatedVacationFilter filter, string UrlBase, string Key, SesionVM empresa)
        {
            string uri = "vacations?end_after=" + filter.date + "&start_before=" + filter.end_date;
           

            if (filter.page_size > 0)
            {
                uri += "&page_size=" + filter.page_size;
            }
            var result = new RestConsumer(BaseAPI.BUK, UrlBase, Key, empresa).GetResponse<PaginatedResponse<Vacation>, object>(uri, new object { });
            if (result == null)
            {
                throw new Exception("No response from BUK");
            }
            return result;
        }

        public ApiResponse DeleteVacation(SesionVM session, Vacation vacation)
        {
            string uri = $"vacations?employee_id={vacation.employee_id}&start_date={vacation.start_date}&end_date={vacation.end_date}";

            ApiResponse response = new RestConsumer(BaseAPI.BUK, session.Url, session.BukKey, session).DeleteResponse<ApiResponse, Vacation>(uri, null);

            if (response == null)
            {
                throw new Exception("No response from BUK");
            }

            return response;
        }

        public ApiResponse SendVacation(SesionVM session, VacationToSend vacation)
        {
            string uri = "vacations";
            ApiResponse response = new RestConsumer(BaseAPI.BUK, session.Url, session.BukKey, session).PostResponse<ApiResponse, VacationToSend>(uri, vacation);

            return response ?? throw new Exception("No response from BUK");
        }
    }
}
