using System;
using System.Collections.Generic;
using System.Text;

namespace API.Helpers.VM.Consts
{
    public static class OperationalConsts
    {
        public static string kickOffDate = "20200201000000";
        public const int STANDARD_QUERY_PERIOD = 1;
        public const int MAXIMUN_REGISTERS_PER_PAGE = 100;
        public const int MAXIMUN_AMOUNT_OF_REGISTERS_TO_REQUEST = 1500;
        public const int MAXIMUN_AMOUNT_OF_USERS = 200;
        public const int MAXIMUN_PARALLEL_PROCESS = 2;
        public const int MAXIMUN_AMOUNT_OF_DAYS = 60;
    }

    public static class TimeOffCreationConsts
    {
        public const string Origin = "APIBUK";
        public const string CreatedByIdentifier = "BUK-PARTNER-API";
    }

    public static class FileReaderConsts
    {
        public const int CompanyNamePosition = 0;
        public const int BUKURLPosition = 1;
        public const int BUKTokenPosition = 2;
        public const int GVURLPosition = 3;
        public const int GVTokenPosition = 4;
        public const int CountryPosition = 5;
        public const int ProcessEndDayPosition = 6;
        public const int AbsenceDelayPosition = 7;
        public const int OvertimeDelayPosition = 8;
        public const int NonWorkedHoursDelayPosition = 9;
        public const int SyncArt22Position = 10;
        public const int AbsenceDelayTypePosition = 11;
        public const int OvertimeDelayTypePosition = 12;
        public const int NonWorkedHoursDelayTypePosition = 13;
        //Esta es la posición en el array
        public const int JobPosition = 14;
        public const string SyncArt22Yes = "SI";
        public const string DelayInDays = "DIAS";
        public const string DelayInMonths = "MESES";
        public const int HasAbsenceDelayTypePosition = 12;
        public const int HasOvertimeDelayTypePosition = 13;
        public const int HasNonWorkedHoursDelayTypePosition = 14;
        //Este es para el conteo del total parametros
        public const int HasJobPosition = 15;
        public const int SyncEmail = 16;

    }

    public static class KpiTypeRelatedTo
    {
        public const string Employee = "Employee";
        public const string Company = "Empresa";
    }
}
