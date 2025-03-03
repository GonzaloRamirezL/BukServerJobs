using API.BUK.DTO;
using API.Helpers.VM;
using System;

namespace API.BUK.IDAO
{
    public interface IBUKDAO
    {
        PaginatedResponse<T> GetNext<T>(string uri, string UrlBase, string Key, SesionVM empresa);
    }
}
