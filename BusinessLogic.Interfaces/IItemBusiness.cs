using API.BUK.DTO;
using API.GV.DTO;
using API.Helpers.VM;
using BusinessLogic.Interfaces.VM;
using System;
using System.Collections.Generic;
using System.Text;

namespace BusinessLogic.Interfaces
{
    public interface IItemBusiness
    {
        /// <summary>
        /// Obtiene un elemento vigente del tipo Item, filtrando por código.
        /// </summary>
        /// <param name="empresa">Datos de la empresa necesarios para acceder a la api de Buk</param>
        /// <param name="code">Código del item para aplicar filtrado</param>
        /// <returns>Item</returns>
        Item GetItem(CompanyConfiguration companyConfiguration, SesionVM empresa, string code);

        /// <summary>
        /// Obtiene todos los items asignados a un usuario específico.
        /// </summary>
        /// <param name="companyConfiguration"></param>
        /// <param name="empresa"></param>
        /// <param name="employeeId"></param>
        /// <param name="date"></param>
        /// <returns></returns>
        List<AssignedItem> GetAssignsForUser(CompanyConfiguration companyConfiguration, SesionVM empresa, int employeeId, string date);

        /// <summary>
        /// Envía cambios para un item y usuario específicos.
        /// </summary>
        void AssignItems(CompanyConfiguration companyConfiguration, SesionVM empresa, List<ItemToAssign> itemsToAssign);

        /// <summary>
        /// Elimina la asignación de un item para un usuario en especifico
        /// </summary>
        void DeleteAssign(CompanyConfiguration companyConfiguration, SesionVM empresa, int assignId);
        
    }
}
