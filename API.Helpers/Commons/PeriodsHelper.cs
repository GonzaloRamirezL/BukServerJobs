using API.BUK.DTO;
using API.Helpers.VM;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace API.Helpers.Commons
{
    public static class PeriodsHelper
    {
        public static List<ActivePeriodCalculatedVM> cleanActivePeriods(List<ActivePeriodCalculatedVM> preprocessedActivePeriods)
        {
            List<ActivePeriodCalculatedVM> processed = new List<ActivePeriodCalculatedVM>();
            DateTime previousPeriodStart = preprocessedActivePeriods[0].Starts;
            DateTime previousPeriodEnd = preprocessedActivePeriods[0].Ends;
            for (int i = 1; i < preprocessedActivePeriods.Count; i++)
            {
                if (preprocessedActivePeriods[i - 1].Ends == preprocessedActivePeriods[i].Starts)
                {
                    previousPeriodEnd = preprocessedActivePeriods[i].Ends;
                }
                else
                {
                    ActivePeriodCalculatedVM processedPeriod = new ActivePeriodCalculatedVM();
                    processedPeriod.Starts = previousPeriodStart;
                    processedPeriod.Ends = previousPeriodEnd;
                    processed.Add(processedPeriod);
                    previousPeriodStart = preprocessedActivePeriods[i].Starts;
                    previousPeriodEnd = preprocessedActivePeriods[i].Ends;
                }
            }
            ActivePeriodCalculatedVM last = new ActivePeriodCalculatedVM();
            last.Starts = previousPeriodStart;
            last.Ends = previousPeriodEnd;
            processed.Add(last);

            return processed;
        }

        public static List<ActivePeriodCalculatedVM> parseEmployeesToActivePeriods(List<Employee> employees)
        {

            List<ActivePeriodCalculatedVM> ranges = new List<ActivePeriodCalculatedVM>();
            foreach (var employee in employees)
            {
                DateTime start = default;
                DateTime end = default;
                if (!employee.active_since.IsNullOrEmpty())
                {
                    start = DateTimeHelper.parseFromBUKFormat(employee.active_since);
                }
                if (employee.current_job != null)
                {
                    if (!employee.current_job.active_until.IsNullOrEmpty())
                    {
                        end = DateTimeHelper.parseFromBUKFormat(employee.current_job.active_until);
                    }
                    else if (employee.current_job.periodicity == "diaria" && employee.status != "activo")
                    {
                        end = start;
                    }
                    else
                    {
                        end = DateTime.Today;
                    }
                }
                else
                {
                    end = start;
                }
                ranges.Add(new ActivePeriodCalculatedVM { Starts = start, Ends = end });
            }
            return ranges.OrderBy(r => r.Starts).ToList();

        }

        public static List<ActivePeriodCalculatedVM> MergeActivePeriods(List<ActivePeriodCalculatedVM> activePeriod, List<ActivePeriodCalculatedVM> employeesStatusLogs)
        {
            List<ActivePeriodCalculatedVM> mergedActivePeriods = new List<ActivePeriodCalculatedVM>();
            List<ActivePeriodCalculatedVM> preMergedAPs = new List<ActivePeriodCalculatedVM>();
            preMergedAPs.AddRange(activePeriod);
            preMergedAPs.AddRange(employeesStatusLogs);
            preMergedAPs = preMergedAPs.OrderBy(p => p.Starts).ToList();
           
            int openIndex = 0;
            for (int i = 1; i < preMergedAPs.Count; i++)
            {
                if (preMergedAPs[openIndex].Ends > preMergedAPs[i].Starts)
                {
                    if (preMergedAPs[openIndex].Ends < preMergedAPs[i].Ends)
                    {
                        preMergedAPs[openIndex].Ends = preMergedAPs[i].Ends;
                    }
                    
                }
                else
                {
                    mergedActivePeriods.Add(preMergedAPs[openIndex]);
                    openIndex = i;
                }
            }
            mergedActivePeriods.Add(preMergedAPs[openIndex]);
            return mergedActivePeriods;
        }
    }
}
