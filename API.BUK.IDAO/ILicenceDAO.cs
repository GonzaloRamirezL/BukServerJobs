using API.BUK.DTO;
using API.BUK.DTO.Filters;
using API.GV.DTO;
using API.Helpers.VM;
using System;
using System.Collections.Generic;
using System.Text;

namespace API.BUK.IDAO
{
    public interface ILicenceDAO : IBUKDAO
    {
        /// <summary>
        /// Obtiene una lista de licencias consultando al endpoint de Buk.
        /// </summary>
        PaginatedResponse<Licence> GetLicences(PaginatedAbsenceFilter filter, SesionVM empresa);

        /// <summary>
        /// Elimina licencias para los usuarios recibidos por parámetro, mediante el endpoint de Buk correspondiente.
        /// </summary>
        bool DeleteLicence(SesionVM session, List<int> userIdsToDelete, FechasProcesamientoVM fechas);

        /// <summary>
        /// Elimina un permiso en Buk
        /// </summary>
        ApiResponse SendLicence(SesionVM session, LicenceToSend license);

        /// <summary>
        /// Obtiene una colección con los tipos de licencia de buk
        /// </summary>
        PaginatedResponse<LicenceType> GetLicenceTypes(PaginatedAbsenceFilter filter, SesionVM empresa);
    }
}
