using System;
using System.Collections.Generic;
using System.Globalization;

namespace API.BUK.DTO
{
    public class Employee : IComparable
    {
        public long person_id { get; set; }
        public long id { get; set; }
        public string picture_url { get; set; }
        public string first_name { get; set; }
        public string surname { get; set; }
        public string second_surname { get; set; }
        public string full_name { get; set; }
        public string rut { get; set; }
        public string nationality { get; set; }
        public string country_code { get; set; }
        public string civil_status { get; set; }
        public string email { get; set; }
        public string personal_email { get; set; }
        public string address { get; set; }
        public string city { get; set; }
        public string district { get; set; }
        public string region { get; set; }
        //public string office_phone { get; set; }
        //public string phone { get; set; }
        public string gender { get; set; }
        public string birthday { get; set; }
        public string university { get; set; }
        public string degree { get; set; }
        public string active_since { get; set; }
        public string status { get; set; }
        public string payment_method { get; set; }
        public string bank { get; set; }
        public string account_type { get; set; }
        public string account_number { get; set; }
        public string progressive_vacations_start { get; set; }
        public string private_role { get; set; }
        public string code_sheet { get; set; }
        public string health_company { get; set; }
        public string pension_regime { get; set; }
        public string pension_fund { get; set; }
        public Dictionary<string, object> custom_attributes { get; set; }
        public CurrentJob current_job { get; set; }
        public string companyRut { get; set; }
        int IComparable.CompareTo(object obj)
        {
            Employee e1 = this;
            Employee e2 = (Employee)obj;
            DateTime? end1 = null;
            DateTime? end2 = null;
            DateTime? start1 = null;
            DateTime? start2 = null;
            bool hasCurrentJob1 = e1.current_job != null;
            bool hasCurrentJob2 = e2.current_job != null;
            if (hasCurrentJob1 && e1.current_job.active_until != null)
            {
                end1 = DateTime.ParseExact(e1.current_job.active_until, "yyyy'-'MM'-'dd", CultureInfo.InvariantCulture);
            }
            if (hasCurrentJob2 && e2.current_job.active_until != null)
            {
                end2 = DateTime.ParseExact(e2.current_job.active_until, "yyyy'-'MM'-'dd", CultureInfo.InvariantCulture);
            }
            if (e1.active_since != null)
            {
                start1 = DateTime.ParseExact(e1.active_since, "yyyy'-'MM'-'dd", CultureInfo.InvariantCulture);
            }
            if (e2.active_since != null)
            {
                start2 = DateTime.ParseExact(e2.active_since, "yyyy'-'MM'-'dd", CultureInfo.InvariantCulture);
            }
            if (end1.HasValue && end2.HasValue)
            {
                if (end1.Value > end2.Value)
                {
                    return 1;
                }
                if (end1.Value < end2.Value)
                {
                    return -1;
                }
                if (start1.HasValue && start2.HasValue)
                {
                    if (start1.Value > start2.Value)
                    {
                        return 1;
                    }
                    if (start1.Value < start2.Value)
                    {
                        return -1;
                    }
                    return -1;
                }
                if (start1.HasValue)
                {
                    return 1;
                }
                return -1;
            }
            if (!end1.HasValue && !end2.HasValue)
            {
                if (start1.HasValue && start2.HasValue)
                {
                    if (start1.Value > start2.Value)
                    {
                        return 1;
                    }
                    if (start1.Value < start2.Value)
                    {
                        return -1;
                    }
                    return -1;
                }
                if (start1.HasValue)
                {
                    return 1;
                }
                return -1;
            }
            if (end1.HasValue)
            {
                if (hasCurrentJob2)
                {
                    return -1;
                }
                return 1;
            }
            if (hasCurrentJob1)
            {
                return 1;
            }
            return -1;
        }
    }
}
