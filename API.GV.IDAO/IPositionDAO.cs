using API.GV.DTO;
using API.Helpers.VM;
using System;
using System.Collections.Generic;
using System.Text;

namespace API.GV.IDAO
{
    public interface IPositionDAO
    {
        /// <summary>
        /// Obtiene todos los cargos creados de la empresa
        /// </summary>
        /// <param name="empresa"></param>
        /// <returns></returns>
        List<PositionVM> GetCompanyPositions(SesionVM empresa);
        /// <summary>
        /// Añade un listado de cargos a la empresa y retorna el listado de los insertados
        /// </summary>
        /// <param name="empresa"></param>
        /// <param name="positions"></param>
        /// <returns></returns>
        List<PositionVM> AddCompanyPositions(SesionVM empresa, List<PositionDTO> positions);
    }
}
