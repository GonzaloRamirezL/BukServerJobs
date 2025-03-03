using API.Helpers.VM;
using System;
using System.Collections.Generic;
using System.Text;

namespace API.GV.IDAO
{
    public interface IGroupDAO
    {
        /// <summary>
        /// Obtiene los grupos de la empresa en base al authorization key
        /// </summary>
        /// <returns></returns>
        List<GroupVM> GetCompanyGroups(SesionVM empresa);
        /// <summary>
        /// Añade un nuevo grupo en al authorization key
        /// </summary>
        /// <param name="empresa"></param>
        /// <param name="group"></param>
        /// <returns></returns>
        bool AddGroup(SesionVM empresa, GroupVM group);
    }
}
