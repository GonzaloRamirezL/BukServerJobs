using API.BUK.DTO;
using API.BUK.DTO.Consts;
using API.GV.DTO;
using API.GV.DTO.Consts;
using API.GV.DTO.Filters;
using API.Helpers.Commons;
using API.Helpers.VM.Consts;
using BusinessLogic.Interfaces.Paises.Peru;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BusinessLogic.Implementation.Paises.Peru
{
    public class TimeOffPeruBusiness: TimeOffBusiness, ITimeOffPeruBusiness
    {
        protected override bool matchType(Licence licence, List<AbsenceType> subTypes, List<TimeOffType> gvTypes, Employee employee, out string gvTypoId)
        {
            var typo = subTypes.FirstOrDefault(s => s.id == licence.licence_type_id);

            gvTypoId = "";
            if (typo != null)
            {
                if (typo.description == StandardTypes.BukLicencia)
                {
                    typo.description = StandardTypes.GVLicenciaMedicaPeru;
                }

                var gvTypo = gvTypes.FirstOrDefault(g => g.Description == typo.description);
                if (gvTypo != null)
                {
                    gvTypoId = gvTypo.Id;
                    return true;
                }

            }
            return false;
        }

        protected override List<Vacation> processVacations(List<Vacation> vacations, List<TimeOff> timeOffs, List<User> users)
        {
            List<Vacation> vacationsToUpsert = new List<Vacation>();
            foreach (var item in vacations)
            {
                User usuario = users.FirstOrDefault(u => u.userCompanyIdentifier == item.employee_id.ToString() || u.integrationCode == item.employee_id.ToString());
                if (usuario != null)
                {
                    TimeOff match = timeOffs.FirstOrDefault(t => matchCase(t, item, TimeOffTypeIds.VacacionesPeru, usuario));
                    if (match == null)
                    {
                        vacationsToUpsert.Add(item);
                    }
                }
            }
            return vacationsToUpsert;
        }

        protected override List<TimeOffToAdd> buildVacations(List<Vacation> vacations, List<User> users)
        {
            List<TimeOffToAdd> timeOffsToAdd = new List<TimeOffToAdd>();
            foreach (Vacation vacation in vacations)
            {
                User employee = users.FirstOrDefault(u => int.Parse(u.integrationCode) == vacation.employee_id);
                if (employee != null)
                {
                    TimeOffToAdd vacationToAdd = new TimeOffToAdd();
                    vacationToAdd.Origin = TimeOffCreationConsts.Origin;
                    vacationToAdd.StartDate = DateTimeHelper.parseToGVFormat(DateTimeHelper.parseFromBUKFormat(vacation.start_date));
                    vacationToAdd.EndDate = DateTimeHelper.parseToGVFormat(DateTimeHelper.parseFromBUKFormat(vacation.end_date, true));
                    vacationToAdd.TimeOffTypeId = TimeOffTypeIds.VacacionesPeru;
                    vacationToAdd.UserIdentifier = employee.Identifier;
                    vacationToAdd.CreatedByIdentifier = TimeOffCreationConsts.CreatedByIdentifier;
                    timeOffsToAdd.Add(vacationToAdd);
                }
            }
            return timeOffsToAdd;
        }
    }
}
