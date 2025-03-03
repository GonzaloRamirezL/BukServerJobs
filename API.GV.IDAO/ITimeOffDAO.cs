using API.GV.DTO;
using API.GV.DTO.Filters;
using API.Helpers.VM;
using System;
using System.Collections.Generic;
using System.Text;

namespace API.GV.IDAO
{
    public interface ITimeOffDAO
    {
        /// <summary>
        /// Devuelve los permisos desde la api de GV
        /// </summary>        
        /// <returns>
        ///     El listado de permisos del periodo solicitado
        /// </returns>
        List<TimeOff> GetList(TimeOffFilter filter, SesionVM empresa);

        /// <summary>
        /// Agrega un permiso nuevo a través de la api de GV
        /// </summary>        
        /// <returns>
        ///     Si fue satisfactoria la operación o no
        /// </returns>
        bool Add(TimeOffToAdd timeOff, SesionVM empresa);

        /// <summary>
        /// Elimina un permiso a través de la api de GV
        /// </summary>        
        /// <returns>
        ///     Si fue satisfactoria o no la operación
        /// </returns>
        bool Delete(TimeOffToDelete timeOff, SesionVM empresa);

        /// <summary>
        /// Devuelve los tipos de permisos desde la api de GV
        /// </summary>        
        /// <returns>
        ///     Listado de tipos de permisos
        /// </returns>
        List<TimeOffType> GetTypes(SesionVM empresa);

        /// <summary>
        /// Devuelve todos los tipos de permisos desde la api de GV, sin discriminar por su estado. (Activado/desactivado)
        /// </summary>
        /// <param name="empresa"></param>
        /// <returns> Listado de tipos de permisos</returns>
        List<TimeOffType> GetAllTypes(SesionVM empresa);

        /// <summary>
        /// Agrega un nuevo tipo de permiso a través de la api de GV
        /// </summary>        
        /// <returns>
        ///     El tipo de permiso nuevo recién agregado
        /// </returns>
        TimeOffType AddType(TimeOffType newType, SesionVM empresa);
    }
}
