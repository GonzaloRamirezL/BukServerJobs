using API.GV.DTO;
using API.GV.IDAO;
using API.Helpers;
using API.Helpers.VM;
using System;
using System.Collections.Generic;
using System.Text;

namespace API.GV.DAO
{
    public class UserDAO : IUserDAO
    {
        public List<User> GetList(SesionVM empresa)
        {
            var result = new RestConsumer(BaseAPI.GV, empresa.GvUrl, empresa.GvKey, empresa).PostResponse<List<User>, object>("User/Get", new {  });
            if (result == null)
            {
                throw new Exception("No response from GV");
            }
            return result;
        }

        public ApiResponse Add(User newUser, SesionVM empresa)
        {
            var result = new RestConsumer(BaseAPI.GV, empresa.GvUrl, empresa.GvKey, empresa).PostResponse<ApiResponse, User>("User/Insert", newUser);
            if (result == null)
            {
                throw new Exception("Not created in GV");
            }
            return result;
        }

        public ApiResponse Enable(User userToEnable, SesionVM empresa)
        {
            var result = new RestConsumer(BaseAPI.GV, empresa.GvUrl, empresa.GvKey, empresa).PostResponse<ApiResponse, User>("User/Enable", userToEnable);
            if (result == null)
            {
                throw new Exception("Not enabled in GV");
            }
            return result;
        }

        public ApiResponse Disable(User userToDisable, SesionVM empresa)
        {
            var result = new RestConsumer(BaseAPI.GV, empresa.GvUrl, empresa.GvKey, empresa).PostResponse<ApiResponse, User>("User/Disable", userToDisable);
            if (result == null)
            {
                throw new Exception("Not disabled in GV");
            }
            return result;
        }

        public ApiResponse Edit(User userToEdit, SesionVM empresa)
        {
            var result = new RestConsumer(BaseAPI.GV, empresa.GvUrl, empresa.GvKey, empresa).PostResponse<ApiResponse, User>("User/Edit", userToEdit);
            if (result == null)
            {
                throw new Exception("Not edited in GV");
            }
            return result;
        }

        public ApiResponse RemoveFromGroups(User userToRemove, SesionVM empresa)
        {
            var result = new RestConsumer(BaseAPI.GV, empresa.GvUrl, empresa.GvKey, empresa).PostResponse<ApiResponse, User>("User/RemoveLeaderFromGroups", userToRemove);
            if (result == null)
            {
                throw new Exception("User not removed from groups");
            }
            return result;
        }

        public ApiResponse MoveToGroup(User userToMove, SesionVM empresa)
        {
            var result = new RestConsumer(BaseAPI.GV, empresa.GvUrl, empresa.GvKey, empresa).PostResponse<ApiResponse, User>("User/moveGeneral", userToMove);
            if (result == null)
            {
                throw new Exception("User not moved to group");
            }
            return result;
        }
    }
}
