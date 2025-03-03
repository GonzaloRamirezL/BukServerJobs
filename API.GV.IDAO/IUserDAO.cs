using API.GV.DTO;
using API.Helpers.VM;
using System;
using System.Collections.Generic;

namespace API.GV.IDAO
{
    public interface IUserDAO
    {
        /// <summary>
        /// Devuelve los usuarios desde la api de GV
        /// </summary>        
        /// <returns>
        ///     El listado de usuarios del periodo solicitado
        /// </returns>
        List<User> GetList(SesionVM empresa);

        /// <summary>
        /// Agrega un usuario nuevo a través de la api de GV
        /// </summary>        
        /// <returns>
        ///    Objeto ApiResponse que indica el resultado de la operación o no
        /// </returns>
        ApiResponse Add(User newUser, SesionVM empresa);

        /// <summary>
        /// Habilita un usuario a través de la api de GV
        /// </summary>        
        /// <returns>
        ///    Objeto ApiResponse que indica el resultado de la operación o no
        /// </returns>
        ApiResponse Enable(User userToEnable, SesionVM empresa);

        /// <summary>
        /// Desabilita un usuario a través de la api de GV
        /// </summary>        
        /// <returns>
        ///    Objeto ApiResponse que indica el resultado de la operación o no
        /// </returns>
        ApiResponse Disable(User userToDisable, SesionVM empresa);

        /// <summary>
        /// Edita un usuario a través de la api de GV
        /// </summary>        
        /// <returns>
        ///    Objeto ApiResponse que indica el resultado de la operación o no
        /// </returns>
        ApiResponse Edit(User userToEdit, SesionVM empresa);

        /// <summary>
        /// Elimina un usuario de todos los grupos que pertenece a través de la api de GV
        /// </summary>        
        /// <returns>
        ///    Objeto ApiResponse que indica el resultado de la operación o no
        /// </returns>
        ApiResponse RemoveFromGroups(User userToRemove, SesionVM empresa);

        /// <summary>
        /// Mueve un usuario al grupo correspondiente a través de la api de GV
        /// </summary>        
        /// <returns>
        ///    Objeto ApiResponse que indica el resultado de la operación o no
        /// </returns>
        ApiResponse MoveToGroup(User userToMove, SesionVM empresa);
    }
}
