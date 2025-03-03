using API.BUK.DTO;
using API.BUK.DTO.Consts;
using API.BUK.DTO.Filters;
using API.Helpers.Commons;
using API.Helpers.VM;
using API.Helpers.VM.Consts;
using BusinessLogic.Interfaces;
using BusinessLogic.Interfaces.VM;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BusinessLogic.Implementation
{
    public class AbsenceBusiness : IAbsenceBusiness
    {
        public List<Absence> GetAbsencesBUK(DateTime startDate, DateTime endDate, SesionVM sesionActiva, CompanyConfiguration companyConfiguration)
        {
            List<Absence> absences = new List<Absence>();
            try
            {
                var absencesResponse = companyConfiguration.AbsenceDAO.GetAbsences(new PaginatedAbsenceFilter
                {
                    from = DateTimeHelper.parseToBUKFormat(startDate),
                    to = DateTimeHelper.parseToBUKFormat(endDate),
                    page_size = OperationalConsts.MAXIMUN_REGISTERS_PER_PAGE
                }, sesionActiva);
                if (!CollectionsHelper.IsNullOrEmpty<Absence>(absencesResponse.data))
                {
                    absences.AddRange(absencesResponse.data);
                }
                while (absencesResponse.pagination != null && !string.IsNullOrWhiteSpace(absencesResponse.pagination.next))
                {
                    absencesResponse = companyConfiguration.AbsenceDAO.GetNext<Absence>(absencesResponse.pagination.next, sesionActiva.Url, sesionActiva.BukKey, sesionActiva);
                    if (!CollectionsHelper.IsNullOrEmpty<Absence>(absencesResponse.data))
                    {
                        absences.AddRange(absencesResponse.data);
                    }
                }
            }
            catch (Exception ex)
            {
                InsightHelper.logException(ex, sesionActiva.Empresa);
                FileLogHelper.log(LogConstants.absences, LogConstants.get, "", "ERROR AL TRAER AUSENCIAS DESDE BUK", null, sesionActiva);
                throw new Exception("Incomplete data from BUK");
            }

            return absences;
        }

        public List<AbsenceType> GetSubTypes(SesionVM Empresa, string macroType, CompanyConfiguration companyConfiguration)
        {
            List<AbsenceType> subTypes = new List<AbsenceType>();
            try
            {
                var subTypesResponse = companyConfiguration.AbsenceDAO.GetTypes(new PaginatedEmployeeFilter()
                {
                    page_size = OperationalConsts.MAXIMUN_REGISTERS_PER_PAGE
                }, Empresa, macroType);

                if (subTypesResponse != null)
                {
                    object _lock = new object();
                    if (!CollectionsHelper.IsNullOrEmpty<API.BUK.DTO.AbsenceType>(subTypesResponse.data))
                    {
                        subTypes.AddRange(subTypesResponse.data);
                    }
                    while (subTypesResponse.pagination != null && !string.IsNullOrWhiteSpace(subTypesResponse.pagination.next))
                    {
                        subTypesResponse = companyConfiguration.AbsenceDAO.GetNext<API.BUK.DTO.AbsenceType>(subTypesResponse.pagination.next, Empresa.Url, Empresa.BukKey, Empresa);
                        if (!CollectionsHelper.IsNullOrEmpty<API.BUK.DTO.AbsenceType>(subTypesResponse.data))
                        {
                            subTypes.AddRange(subTypesResponse.data);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                InsightHelper.logException(ex, Empresa.Empresa);
                FileLogHelper.log(LogConstants.absences, LogConstants.get, "", "ERROR AL TRAER TIPOS DE AUSENCIAS DESDE BUK", null, Empresa);
                throw new Exception("Incomplete data from BUK");
            }

            return subTypes;
        }

        public int FindStandardAbsenceId(SesionVM Empresa, CompanyConfiguration companyConfiguration)
        {
            List<API.BUK.DTO.AbsenceType> subTypes = GetSubTypes(Empresa, BUKMacroAbsenceTypes.Inasistencia, companyConfiguration);
            subTypes = subTypes.OrderBy(s => s.id).ToList();
            var standardAbsence = subTypes.FirstOrDefault(s => s.kind == BUKStandardAbsence.kind && s.name == BUKStandardAbsence.name
                                                            && s.with_pay == BUKStandardAbsence.with_pay && s.description == BUKStandardAbsence.description
                                                            && s.code == BUKStandardAbsence.code);
            if (standardAbsence != null)
            {
                return standardAbsence.id;
            }
            return AbsenceTypeID.WithoutStandardAbsence;
        }

        public void AddAbsences(List<AbsenceToAdd> absences, SesionVM empresa, CompanyConfiguration companyConfiguration)
        {
            foreach (AbsenceToAdd absence in absences)
            {
                AddAbsence(absence, empresa, companyConfiguration);
            }
        }

        public void AddAbsence(AbsenceToAdd absence, SesionVM empresa, CompanyConfiguration companyConfiguration)
        {
            companyConfiguration.AbsenceDAO.AddAbsence(absence, empresa);
        }

        public bool DeleteAbsence(AbsencesToDelete absences, SesionVM empresa, CompanyConfiguration companyConfiguration)
        {
            return companyConfiguration.AbsenceDAO.DeleteAbsence(absences, empresa);
        }
    }
}
