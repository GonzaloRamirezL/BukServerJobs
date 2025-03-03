using API.BUK.DTO;
using API.Helpers.VM;
using BusinessLogic.Interfaces.VM;
using System;
using System.Collections.Generic;
using System.Text;

namespace BusinessLogic.Interfaces
{
    public interface ILicenceBusiness
    { 
        /// <summary>
        /// Devuelve las licencias desde BUK en el periodo solicitado
        /// </summary>        
        /// <returns>Listado de licence</returns>
        List<Licence> GetLicences(DateTime startDate, DateTime endDate, SesionVM sesionActiva, CompanyConfiguration companyConfiguration);

        /// <summary>
        /// Obtiene una colección con los tipos de licencias en buk.
        /// </summary>
        List<LicenceType> GetLicenseTypes(SesionVM sesionActiva, CompanyConfiguration companyConfiguration);

        /// <summary>
        /// Elimina licencias en buk para el rango de fechas y los usuarios indicados por parámetro
        /// </summary>
        void DeleteLicences(SesionVM session, CompanyConfiguration companyConfiguration, List<int> userIdsToDelete, FechasProcesamientoVM fechas);

        /// <summary>
        /// Envia una colección de licencias para su alta en buk
        /// </summary>
        void SendLicences(SesionVM session, CompanyConfiguration companyConfiguration, List<LicenceToSend> licences);
    }
}
