using API.Helpers.VM;
using System;
using System.Collections.Generic;
using System.Text;

namespace BusinessLogic.Interfaces
{
    public interface IGroupBusiness
    {
        /// <summary>
        /// Obtiene los grupos de la empresa
        /// </summary>
        /// <param name="empresa"></param>
        /// <returns></returns>
        List<GroupVM> GetCompanyGroups(SesionVM empresa);
        /// <summary>
        /// Añade un nuevo grupo a la empresa
        /// </summary>
        /// <param name="empresa"></param>
        /// <param name="group"></param>
        /// <returns></returns>
        bool AddGroup(SesionVM empresa, GroupVM group);
    }
}
