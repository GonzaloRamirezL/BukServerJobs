using API.BUK.DTO;
using API.BUK.IDAO;
using API.GV.DTO;
using API.Helpers;
using API.Helpers.Commons;
using API.Helpers.VM;
using System;

namespace API.BUK.DAO
{
    public class ItemDAO : BUKDAO, IItemDAO
    {
        public PaginatedResponse<Item> GetItems(SesionVM empresa, string itemCode)
        {
            string currentDate = DateTimeHelper.parseToBUKFormat(DateTime.Now);
            string uri = $"items?code={itemCode}&date={currentDate}";

            PaginatedResponse<Item> result = new RestConsumer(BaseAPI.BUK, empresa.Url, empresa.BukKey, empresa).GetResponse<PaginatedResponse<Item>, object>(uri, new object { });
            return result;
        }

        public PaginatedResponse<AssignedItem> GetAssignsForUser(SesionVM empresa, int employeeId, string date)
        {
            string uri = $"employees/{employeeId}/assigns?date={date}&page_size=100";

            PaginatedResponse<AssignedItem> result = new RestConsumer(BaseAPI.BUK, empresa.Url, empresa.BukKey, empresa).GetResponse<PaginatedResponse<AssignedItem>, object>(uri, new object { });
            return result;
        }

        public ApiResponse AssignItem(SesionVM empresa, ItemToAssign itemToAssign)
        {
            string uri = "assigns";
            ApiResponse response = new RestConsumer(BaseAPI.BUK, empresa.Url, empresa.BukKey, empresa).PostResponse<ApiResponse, ItemToAssign>(uri, itemToAssign);

            return response ?? throw new Exception("No response from BUK");
        }

        public bool DeleteAssign(SesionVM session, int assignId)
        {
            string uri = $"assigns/{assignId}";

            var response = new RestConsumer(BaseAPI.BUK, session.Url, session.BukKey, session).DeleteResponse<EliminationResponse, object>(uri, null);

            if (response == null)
            {
                throw new Exception("No response from BUK");
            }

            return response.deleted;
        }
    }
}
