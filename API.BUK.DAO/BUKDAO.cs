using API.BUK.DTO;
using API.BUK.IDAO;
using API.Helpers;
using API.Helpers.VM;
using System;
using System.Collections.Generic;
using System.Text;

namespace API.BUK.DAO
{
    public class BUKDAO : IBUKDAO
    {
        public PaginatedResponse<T> GetNext<T>(string uri, string UrlBase, string Key, SesionVM empresa)
        {
            return new RestConsumer(BaseAPI.BUK, UrlBase, Key, empresa).GetResponse<PaginatedResponse<T>, object>(uri, new object { });
        }
    }
}
