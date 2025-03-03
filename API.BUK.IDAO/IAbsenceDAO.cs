using API.BUK.DTO;
using API.BUK.DTO.Filters;
using API.Helpers.VM;
using System;
using System.Collections.Generic;
using System.Text;

namespace API.BUK.IDAO
{
    public interface IAbsenceDAO : IBUKDAO
    {
        PaginatedResponse<AbsenceType> GetTypes(PaginatedFilter filter, SesionVM empresa, string macroType);

        PaginatedResponse<Absence> GetAbsences(PaginatedAbsenceFilter filter, SesionVM empresa);

        void AddAbsence(AbsenceToAdd absence, SesionVM empresa);

        public bool DeleteAbsence(AbsencesToDelete absences, SesionVM empresa);
    }
}
