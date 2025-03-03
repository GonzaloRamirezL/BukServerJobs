using API.BUK.DTO;
using API.BUK.DTO.Consts;
using API.GV.DTO;
using API.GV.DTO.Consts;
using API.GV.DTO.Filters;
using API.Helpers.Commons;
using API.Helpers.VM.Consts;
using BusinessLogic.Interfaces.Paises.Colombia;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BusinessLogic.Implementation.Paises.Colombia
{
    public class TimeOffColombiaBusiness : TimeOffBusiness, ITimeOffColombiaBusiness
    {
        /// <summary>
        /// Determina si un permiso traido de GV y que no tien match con ninguno de BUK es de los tipos de permisos comprendidos en la integracion
        /// </summary>
        /// <param name="timeOff"></param>
        /// <returns></returns>
        protected override bool isConsidered(TimeOff timeOff)
        {
            return (timeOff.TimeOffTypeId == ColombianTimeOffTypeIds.Licencia)
                || (timeOff.TimeOffTypeId == ColombianTimeOffTypeIds.ConGoce)
                || (timeOff.TimeOffTypeId == ColombianTimeOffTypeIds.SinGoce)
                || (timeOff.TimeOffTypeId == ColombianTimeOffTypeIds.Vacaciones)
                || (timeOff.TimeOffTypeId == SuspensionsGVType.SuspensionTemporal)
                || (timeOff.TimeOffTypeId == SuspensionsGVType.ActoAutoridad)
                || (timeOff.TimeOffTypeId == SuspensionsGVType.SuspensionGeneral);
        }

        /// <summary>
        /// Determina si un tipo de Licencia ya existe en los tipos de permiso en GV
        /// </summary>
        protected override bool matchType(Licence licence, List<AbsenceType> subTypes, List<TimeOffType> gvTypes, Employee employee, out string gvTypoId)
        {
            var typo = subTypes.FirstOrDefault(s => s.id == licence.licence_type_id);

            if ((typo == null && string.IsNullOrEmpty(licence.type) && licence.licence_type_id == 0))
            {
                typo = new AbsenceType();
                typo.description = StandardTypes.GVLicencia;
            }

            if (licence.type == ColombiaStandardTypes.BukDiaFamilia)
            {
                typo.description = ColombiaStandardTypes.GVRemunerada;
            }

            gvTypoId = "";
            switch (licence.type)
            {
                case StandardTypes.BukLicencia:
                    typo.description = StandardTypes.GVLicencia;
                    break;

                case ColombiaStandardTypes.BukMaternidadPaternidad:
                    string gender = (employee == null) ? "M" : employee.gender;
                    typo.description = (gender == "M") ? ColombiaStandardTypes.GVPaternidad : ColombiaStandardTypes.GVMaternidad;
                    break;

                case ColombiaStandardTypes.BukRemunerada:
                    typo.description = ColombiaStandardTypes.GVRemunerada;
                    break;

                case ColombiaStandardTypes.BukLuto:
                    typo.description = ColombiaStandardTypes.GVLuto;
                    break;

                case ColombiaStandardTypes.BukDomestica:
                    typo.description = ColombiaStandardTypes.GVDomestica;
                    break;

                case ColombiaStandardTypes.BukNoRemunerada:
                    typo.description = ColombiaStandardTypes.GVNoRemunerada;
                    break;

                case ColombiaStandardTypes.BukSuspension:
                    typo.description = ColombiaStandardTypes.GVSuspension;
                    break;

                case ColombiaStandardTypes.BukIncapacidad:
                    typo.description = ColombiaStandardTypes.GVIncapacidad;
                    break;

                case ColombiaStandardTypes.BukDomingo:
                    typo.description = ColombiaStandardTypes.GVDomingo;
                    break;

                case ColombiaStandardTypes.BukFestivo:
                    typo.description = ColombiaStandardTypes.GVFestivo;
                    break;

                case ColombiaStandardTypes.BukAborto:
                    typo.description = ColombiaStandardTypes.GVAborto;
                    break;
            }

            if (typo != null)
            {
                var gvTypo = gvTypes.FirstOrDefault(g => g.Description == typo.description);
                if (gvTypo != null)
                {
                    gvTypoId = gvTypo.Id;
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Determina si un tipo de Permiso ya existe en los tipos de permiso en GV
        /// </summary>
        protected override bool matchType(Permission permission, List<AbsenceType> subTypes, List<TimeOffType> gvTypes, Employee employee, out string gvTypoId)
        {
            var typo = subTypes.FirstOrDefault(s => s.id == permission.permission_type_id);
            gvTypoId = "";
            if (typo != null)
            {
                if (permission.paid != typo.with_pay)
                {
                    if (permission.paid)
                    {
                        typo.description += " con goce";
                    }
                    else
                    {
                        typo.description += " sin goce";
                    }

                }
                else if (typo.description == StandardTypes.BukPermisoConGoceColombia)
                {
                    typo.description = StandardTypes.GVPermisoConGoceColombia;
                }
                else if (typo.description == StandardTypes.BukPermisoSinGoceColombia)
                {
                    typo.description = StandardTypes.GVPermisoSinGoceColombia;
                }

                // New matched permissions
                if (!string.IsNullOrEmpty(permission.matched_type))
                {
                    if (permission.matched_type == ColombiaStandardTypes.BukDomingo)
                    {
                        typo.description = ColombiaStandardTypes.GVDomingo;
                    }
                    else if (permission.matched_type == ColombiaStandardTypes.BukFestivo)
                    {
                        typo.description = ColombiaStandardTypes.GVFestivo;
                    }
                }

                var gvTypo = gvTypes.FirstOrDefault(g => g.Description == typo.description);
                if (gvTypo != null)
                {
                    gvTypoId = gvTypo.Id;
                    if (permission.paid == typo.with_pay)
                    {
                        return true;
                    }

                }

            }
            return false;
        }

        /// <summary>
        /// Determina si un permiso de GeoVictoria tiene algun match con algun permiso de BUK
        /// </summary>
        /// <param name="timeOff"></param>
        /// <param name="vacations"></param>
        /// <param name="absences"></param>
        /// <param name="users"></param>
        /// <returns></returns>
        protected override bool matchCase(TimeOff timeOff, List<Vacation> vacations, List<Licence> licences, List<Permission> permissions, List<Suspension> suspensions, List<User> users, TimeOffType gvType)
        {
            User user = users.FirstOrDefault(u => timeOff.UserIdentifier == u.Identifier);
            if (user == null)
            {
                return false;
            }

            var integrationCodesList = user.integrationCode.Split(',');

            List<Permission> permissionsCandidates = permissions.FindAll(a => (user.userCompanyIdentifier == a.employee_id.ToString() || integrationCodesList.Contains(a.employee_id.ToString()))
                && (DateTimeHelper.parseFromGVFormat(timeOff.Starts) == DateTimeHelper.parseFromBUKFormat(a.start_date))
                && (DateTimeHelper.parseFromGVFormat(timeOff.Ends) == DateTimeHelper.parseFromBUKFormat(a.end_date, true)));
            foreach (var item in permissionsCandidates)
            {
                if (gvType.IsParcial == "False" && item.paid && gvType.IsPayable == "True")
                {
                    return true;
                }

                if (gvType.IsParcial == "False" && !item.paid && gvType.IsPayable == "False")
                {
                    return true;
                }
            }
            List<Licence> licencesCandidates = licences.FindAll(a => (user.userCompanyIdentifier == a.employee_id.ToString() || integrationCodesList.Contains(a.employee_id.ToString()))
                && (DateTimeHelper.parseFromGVFormat(timeOff.Starts) == DateTimeHelper.parseFromBUKFormat(a.start_date))
                && (DateTimeHelper.parseFromGVFormat(timeOff.Ends) == DateTimeHelper.parseFromBUKFormat(a.end_date, true)));
            if (!licencesCandidates.IsNullOrEmpty() &&
                (timeOff.TimeOffTypeId == ColombianTimeOffTypeIds.Licencia
                || timeOff.TimeOffTypeId == ColombianTimeOffTypeIds.SinGoce
                || timeOff.TimeOffTypeId == ColombianTimeOffTypeIds.ConGoce
                || timeOff.TimeOffTypeId == ColombianTimeOffTypeIds.MaternidadPaternidad
                || timeOff.TimeOffTypeId == ColombianTimeOffTypeIds.Incapacidad
                || timeOff.TimeOffTypeId == ColombianTimeOffTypeIds.CalamidadDomestica
                || timeOff.TimeOffTypeId == ColombianTimeOffTypeIds.PermisosOficialesTransitorios
                || timeOff.TimeOffTypeId == ColombianTimeOffTypeIds.Luto
                || timeOff.TimeOffTypeId == ColombianTimeOffTypeIds.Aborto
                || timeOff.TimeOffTypeId == ColombianTimeOffTypeIds.DomingoCompensatorio
                || timeOff.TimeOffTypeId == ColombianTimeOffTypeIds.FeriadoCompensatorio))
            {
                return true;
            }
            List<Suspension> suspensionsCandidates = suspensions.FindAll(a => (user.userCompanyIdentifier == a.employee_id.ToString() || integrationCodesList.Contains(a.employee_id.ToString()))
                && (DateTimeHelper.parseFromGVFormat(timeOff.Starts) == DateTimeHelper.parseFromBUKFormat(a.start_date))
                && (DateTimeHelper.parseFromGVFormat(timeOff.Ends) == DateTimeHelper.parseFromBUKFormat(a.end_date, true)));
            if (!suspensionsCandidates.IsNullOrEmpty() &&
                (timeOff.TimeOffTypeId == SuspensionsGVType.SuspensionTemporal
                || timeOff.TimeOffTypeId == SuspensionsGVType.ActoAutoridad
                || timeOff.TimeOffTypeId == SuspensionsGVType.SuspensionGeneral))
            {
                return true;
            }

            List<Vacation> vacationsCandidates = vacations.FindAll(v => (user.userCompanyIdentifier == v.employee_id.ToString() || integrationCodesList.Contains(v.employee_id.ToString()))
                && (DateTimeHelper.parseFromGVFormat(timeOff.Starts) == DateTimeHelper.parseFromBUKFormat(v.start_date))
                && (DateTimeHelper.parseFromGVFormat(timeOff.Ends) == DateTimeHelper.parseFromBUKFormat(v.end_date, true)));
            return !vacationsCandidates.IsNullOrEmpty() && timeOff.TimeOffTypeId == ColombianTimeOffTypeIds.Vacaciones;

        }
       
        /// <summary>
        /// Construye las vacaciones a agregar en GV
        /// </summary>

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
                    vacationToAdd.TimeOffTypeId = ColombianTimeOffTypeIds.Vacaciones;
                    vacationToAdd.UserIdentifier = employee.Identifier;
                    vacationToAdd.CreatedByIdentifier = TimeOffCreationConsts.CreatedByIdentifier;
                    timeOffsToAdd.Add(vacationToAdd);
                }
            }
            return timeOffsToAdd;
        }



    }
}
