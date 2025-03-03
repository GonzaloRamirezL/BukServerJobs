using API.GV.DTO.Filters;
using API.Helpers.VM;

namespace API.GV.IDAO
{
    public interface ICustomReportDAO
    {
        /// <summary>
        /// Devuelve un object con los calculos de un reporte personalizado de una empresa desde la api de GV
        /// </summary>        
        /// <returns>
        ///     Objeto que contiene los datos del reporte personalizado para los usuarios y el período solicitado
        /// </returns>
        object Get(CustomReportFilter filter, SesionVM empresa);
    }
}