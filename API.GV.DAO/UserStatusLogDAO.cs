using API.GV.DTO;
using API.GV.IDAO;
using API.Helpers;
using API.Helpers.VM;
using System;
using System.Collections.Generic;
using System.Text;

namespace API.GV.DAO
{
    public class UserStatusLogDAO : IUserStatusLogDAO
    {
        public List<UserStatusLog> GetStatusLog(string users, SesionVM empresa)
        {
            var result = new RestConsumer(BaseAPI.GV, empresa.GvUrl, empresa.GvKey, empresa).PostResponse<List<UserStatusLog>, object>("UserStatusLog/Get", new { UserIdentifiers = users });
            if (result == null)
            {
                throw new Exception("No response from GV");
            }
            return result;
        }
    }
}
