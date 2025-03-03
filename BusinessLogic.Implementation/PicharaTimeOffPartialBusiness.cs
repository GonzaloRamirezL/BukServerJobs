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
    public class PicharaTimeOffPartialBusiness : TimeOffBusiness
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

            toUpsert.AddRange(this.buildTimeOffsToAdd(licencesToUpsert, users));
            toUpsert.AddRange(this.buildTimeOffsToAdd(paidLeavesToUpsert, users, subTypes));
            toUpsert.AddRange(this.buildVacations(vacationsToUpsert, users));

            List<TimeOff> toToDelete = timeOffs.FindAll(t => !this.matchCase(t, vacations, licencias, permissions, suspensions, users, gvTypes.Where(x => x.Id == t.TimeOffTypeId).First()));
            toToDelete = toToDelete.FindAll(t => this.isConsidered(t));

            toDelete = toToDelete.ConvertAll(buildToDelete);

            return (toUpsert, toDelete, usersToDeactivateBySuspension);
        }
        /// <summary>
        /// Procesa las licencias y devuelve las que deben agregarse a GV
        /// </summary>
        protected override List<(string, Absence)> processLicences(List<Licence> licencias, List<TimeOff> timeOffs, List<User> users, List<AbsenceType> subTypes, List<TimeOffType> gvTypes, CompanyConfiguration companyConfiguration, SesionVM Empresa, List<Employee> employees)
        {
            List<(string, Absence)> licencesToUpsert = new List<(string, Absence)>();
            Employee employee = new Employee();
            foreach (var item in licencias)
            {
                User usuario = users.FirstOrDefault(u => u.integrationCode == item.employee_id.ToString());
                if (usuario != null)
                {
                    string gvTypoId = "";
                    employee = employees.FirstOrDefault(x => x.person_id == item.employee_id);
                    if (matchType(item, subTypes, gvTypes, employee, out gvTypoId))
                    {
                        TimeOff match = timeOffs.FirstOrDefault(t => matchCase(t, item, gvTypoId, usuario));

                        if (match == null)
                        {
                            licencesToUpsert.Add((gvTypoId, item));
                        }
                    }
                    else
                    {
                        var typo = subTypes.FirstOrDefault(s => s.id == item.licence_type_id);
                        if (typo != null)
                        {
                            FileLogHelper.log(LogConstants.general, LogConstants.get, "", "CREANDO TIPO PERMISO: " + item.licence_type_id, null, Empresa);
                            try
                            {
                                TimeOffType newType = AddGVType(typo, Empresa, item.days_count, typo.with_pay, companyConfiguration);
                                gvTypes.Add(newType);
                                licencesToUpsert.Add((newType.Id, item));
                            }
                            catch (Exception)
                            {
                                FileLogHelper.log(LogConstants.general, LogConstants.get, "", "NO SE PUDO CREAR EL TIPO PERMISO: " + item.licence_type_id, null, Empresa);
                            }
                        }
                    }
                }
            }

            return licencesToUpsert;
        }
        /// <summary>
        /// Este override añade el permiso independiente si es parcial o no
        /// </summary>
        /// <param name="permissions"></param>
        /// <param name="timeOffs"></param>
        /// <param name="users"></param>
        /// <param name="subTypes"></param>
        /// <param name="gvTypes"></param>
        /// <param name="companyConfiguration"></param>
        /// <param name="Empresa"></param>
        /// <param name="employees"></param>
        /// <returns></returns>
        protected override List<(string, Absence)> processPermissions(List<Permission> permissions, List<TimeOff> timeOffs, List<User> users, List<AbsenceType> subTypes, List<TimeOffType> gvTypes, CompanyConfiguration companyConfiguration, SesionVM Empresa, List<Employee> employees)
        {
            List<(string, Absence)> leavesToUpsert = new List<(string, Absence)>();
            foreach (var item in permissions)
            {
                User usuario = users.FirstOrDefault(u => u.userCompanyIdentifier == item.employee_id.ToString() || u.integrationCode == item.employee_id.ToString());
                if (usuario != null)
                {
                    string gvTypoId = "";
                    Employee employee = employees.FirstOrDefault(x => x.person_id == item.employee_id);
                    if (this.matchType(item, subTypes, gvTypes, employee, out gvTypoId))
                    {
                        TimeOff match = timeOffs.FirstOrDefault(t => matchCase(t, item, gvTypoId, usuario));
                        if (match == null)
                        {

                            leavesToUpsert.Add((gvTypoId, item));
                        }
                    }
                    else
                    {
                        var typo = subTypes.FirstOrDefault(s => s.id == item.permission_type_id);
                        if (typo != null)
                        {
                            TimeOffType newType = AddGVType(typo, Empresa, item.days_count, typo.with_pay, companyConfiguration);
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

            return leavesToUpsert;
        }

        /// <summary>
        /// Independiente del subtipo de Licencia, se deben asignar como "Licencia Médica Estándar” (ID: -2), en caso de ser con goce de sueldo, se añade
        /// </summary>
        /// <param name="licence"></param>
        /// <param name="subTypes"></param>
        /// <param name="gvTypes"></param>
        /// <param name="employee"></param>
        /// <param name="gvTypoId"></param>
        /// <returns></returns>
        protected override bool matchType(Licence licence, List<AbsenceType> subTypes, List<TimeOffType> gvTypes, Employee employee, out string gvTypoId)
        {
            //Esto está actualizando el objeto del listado
            AbsenceType typo = subTypes.FirstOrDefault(s => s.id == licence.licence_type_id);

            gvTypoId = "";
            if (typo != null)
            {
                //Esto porque algunos textos de BUK traen un espacio al final
                if (typo.description.Trim().ToLower() == StandardTypes.BukLicencia.ToLower() && !typo.with_pay)
                {
                    typo.description = StandardTypes.GVLicencia;
                }
                //Aquí por si existe una licencia con goce de sueldo
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
        /// Construye el Permiso a insertar
        /// </summary>
        /// <param name="bukDataForTimeOffs"></param>
        /// <param name="users"></param>
        /// <param name="absenceTypes"></param>
        /// <returns></returns>
        protected List<TimeOffToAdd> buildTimeOffsToAdd(List<(string, Absence)> bukDataForTimeOffs, List<User> users, List<AbsenceType> absenceTypes)
        {

            List<TimeOffToAdd> timeOffsToAdd = new List<TimeOffToAdd>();
            foreach ((string, Absence) item in bukDataForTimeOffs)
            {
                User employee = users.FirstOrDefault(u => int.Parse(u.integrationCode) == item.Item2.employee_id);
                //Aquí se está volviendo a crear el objeto, ya que por alguna razón, al encontrarlo, no me dejaba acceder a permission_type_id
                string jsonObject = JsonConvert.SerializeObject(item.Item2);
                Absence absence = JsonConvert.DeserializeObject<Absence>(jsonObject);
                AbsenceType typo = absenceTypes.FirstOrDefault(at => at.id == absence.permission_type_id);
                if (employee != null)
                {
                    string payableString = typo != null && typo.with_pay ? BUKMacroAbsenceTypes.ConGoce : BUKMacroAbsenceTypes.SinGoce;
                    string absenceName = StandardTypes.BukMediaJornada + payableString;
                    if (typo != null && (absenceName.ToLower() == typo.description.Trim().ToLower()
                    || typo.description.Trim().ToLower().Contains(StandardTypes.BukMediaJornada.ToLower())))
                    {
                        //Se homologaban los permisos parciales por 4 Horas y 30 Minutos
                        TimeSpan tiempoParcial = new TimeSpan(4, 30, 0);
                        TimeOffToAdd licenseToAdd = new TimeOffToAdd();
                        licenseToAdd.Origin = TimeOffCreationConsts.Origin;
                        licenseToAdd.StartDate = DateTimeHelper.parseToGVFormat(DateTimeHelper.parseFromBUKFormat(item.Item2.start_date));
                        licenseToAdd.EndDate = DateTimeHelper.parseToGVFormat(DateTimeHelper.parseFromBUKFormat(item.Item2.end_date, true));
                        licenseToAdd.TimeOffTypeId = item.Item1;
                        licenseToAdd.UserIdentifier = employee.Identifier;
                        licenseToAdd.CreatedByIdentifier = TimeOffCreationConsts.CreatedByIdentifier;
                        licenseToAdd.Description = typo.description;
                        licenseToAdd.Hours = TimeSpanHelper.TimeSpanToHHMM(tiempoParcial);
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
        /// Añade un nuevo tipo de sub permiso ligado a la empresa en caso de no existir
        /// </summary>
        /// <param name="bukType"></param>
        /// <param name="empresa"></param>
        /// <param name="duration"></param>
        /// <param name="withpay"></param>
        /// <param name="companyConfiguration"></param>
        /// <returns></returns>
        protected override TimeOffType AddGVType(AbsenceType bukType, SesionVM empresa, double duration, bool? withpay, CompanyConfiguration companyConfiguration)
        {
            TimeOffType newType = new TimeOffType();
            newType.IsPayable = bukType.with_pay.ToString();
            string payableString = withpay.HasValue && withpay.Value ? BUKMacroAbsenceTypes.ConGoce : BUKMacroAbsenceTypes.SinGoce;
            string timeOffDescription = StandardTypes.BukMediaJornada + payableString;
            newType.Description = bukType.description.Trim();
            if (timeOffDescription.ToLower() == bukType.description.Trim().ToLower()
                || bukType.description.Trim().ToLower().Contains(StandardTypes.BukMediaJornada.ToLower()))
            {
                //Se homologaban los permisos parciales por 4 Horas y 30 Minutos
                TimeSpan tiempoParcial = new TimeSpan(4, 30, 0);
                newType.IsParcial = "true";
                newType.AllowsPunch = "true";
                newType.LengthInHours = TimeSpanHelper.TimeSpanToHHMM(tiempoParcial);
                newType.DiscountsWorkedHours = "false";
            }
            else
            {
                if (duration == 1)
                {
                    newType.IsParcial = "false";
                }
                else if (duration % 1 == 0)
                {
                    newType.IsParcial = "false";
                }
                else
                {
                    newType.IsParcial = "true";
                }
            }

            FileLogHelper.log(LogConstants.timeOff, LogConstants.get, "", "TRATANDO DE CREAR TIPO DE PERMISO: " + newType.Stringify(), null, empresa);
            return companyConfiguration.TimeOffDAO.AddType(newType, empresa);
        }
        /// <summary>
        /// Verifica si existe un permiso en GV, también verifica que el permiso contenga si es con goce de sueldo o no
        /// </summary>
        /// <param name="permission"></param>
        /// <param name="subTypes"></param>
        /// <param name="gvTypes"></param>
        /// <param name="employee"></param>
        /// <param name="gvTypoId"></param>
        /// <returns></returns>
        protected override bool matchType(Permission permission, List<AbsenceType> subTypes, List<TimeOffType> gvTypes, Employee employee, out string gvTypoId)
        {
            var typo = subTypes.FirstOrDefault(s => s.id == permission.permission_type_id);
            gvTypoId = "";
            if (typo != null)
            {
                if (permission.paid && typo.with_pay)
                {
                    typo.description = typo.description.Trim();
                    if (permission.paid)
                    {
                        if (!typo.description.Contains("con goce de sueldo"))
                        {
                            typo.description += " con goce de sueldo";
                        }
                    }
                    else
                    {
                        if (!typo.description.Contains("sin goce de sueldo"))
                        {
                            typo.description += " sin goce de sueldo";
                        }
                    }
                }
                else if (typo.description == StandardTypes.BukPermisoConGoce)
                {
                    typo.description = StandardTypes.GVPermisoConGoce;
                }
                else if (typo.description == StandardTypes.BukPermisoSinGoce)
                {
                    typo.description = StandardTypes.GVPermisoSinGoce;
                }
                var gvTypo = gvTypes.FirstOrDefault(g => g.Description == typo.description);
                if (gvTypo != null)
                {
                    gvTypoId = gvTypo.Id;
                    if (permission.paid && typo.with_pay)
                    {
                        return true;
                    }

                }

            }
            return false;
        }
    }
}
