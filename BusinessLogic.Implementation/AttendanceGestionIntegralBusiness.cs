using BusinessLogic.Interfaces;
using API.BUK.DTO;
using API.Helpers.VM;
using BusinessLogic.Interfaces.VM;
using System.Collections.Generic;
using System;
using API.Helpers.Commons;
using API.Helpers.VM.Consts;
using System.Linq;
using API.GV.DTO;
using System.Text.RegularExpressions;
using API.GV.DTO.Consts;

namespace BusinessLogic.Implementation
{
    public class AttendanceGestionIntegralBusiness : AttendanceBusiness, IAttendanceBusiness
    {
        public const int GESTION_GROUP2_START_DAY = 23;
        public const int GESTION_GROUP2_END__DAY = 24;
        public override Attendance GetAttendance(SesionVM Empresa, List<User> users, DateTime startDate, DateTime endDate, CompanyConfiguration companyConfiguration)
        {
            List<GroupVM> groups = companyConfiguration.GroupBusiness.GetCompanyGroups(Empresa);
            List<string> userIdentifiers = users.Select(u => u.Identifier).ToList();

            int step = CommonHelper.calculateIterationIncrement(userIdentifiers.Count, (endDate - startDate).Days);
            FileLogHelper.log(LogConstants.general, LogConstants.get, "", "USUARIOS A BUSCAR EN LIBRO DE ASISTENCIA: " + userIdentifiers.Count, null, Empresa);
            FileLogHelper.log(LogConstants.general, LogConstants.get, "", "ITERACIONES CON USUARIOS Y LIBRO: " + (OperationalConsts.MAXIMUN_AMOUNT_OF_REGISTERS_TO_REQUEST / step), null, Empresa);

            Attendance result = new Attendance();
            try
            {
                for (int i = 0; i <= userIdentifiers.Count; i += step)
                {
                    GroupVM group = groups.FirstOrDefault(a => (a.CostCenter == users[i].GroupIdentifier) && a.Enabled);
                    List<string> iterUsers = userIdentifiers.Skip(i).Take(step).ToList();
                    
                    string concat = String.Join(',', iterUsers);
                    if (group == null)
                    {
                        continue;
                    }

                    if (group.CustomColumn1 == GestionIntegralGroups.GESTION_GROUP1)
                    {
                        (startDate, endDate) = DateTimeHelper.calculateGroupDates(startDate, endDate, GestionIntegralGroups.GESTION_GROUP1, GESTION_GROUP2_START_DAY, GESTION_GROUP2_END__DAY);

                        if (i == 0)
                        {
                            result = companyConfiguration.AttendanceDAO.Get(new API.GV.DTO.Filters.AttendanceFilter { UserIds = concat, StartDate = DateTimeHelper.parseToGVFormat(startDate), EndDate = DateTimeHelper.parseToGVFormat(endDate) }, Empresa);
                        }
                        else if (!CollectionsHelper.IsNullOrEmpty<string>(iterUsers))
                        {

                            result.Users.AddRange(companyConfiguration.AttendanceDAO.Get(new API.GV.DTO.Filters.AttendanceFilter { UserIds = concat, StartDate = DateTimeHelper.parseToGVFormat(startDate), EndDate = DateTimeHelper.parseToGVFormat(endDate) }, Empresa).Users);
                        }
                    }
                    else if (group.CustomColumn1 == GestionIntegralGroups.GESTION_GROUP2)
                    {
                        (startDate, endDate) = DateTimeHelper.calculateGroupDates(startDate, endDate, GestionIntegralGroups.GESTION_GROUP2, GESTION_GROUP2_START_DAY, GESTION_GROUP2_END__DAY);

                        if (i == 0)
                        {
                            result = companyConfiguration.AttendanceDAO.Get(new API.GV.DTO.Filters.AttendanceFilter { UserIds = concat, StartDate = DateTimeHelper.parseToGVFormat(startDate), EndDate = DateTimeHelper.parseToGVFormat(endDate) }, Empresa);
                        }
                        else if (!CollectionsHelper.IsNullOrEmpty<string>(iterUsers))
                        {

                            result.Users.AddRange(companyConfiguration.AttendanceDAO.Get(new API.GV.DTO.Filters.AttendanceFilter { UserIds = concat, StartDate = DateTimeHelper.parseToGVFormat(startDate), EndDate = DateTimeHelper.parseToGVFormat(endDate) }, Empresa).Users);
                        }
                    }
                    else
                    {
                        continue;
                    }

                }
            }
            catch (Exception ex)
            {
                InsightHelper.logException(ex, Empresa.Empresa);
                FileLogHelper.log(LogConstants.general, LogConstants.get, "", "ERROR AL OBTENER LIBRO DE ASISTENCIA DESDE GV", null, Empresa);
                throw new Exception("Incomplete data from GV");
            }

            return result;
        }
    }
}
