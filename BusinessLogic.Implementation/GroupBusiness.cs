using API.GV.DAO;
using API.GV.IDAO;
using API.Helpers.VM;
using BusinessLogic.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace BusinessLogic.Implementation
{
    public class GroupBusiness : IGroupBusiness
    {
        private readonly IGroupDAO groupDAO;
        public GroupBusiness()
        {
            this.groupDAO = new GroupDAO();
        }

        public bool AddGroup(SesionVM empresa, GroupVM group)
        {
            return this.groupDAO.AddGroup(empresa, group);
        }

        public List<GroupVM> GetCompanyGroups(SesionVM empresa)
        {
            return groupDAO.GetCompanyGroups(empresa);
        }
    }
}
