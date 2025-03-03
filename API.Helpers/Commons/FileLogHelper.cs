using API.BUK.DTO;
using API.GV.DTO;
using API.GV.DTO.Filters;
using API.Helpers.VM;
using API.Helpers.VM.Consts;
using System;
using System.Threading;

namespace API.Helpers.Commons
{
    public static class FileLogHelper
    {
        private static readonly string ID = Guid.NewGuid().ToString();
        public static Mutex m_Mutex = new Mutex(false, @"Global\BukParterIntegrationMutex");
        public static void log(string partitionKey, string action, string userIdentifier, string message, object data, SesionVM empresa)
        {
            string rowKey = DateTime.Now.Ticks.ToString();
            string tableName = ("buknativa" + empresa.Empresa).ToLower().Replace("-", String.Empty).Replace(" ", String.Empty);

            if (string.IsNullOrEmpty(partitionKey))
            {
                partitionKey = LogConstants.general;
            }

            string finalMessage = "";
            bool concatMessage = true;
            if (partitionKey == LogConstants.user)
            {
                User user = (data != null) ? (User)data : new User();

                if (!string.IsNullOrEmpty(user.Name) && !string.IsNullOrEmpty(user.Identifier))
                {
                    finalMessage = user.ToString();
                }
            }
            else if (partitionKey == LogConstants.timeOff)
            {
                TimeOffToAdd timeOff = new TimeOffToAdd();

                try
                {
                    timeOff = (data != null) ? (TimeOffToAdd)data : new TimeOffToAdd();
                }
                catch (Exception ex)
                {
                    TimeOffToDelete timeOffToDelete = (TimeOffToDelete)data;
                    timeOff.StartDate = timeOffToDelete.Start;
                    timeOff.EndDate = timeOffToDelete.End;
                    timeOff.UserIdentifier = timeOffToDelete.UserIdentifier;
                    timeOff.Description = timeOffToDelete.Description;
                    timeOff.TimeOffTypeId = timeOffToDelete.TypeIdentifier;
                }

                if (!string.IsNullOrEmpty(timeOff.StartDate) && !string.IsNullOrEmpty(timeOff.TimeOffTypeId))
                {
                    finalMessage = timeOff.ToString();
                }
            }
            else if (partitionKey == LogConstants.hnt)
            {
                NonWorkedHours hnt = (data != null) ? (NonWorkedHours)data : new NonWorkedHours();

                if (hnt.employee_id != 0 && hnt.type_id != 0)
                {
                    finalMessage = hnt.ToString();
                }
            }
            else if (partitionKey == LogConstants.hhee)
            {
                Overtime hhee = (data != null) ? (Overtime)data : new Overtime();

                if (hhee.employee_id != 0 && hhee.type_id != 0)
                {
                    finalMessage = hhee.ToString();
                }
            }
            else if (partitionKey == LogConstants.absences)
            {
                AbsenceToAdd absence = (data != null) ? (AbsenceToAdd)data : new AbsenceToAdd();

                if (!string.IsNullOrEmpty(absence.start_date) && absence.absence_type_id != 0)
                {
                    finalMessage = absence.ToString();
                }
            }
            else if (partitionKey == LogConstants.cutOffDate)
            {
                FechasProcesamientoVM dates = (FechasProcesamientoVM)data;
                finalMessage = dates.ToString();
            }
            else if (partitionKey == LogConstants.period)
            {
                ProcessPeriod period = (ProcessPeriod)data;
                finalMessage = period.ToString();
            }
            else
            {
                finalMessage = message;
                concatMessage = false;
            }

            if (concatMessage && !string.IsNullOrEmpty(message))
            {
                finalMessage += " " + message;
            }

            IntegrationLog execLog = new IntegrationLog
            {
                PartitionKey = partitionKey,
                RowKey = rowKey,
                user_identifier = userIdentifier,
                action = action,
                message = finalMessage
            };

            upsertRecord(execLog, tableName);
            Console.WriteLine(execLog.ToString());
        }

        public static void UserLog(string partitionKey, string action, string userIdentifier, string message, object data, SesionVM empresa, string companyId)
        {
            string rowKey = DateTime.Now.Ticks.ToString();
            string tableName = "userslog";
            UserIntegrationLog execLog = new UserIntegrationLog
            {
                PartitionKey = partitionKey,
                RowKey = rowKey,
                user_identifier = userIdentifier,
                action = action,
                message = message,
                company_identifier = companyId
            };

            UpsertRecordUser(execLog, tableName);
            Console.WriteLine(execLog.ToString());
        }
        private static void upsertRecord(IntegrationLog integrationLog, string tableName)
        {
            new TableStorageHelper(Storage.IntegracionesLogs).Upsert(integrationLog, tableName);
        }
        private static void UpsertRecordUser(UserIntegrationLog userLog, string tableName)
        {
            new TableStorageHelper(Storage.IntegracionesLogs).Upsert(userLog, tableName);
        }
    }
}
