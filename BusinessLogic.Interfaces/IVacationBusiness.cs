using API.BUK.DTO;
using API.Helpers.VM;
using API.Helpers.VM.Consts;
using BusinessLogic.Interfaces.VM;
using System;
using System.Collections.Generic;
using System.Text;

namespace BusinessLogic.Interfaces
{
    public interface IVacationBusiness
    {
        /// <summary>
        /// Devuelve las vacaciones desde BUK en el periodo solicitado
        /// </summary>        
        /// <returns>
        ///     Listado de vacaciones
        /// </returns>
        List<Vacation> GetVacations(DateTime startDate, DateTime endDate, SesionVM Empresa, CompanyConfiguration companyConfiguration, int registersPerPage = OperationalConsts.MAXIMUN_REGISTERS_PER_PAGE);

        /// <summary>
        /// Elimina las vacaciones para los usuarios recibidos dentro del rango de fechas
        /// </summary>
        void DeleteVacations(SesionVM session, CompanyConfiguration companyConfiguration, List<Vacation> vacationsToDelete);

        /// <summary>
        /// Envia periodos de vacaciones a registrar en Buk
        /// </summary>
        void SendVacations(SesionVM session, CompanyConfiguration companyConfiguration, List<VacationToSend> vacationsToSend);
    }
}
