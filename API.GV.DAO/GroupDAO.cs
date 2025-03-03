using API.GV.IDAO;
using API.Helpers;
using API.Helpers.VM;
using System;
using System.Collections.Generic;
using System.Text;

namespace API.GV.DAO
{
    public class GroupDAO : IGroupDAO
    {
        public bool AddGroup(SesionVM empresa, GroupVM group)
        {
            return new RestConsumer(BaseAPI.GV, empresa.GvUrl, empresa.GvKey, empresa).PostResponse<bool, GroupVM>("Group/AddGroup", group);
        }

        public List<GroupVM> GetCompanyGroups(SesionVM empresa)
        {
            return new RestConsumer(BaseAPI.GV, empresa.GvUrl, empresa.GvKey, empresa).PostResponse<List<GroupVM>, object>("Group/Get", new object { });
        }
    }
}
