﻿using API.GV.DTO;
using API.Helpers.Commons;
using API.Helpers.VM;
using API.Helpers.VM.Consts;
using BusinessLogic.Interfaces.VM;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BusinessLogic.Implementation
{
    public class UserStatusLogBusinessCentralServicing : UserStatusLogBusiness
    {
        public override List<UserStatusLogCalculatedVM> GetUserStatusLogs(List<User> users, SesionVM Empresa, CompanyConfiguration companyConfiguration)
        {
            int paso = CommonHelper.calculateIterationIncrement(users.Count, 1);
            List<UserStatusLogCalculatedVM> logsProcessed = new List<UserStatusLogCalculatedVM>();
            List<string> ruts = users.Select(u => u.Identifier).ToList();
            List<UserStatusLog> result = new List<UserStatusLog>();
            for (int i = 0; i < users.Count; i += paso)
            {
                List<string> range = ruts.Skip(i).Take(paso).ToList();
                var statusLogs = companyConfiguration.UserStatusLogDAO.GetStatusLog(String.Join(',', range), Empresa);
                if (statusLogs != null)
                {
                    result.AddRange(statusLogs);
                }
            }

            foreach (UserStatusLog log in result)
            {
                UserStatusLogCalculatedVM logProcessed = new UserStatusLogCalculatedVM();
                logProcessed.Identifier = log.Identifier;
                List<ActivePeriodCalculatedVM> activePeriodsPreProcessed = new List<ActivePeriodCalculatedVM>();

                foreach (var period in log.ActivePeriods)
                {
                    ActivePeriodCalculatedVM activePeriod = new ActivePeriodCalculatedVM();
                    activePeriod.Starts = DateTimeHelper.parseFromGVFormat(period.From).Date;
                    activePeriod.Ends = DateTimeHelper.parseFromGVFormat(period.To).Date;
                    activePeriodsPreProcessed.Add(activePeriod);
                }

                if (activePeriodsPreProcessed != null && activePeriodsPreProcessed.Count() > 0)
                {
                    logProcessed.ActivePeriods = PeriodsHelper.cleanActivePeriods(activePeriodsPreProcessed);
                    logsProcessed.Add(logProcessed);
                }
                else
                {
                    FileLogHelper.log(LogConstants.general, LogConstants.get, "", $"USUARIO {log.Identifier} NO POSEE PERIODO ACTIVO", null, Empresa);
                }
            }

            return logsProcessed;
        }
    }
}
