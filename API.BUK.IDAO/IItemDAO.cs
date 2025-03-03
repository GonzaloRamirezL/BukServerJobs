using API.BUK.DTO;
using API.GV.DTO;
using API.Helpers.VM;
using System;
using System.Collections.Generic;
using System.Text;

namespace API.BUK.IDAO
{
    public interface IItemDAO : IBUKDAO
    {
       
        /// <summary>
        /// Obtiene un elemento del tipo Item, filtrando por código.
        /// </summary>
        /// <param name="empresa">Datos de la empresa necesarios para acceder a la api de Buk</param>
        /// <param name="code">Código del item para aplicar filtrado</param>
        /// <returns>Item</returns>
        PaginatedResponse<Item> GetItems(SesionVM empresa, string code);

        /// <summary>
        /// Obtiene todas las asignaciones (Item) para un empleado
        /// </summary>
        /// <param name="empresa"></param>
        /// <param name="employeeId"></param>
        /// <returns></returns>
        PaginatedResponse<AssignedItem> GetAssignsForUser(SesionVM empresa, int employeeId, string date);
        
        /// <summary>
        /// Envía cambios para un item y usuario especificos.
        /// </summary>
        ApiResponse AssignItem(SesionVM empresa, ItemToAssign itemToAssign);

        /// <summary>
        /// Elimina la asignación de un item vinculado a un usuario en Buk
        /// </summary>
        /// <param name="session"></param>
        /// <param name="assignId"></param>
        /// <returns></returns>
        bool DeleteAssign(SesionVM session, int assignId);
    }
}
