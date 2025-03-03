using API.BUK.DTO;
using API.BUK.DTO.Consts;
using API.GV.DTO;
using API.GV.DTO.Consts;
using API.GV.DTO.Filters;
using API.Helpers.Commons;
using API.Helpers.VM;
using API.Helpers.VM.Consts;
using BusinessLogic.Interfaces;
using BusinessLogic.Interfaces.VM;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessLogic.Implementation
{
    public class CasaIdeasTimeOffPartialBusiness : TimeOffBusiness
    {
        /// <summary>
        /// Este metodo está sobre escrito, debido a que el base tiene por separado conGoce y sinGoce sin razón, ya que usan el mismo método
        /// </summary>
        /// <param name="licencias"></param>
        /// <param name="permissions"></param>
        /// <param name="vacations"></param>
        /// <param name="timeOffs"></param>
        /// <param name="suspensions"></param>
        /// <param name="users"></param>
        /// <param name="Empresa"></param>
        /// <param name="subTypes"></param>
        /// <param name="gvTypes"></param>
        /// <param name="startDate"></param>
        /// <param name="endDate"></param>
        /// <param name="companyConfiguration"></param>
        /// <param name="employees"></param>
        /// <returns></returns>
        protected override (List<TimeOffToAdd>, List<TimeOffToDelete>, List<User>) prepareChanges(List<Licence> licencias, List<Permission> permissions, List<Vacation> vacations, List<TimeOff> timeOffs, List<Suspension> suspensions, List<User> users, SesionVM Empresa, List<AbsenceType> subTypes, List<TimeOffType> gvTypes, DateTime startDate, DateTime endDate, CompanyConfiguration companyConfiguration, List<Employee> employees)
        {
            List<TimeOffToAdd> toUpsert = new List<TimeOffToAdd>();
            List<TimeOffToDelete> toDelete = new List<TimeOffToDelete>();

            suspensions = suspensions.FindAll(s => s.suspension_type != SuspensionsType.ReduccionJornada);

            Parallel.ForEach(permissions, cg => { FileLogHelper.log(LogConstants.general, LogConstants.get, "", "Permisos :" + cg.Stringify(), null, Empresa); });
            Parallel.ForEach(licencias, l => { FileLogHelper.log(LogConstants.general, LogConstants.get, "", "Permiso Licencia :" + l.Stringify(), null, Empresa); });
            Parallel.ForEach(vacations, v => { FileLogHelper.log(LogConstants.general, LogConstants.get, "", "Permiso Vacacion :" + v.Stringify(), null, Empresa); });
            Parallel.ForEach(suspensions, s => { FileLogHelper.log(LogConstants.general, LogConstants.get, "", "Suspension :" + s.Stringify(), null, Empresa); });

            List<(string, Absence)> licencesToUpsert = processLicences(licencias, timeOffs, users, subTypes, gvTypes, companyConfiguration, Empresa, employees);
            List<(string, Absence)> paidLeavesToUpsert = processPermissions(permissions, timeOffs, users, subTypes, gvTypes, companyConfiguration, Empresa, employees);
            List<Vacation> vacationsToUpsert = processVacations(vacations, timeOffs, users);
            List<User> usersToDeactivateBySuspension = processSuspensionsToDeactivateUsers(suspensions, users);

            toUpsert.AddRange(this.buildTimeOffsToAdd(licencesToUpsert, users, gvTypes, Empresa, companyConfiguration));
            toUpsert.AddRange(this.buildTimeOffsToAdd(paidLeavesToUpsert, users, gvTypes, Empresa, companyConfiguration));
            toUpsert.AddRange(this.buildVacations(vacationsToUpsert, users));

            List<TimeOff> toToDelete = timeOffs.FindAll(t => !users.Exists(u => u.Identifier == t.UserIdentifier));
            toToDelete = toToDelete.FindAll(t => this.isConsidered(t));

            toDelete = toToDelete.ConvertAll(buildToDelete);

            return (toUpsert, toDelete, usersToDeactivateBySuspension);
        }
        /// <summary>
        /// Construye el Permiso a insertar, también verifica que no sea parcial, si lo es, intenta crear uno nuevo y asignarlo correctamente
        /// </summary>
        /// <param name="bukDataForTimeOffs"></param>
        /// <param name="users"></param>
        /// <param name="gvTypes"></param>
        /// <param name="Empresa"></param>
        /// <param name="companyConfiguration"></param>
        /// <returns></returns>
        protected List<TimeOffToAdd> buildTimeOffsToAdd(List<(string, Absence)> bukDataForTimeOffs, List<User> users, List<TimeOffType> gvTypes, SesionVM Empresa, CompanyConfiguration companyConfiguration)
        {

            List<TimeOffToAdd> timeOffsToAdd = new List<TimeOffToAdd>();
            foreach ((string, Absence) item in bukDataForTimeOffs)
            {
                User employee = users.FirstOrDefault(u => int.Parse(u.integrationCode) == item.Item2.employee_id);
                //Aquí se está volviendo a crear el objeto, ya que por alguna razón, al encontrarlo, no me dejaba acceder a permission_type_id
                string jsonObject = JsonConvert.SerializeObject(item.Item2);
                Absence absence = JsonConvert.DeserializeObject<Absence>(jsonObject);
                if (employee != null)
                {
                    if (absence.day_percent < 1)
                    {
                        TimeSpan fullTimeDiscount = CasaIdeasHomologacionPermisosParciales.FullTime;
                        TimeSpan partTimeDiscount = CasaIdeasHomologacionPermisosParciales.PartTime;
                        TimeSpan tiempoParcial;
                        if (employee.Custom1 != null && employee.Custom1 == JornadaTrabajo.FullTime)
                        {
                            tiempoParcial = fullTimeDiscount;
                        }
                        else if (employee.Custom1 != null && employee.Custom1 == JornadaTrabajo.PartTime)
                        {
                            tiempoParcial = partTimeDiscount;
                        }
                        else
                        {
                            tiempoParcial = fullTimeDiscount;
                        }
                        TimeOffType timeOffName = gvTypes.FirstOrDefault(gvt => gvt.Id == item.Item1);
                        TimeOffType parcialType = gvTypes.Find(pt => pt.Description == timeOffName.Description && !pt.IsParcial.IsNullOrEmpty() && bool.Parse(pt.IsParcial));
                        if (parcialType == null)
                        {
                            TimeOffType newType = new TimeOffType
                            {
                                IsParcial = "True",
                                AllowsPunch = "True",
                                LengthInHours = TimeSpanHelper.TimeSpanToHHMM(tiempoParcial),
                                DiscountsWorkedHours = "False",
                                Description = timeOffName.Description,
                                IsPayable = timeOffName.IsPayable
                            };
                            parcialType = this.AddParcialType(Empresa, newType, companyConfiguration);
                        }
                        TimeOffToAdd licenseToAdd = new TimeOffToAdd();
                        licenseToAdd.Origin = TimeOffCreationConsts.Origin;
                        licenseToAdd.StartDate = DateTimeHelper.parseToGVFormat(DateTimeHelper.parseFromBUKFormat(item.Item2.start_date));
                        licenseToAdd.EndDate = DateTimeHelper.parseToGVFormat(DateTimeHelper.parseFromBUKFormat(item.Item2.end_date, true));
                        licenseToAdd.TimeOffTypeId = parcialType.Id;
                        licenseToAdd.UserIdentifier = employee.Identifier;
                        licenseToAdd.CreatedByIdentifier = TimeOffCreationConsts.CreatedByIdentifier;
                        licenseToAdd.Description = item.Item2.type;
                        licenseToAdd.Hours = parcialType.LengthInHours;
                        timeOffsToAdd.Add(licenseToAdd);
                    }
                    else
                    {
                        TimeOffToAdd licenseToAdd = new TimeOffToAdd();
                        licenseToAdd.Origin = TimeOffCreationConsts.Origin;
                        licenseToAdd.StartDate = DateTimeHelper.parseToGVFormat(DateTimeHelper.parseFromBUKFormat(item.Item2.start_date));
                        licenseToAdd.EndDate = DateTimeHelper.parseToGVFormat(DateTimeHelper.parseFromBUKFormat(item.Item2.end_date, true));
                        licenseToAdd.StartTime = "00:00";
                        licenseToAdd.StartTime = "23:59";
                        licenseToAdd.TimeOffTypeId = item.Item1;
                        licenseToAdd.UserIdentifier = employee.Identifier;
                        licenseToAdd.CreatedByIdentifier = TimeOffCreationConsts.CreatedByIdentifier;
                        licenseToAdd.Description = item.Item2.type;
                        timeOffsToAdd.Add(licenseToAdd);
                    }
                }
            }

            return timeOffsToAdd;
        }
        /// <summary>
        /// Inserta los permisos, si las horas están vacias, valida que el permiso comprenda el día completo
        /// </summary>
        /// <param name="timeOffsToAdd"></param>
        /// <param name="Empresa"></param>
        /// <param name="companyConfiguration"></param>
        /// <returns></returns>
        protected override List<TimeOffToAdd> AddTimeOffs(List<TimeOffToAdd> timeOffsToAdd, SesionVM Empresa, CompanyConfiguration companyConfiguration)
        {
            List<TimeOffToAdd> conErrores = new List<TimeOffToAdd>();
            ParallelOptions pOptions = new ParallelOptions();
            pOptions.MaxDegreeOfParallelism = 2;
            int total = timeOffsToAdd.Count;
            FileLogHelper.log(LogConstants.timeOff, LogConstants.get, "", "ENVIANDO OPERACIONES A GV (PERMISOS) PARA UN TOTAL DE " + total, null, Empresa);
            int current = 0;
            object lockCurrent = new object();
            Parallel.ForEach(timeOffsToAdd, pOptions, timeOff =>
            {
                lock (lockCurrent)
                {
                    current++;
                }

                if (timeOff.Hours.IsNullOrEmpty())
                {
                    string endHours = timeOff.EndDate.Substring(8);
                    if ((timeOff.StartDate == timeOff.EndDate) && (endHours == "000000"))
                    {
                        timeOff.EndDate = timeOff.EndDate.Substring(0, 8) + "235959";
                    }
                }

                if (!companyConfiguration.TimeOffDAO.Add(timeOff, Empresa))
                {
                    FileLogHelper.log(LogConstants.timeOff, LogConstants.error_add, "", " Error al enviar permiso ", timeOff, Empresa);
                    lock (lockCurrent)
                    {
                        conErrores.Add(timeOff);
                    }
                }
                else
                {
                    FileLogHelper.log(LogConstants.timeOff, LogConstants.add, "", "", timeOff, Empresa);
                }
            });

            return conErrores;
        }

        /// <summary>
        /// Agrega un tipo de permiso nuevo en GV
        /// </summary>
        protected virtual TimeOffType AddParcialType(SesionVM empresa, TimeOffType newType, CompanyConfiguration companyConfiguration)
        {
            FileLogHelper.log(LogConstants.timeOff, LogConstants.get, "", "TRATANDO DE CREAR TIPO DE PERMISO: " + newType.Stringify(), null, empresa);
            return companyConfiguration.TimeOffDAO.AddType(newType, empresa);
        }
    }
}
