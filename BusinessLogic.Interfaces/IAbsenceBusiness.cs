using API.BUK.DTO;
using API.Helpers.VM;
using BusinessLogic.Interfaces.VM;
using System;
using System.Collections.Generic;
using System.Text;

namespace BusinessLogic.Interfaces
{
    public interface IAbsenceBusiness
    {
        /// <summary>
        /// Devuelve el listado del macrotipo absence(absence,licence,permission) de BUK en el periodo solicitado
        /// </summary>        
        /// <returns>
        ///     El listado de absences del periodo solicitado
        /// </returns>
        List<Absence> GetAbsencesBUK(DateTime startDate, DateTime endDate, SesionVM sesionActiva, CompanyConfiguration companyConfiguration);

        /// <summary>
        /// Devuelve el listado de subtipos dado un macrotipo de BUK 
        /// </summary>        
        /// <returns>
        ///     El listado de absencetype 
        /// </returns>
        List<AbsenceType> GetSubTypes(SesionVM Empresa, string macroType, CompanyConfiguration companyConfiguration);

        /// <summary>
        /// Busca el tipo de absence estandar en BUK
        /// </summary>        
        /// <returns>
        ///     ID de tipo de absence estandar
        /// </returns>
        int FindStandardAbsenceId(SesionVM Empresa, CompanyConfiguration companyConfiguration);

        /// <summary>
        /// Agrega una absence nueva a través de la api de BUK
        /// </summary>        
        /// <returns>
        ///     No devuelve nada
        /// </returns>
        void AddAbsence(AbsenceToAdd absence, SesionVM empresa, CompanyConfiguration companyConfiguration);

        /// <summary>
        /// Elimina todas las absences de tipo absence para un listado de usuarios en un periodo determinado a través de la api de BUK
        /// </summary>        
        /// <returns>
        ///     Si fue satisfactoria o no la operación
        /// </returns>
        bool DeleteAbsence(AbsencesToDelete absences, SesionVM empresa, CompanyConfiguration companyConfiguration);
        
        /// <summary>
        /// Envía una colección de ausencias para su alta en Buk.
        /// </summary>
        void AddAbsences(List<AbsenceToAdd> absences, SesionVM empresa, CompanyConfiguration companyConfiguration);
    }
}
