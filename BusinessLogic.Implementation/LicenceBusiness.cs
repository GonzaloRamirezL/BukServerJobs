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
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessLogic.Implementation
{
    public class LicenceBusiness :  ILicenceBusiness
    {
        /// <summary>
        /// Obtiene una lista de licencias para el rango de fechas especificado.
        /// </summary>
        public List<Licence> GetLicences(DateTime startDate, DateTime endDate, SesionVM sesionActiva, CompanyConfiguration companyConfiguration)
        {
            List<Licence> licences = new List<Licence>();
            object lockLicence = new object();
            try
            {
                var licencesResponse = companyConfiguration.LicenceDAO.GetLicences(new PaginatedAbsenceFilter
                {
                    from = DateTimeHelper.parseToBUKFormat(startDate),
                    to = DateTimeHelper.parseToBUKFormat(endDate),
                    page_size = OperationalConsts.MAXIMUN_REGISTERS_PER_PAGE
                }, sesionActiva);
                if (!CollectionsHelper.IsNullOrEmpty<Licence>(licencesResponse.data))
                {
                    licences.AddRange(licencesResponse.data);
                }

                while (licencesResponse.pagination != null && !string.IsNullOrWhiteSpace(licencesResponse.pagination.next))
                {
                    licencesResponse = companyConfiguration.LicenceDAO.GetNext<Licence>(licencesResponse.pagination.next, sesionActiva.Url, sesionActiva.BukKey, sesionActiva);
                    if (!CollectionsHelper.IsNullOrEmpty<Licence>(licencesResponse.data))
                    {
                        licences.AddRange(licencesResponse.data);
                    }
                }
            }
            catch (Exception ex)
            {
                InsightHelper.logException(ex, sesionActiva.Empresa);
                FileLogHelper.log(LogConstants.timeOff, LogConstants.error_add, "", "Error al obtener licencias desde BUK " + ex.ToString(), null, sesionActiva);
                throw new Exception("Incomplete data from BUK");
            }
            licences = licences.FindAll(l => l.days_count % 1 == 0);
            Parallel.ForEach(licences, l => {

                if (l.end_date == null)
                {
                    DateTime endDate = DateTimeHelper.parseFromBUKFormat(l.start_date, true);
                    if (l.days_count >= 1)
                    {
                        endDate = endDate.AddDays(l.days_count - 1);
                    }
                    l.end_date = DateTimeHelper.parseToBUKFormat(endDate);


                }

            });

            return licences;
        }

        public List<LicenceType> GetLicenseTypes(SesionVM sesionActiva, CompanyConfiguration companyConfiguration)
        {
            List<LicenceType> permissionsTypes = new List<LicenceType>();
            try
            {
                var permissionResponse = companyConfiguration.LicenceDAO.GetLicenceTypes(
                    new PaginatedAbsenceFilter { page_size = OperationalConsts.MAXIMUN_REGISTERS_PER_PAGE }, sesionActiva);

                if (!CollectionsHelper.IsNullOrEmpty<LicenceType>(permissionResponse.data))
                {
                    permissionsTypes.AddRange(permissionResponse.data);
                }
                while (permissionResponse.pagination != null && !string.IsNullOrWhiteSpace(permissionResponse.pagination.next))
                {
                    permissionResponse = companyConfiguration.PermissionDAO.GetNext<LicenceType>(permissionResponse.pagination.next, sesionActiva.Url, sesionActiva.BukKey, sesionActiva);
                    if (!CollectionsHelper.IsNullOrEmpty<LicenceType>(permissionResponse.data))
                    {
                        permissionsTypes.AddRange(permissionResponse.data);
                    }
                }
            }
            catch (Exception ex)
            {
                InsightHelper.logException(ex, sesionActiva.Empresa);
                FileLogHelper.log(LogConstants.timeOff, LogConstants.error_add, "", "Error al obtener tipos de permiso desde BUK " + ex.ToString(), null, sesionActiva);
                throw new Exception("Incomplete data from BUK");
            }

            return permissionsTypes;
        }

        /// <summary>
        /// Elimina licencias en buk para los usuarios indicados por parámetro
        /// </summary>
        public void DeleteLicences(SesionVM session, CompanyConfiguration companyConfiguration, List<int> userIdsToDelete, FechasProcesamientoVM fechas)
        {
            try
            {
                if (userIdsToDelete.Any())
                {
                    companyConfiguration.LicenceDAO.DeleteLicence(session, userIdsToDelete, fechas);
                }
            }
            catch (Exception ex)
            {
                InsightHelper.logException(ex, session.Empresa);
                throw new Exception("License could not be deleted");
            }
        }

        public void SendLicences(SesionVM session, CompanyConfiguration companyConfiguration, List<LicenceToSend> licenses)
        {
            try
            {
                foreach (LicenceToSend license in licenses)
                {
                    companyConfiguration.LicenceDAO.SendLicence(session, license);
                }
            }
            catch (Exception ex)
            {
                InsightHelper.logException(ex, session.Empresa);
                throw new Exception("License could not be created");
            }
        }
    }
}
