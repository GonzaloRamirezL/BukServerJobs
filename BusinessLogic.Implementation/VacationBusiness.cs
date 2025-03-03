using API.BUK.DTO;
using API.BUK.DTO.Filters;
using API.BUK.IDAO;
using API.Helpers;
using API.Helpers.Commons;
using API.Helpers.VM;
using API.Helpers.VM.Consts;
using BusinessLogic.Interfaces;
using BusinessLogic.Interfaces.VM;
using System;
using System.Collections.Generic;
using System.Text;

namespace BusinessLogic.Implementation
{
    public class VacationBusiness  :  IVacationBusiness
    {
        public void DeleteVacations(SesionVM session, CompanyConfiguration companyConfiguration, List<Vacation> vacationsToDelete)
        {
            try
            {
                foreach (Vacation vacation in vacationsToDelete)
                {
                    companyConfiguration.VacationDAO.DeleteVacation(session, vacation);
                }
            }
            catch (Exception ex)
            {
                InsightHelper.logException(ex, session.Empresa);
                throw new Exception("Vacation could not be deleted");
            }
        }

        public List<Vacation> GetVacations(DateTime startDate, DateTime endDate, SesionVM Empresa, CompanyConfiguration companyConfiguration, int registersPerPage = OperationalConsts.MAXIMUN_REGISTERS_PER_PAGE)
        {
            List<Vacation> vacations = new List<Vacation>();
            try
            {
                var vacationsResponse = companyConfiguration.VacationDAO.Get(new PaginatedVacationFilter
                {
                    date = DateTimeHelper.parseToBUKFormat(startDate),
                    end_date = DateTimeHelper.parseToBUKFormat(endDate),
                    page_size = registersPerPage
                }, Empresa.Url, Empresa.BukKey, Empresa);

                if (!CollectionsHelper.IsNullOrEmpty<Vacation>(vacationsResponse.data))
                {
                    vacations.AddRange(vacationsResponse.data);
                }

                while (vacationsResponse.pagination != null && !string.IsNullOrWhiteSpace(vacationsResponse.pagination.next))
                {
                    vacationsResponse = companyConfiguration.VacationDAO.GetNext<Vacation>(vacationsResponse.pagination.next, Empresa.Url, Empresa.BukKey, Empresa);
                    if (!CollectionsHelper.IsNullOrEmpty<Vacation>(vacationsResponse.data))
                    {
                        vacations.AddRange(vacationsResponse.data);
                    }
                }
            }
            catch (Exception ex)
            {
                InsightHelper.logException(ex, Empresa.Empresa);
                FileLogHelper.log(LogConstants.general, LogConstants.get, "", "ERROR AL OBTENER VACACIONES DESDE BUK", null, Empresa);
                throw new Exception("Incomplete data from BUK");
            }

            return vacations;
        }

        public void SendVacations(SesionVM session, CompanyConfiguration companyConfiguration, List<VacationToSend> vacationsToSend)
        {
            try
            {
                foreach (VacationToSend vacation in vacationsToSend)
                {
                    companyConfiguration.VacationDAO.SendVacation(session, vacation);
                }
            }
            catch (Exception ex)
            {
                InsightHelper.logException(ex, session.Empresa);
                throw new Exception("Vacation could not be created");
            }
        }
    }
}
