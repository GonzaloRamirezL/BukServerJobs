using API.BUK.DTO;
using API.GV.DTO;
using API.Helpers.Commons;
using API.Helpers.VM;
using API.Helpers.VM.Consts;
using BusinessLogic.Interfaces.VM;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BusinessLogic.Implementation
{
    public class TimeOffFridayHalfDayBusiness : TimeOffBusiness
    {
        private const string TIMEOFF_NAME_FRIDAY = "Permiso viernes";

        protected override List<(string, Absence)> processPermissions(List<Permission> sinGoce, List<TimeOff> timeOffs, List<User> users, List<AbsenceType> subTypes, List<TimeOffType> gvTypes, CompanyConfiguration companyConfiguration, SesionVM Empresa, List<Employee> employees)
        {
            List<(string, Absence)> leavesToUpsert = new List<(string, Absence)>();

            string timeOffTypeIdFriday = gvTypes.FirstOrDefault(x => x.Description == TIMEOFF_NAME_FRIDAY).Id;

            foreach (var item in sinGoce)
            {
                User usuario = users.FirstOrDefault(u => u.userCompanyIdentifier == item.employee_id.ToString() || u.integrationCode == item.employee_id.ToString());
                if (usuario != null)
                {
                    if (DateTimeHelper.IsFridayTimeOff(item))
                    {
                        TimeOff matchFriday = timeOffs.FirstOrDefault(t => matchCase(t, item, timeOffTypeIdFriday, usuario));
                        if (matchFriday == null)
                        {
                            leavesToUpsert.Add((timeOffTypeIdFriday, item));
                        }
                    }
                    else if (item.days_count % 1 == 0)
                    {
                        string gvTypoId = "";
                        Employee employee = employees.FirstOrDefault(x => x.person_id == item.employee_id);
                        if (this.matchType(item, subTypes, gvTypes, employee, out gvTypoId))
                        {
                            TimeOff match = timeOffs.FirstOrDefault(t => matchCase(t, item, gvTypoId, usuario));
                            if (match == null)
                            {
                                var currentSubtype = subTypes.Where(x => x.id == item.permission_type_id).First();
                                var currentGvType = gvTypes.Where(x => x.Description == currentSubtype.description).First();
                                Boolean.TryParse(currentGvType.IsParcial, out var is_parcial_permission);
                                if (!is_parcial_permission)
                                {
                                    leavesToUpsert.Add((gvTypoId, item));
                                }
                            }
                        }
                        else
                        {
                            var typo = subTypes.FirstOrDefault(s => s.id == item.permission_type_id);
                            if (typo != null)
                            {
                                TimeOffType newType = AddGVType(typo, Empresa, item.days_count, null, companyConfiguration);
                                gvTypes.Add(newType);
                                leavesToUpsert.Add((newType.Id, item));
                            }
                            else if (!gvTypoId.IsNullOrEmpty())
                            {
                                FileLogHelper.log(LogConstants.general, LogConstants.get, "", "CREANDO TIPO PERMISO: " + item.permission_type_id, null, Empresa);
                                try
                                {
                                    TimeOffType newType = AddGVType(typo, Empresa, item.days_count, item.paid, companyConfiguration);
                                    gvTypes.Add(newType);
                                    leavesToUpsert.Add((newType.Id, item));
                                }
                                catch (Exception)
                                {
                                    FileLogHelper.log(LogConstants.general, LogConstants.get, "", "NO SE PUDO CREAR EL TIPO PERMISO: " + item.permission_type_id, null, Empresa);
                                }
                            }
                        }
                    }
                }
            }
            return leavesToUpsert;
        }
    }
}
