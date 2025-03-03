using API.BUK.DTO;
using API.BUK.DTO.Consts;
using API.GV.DTO;
using API.Helpers.VM;
using API.Helpers.VM.Consts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace API.Helpers.Commons
{
    public static class CommonHelper
    {
        public static string rutToGVFormat(string rut)
        {
            return rut.Replace(".", String.Empty).Replace("-", String.Empty).ToUpper();
        }

        public static int calculateIterationIncrement(int usersCount, int totalDays)
        {
            int paso = usersCount;
            if (usersCount > OperationalConsts.MAXIMUN_AMOUNT_OF_REGISTERS_TO_REQUEST)
            {
                paso = OperationalConsts.MAXIMUN_AMOUNT_OF_USERS;

            }
            else if (usersCount * totalDays > OperationalConsts.MAXIMUN_AMOUNT_OF_REGISTERS_TO_REQUEST)
            {
                paso = OperationalConsts.MAXIMUN_AMOUNT_OF_REGISTERS_TO_REQUEST / usersCount * totalDays;
            }
            if (paso > OperationalConsts.MAXIMUN_AMOUNT_OF_USERS)
            {
                paso = OperationalConsts.MAXIMUN_AMOUNT_OF_USERS;
            }
            if (paso == 0)
            {
                paso++;
            }
            return paso;
        }

        public static List<Employee> cleanSheets(List<Employee> sheets, DateTime from, DateTime to)
        {
            List<Employee> selectedOnes = new List<Employee>();
            var ruts = sheets.GroupBy(s => s.rut).Select(grp => grp.First().rut);
            foreach (var rut in ruts)
            {
                List<Employee> withRut = sheets.FindAll(s => s.rut == rut);
                if (withRut.Count == 1)
                {
                    selectedOnes.Add(withRut[0]);
                }
                else
                {
                    withRut.Sort();
                    int indexSelected = 0;
                    for (int i = 0; i < withRut.Count; i++)
                    {
                        var employeeI = withRut[i];
                        DateTime? start = null;
                        DateTime? end = null;
                        if (employeeI.active_since != null)
                        {
                            start = DateTimeHelper.parseFromBUKFormat(employeeI.active_since);
                        }
                        if (employeeI.current_job != null && employeeI.current_job.active_until != null)
                        {
                            end = DateTimeHelper.parseFromBUKFormat(employeeI.current_job.active_until);
                        }
                        if (start.HasValue && end.HasValue)
                        {
                            if ((start.Value >= from && start.Value <= to)
                                || (end.Value >= from && end.Value <= to)
                                || (start.Value <= from && end.Value >= to))
                            {
                                indexSelected = i;
                            }
                        }
                        else if (end.HasValue)
                        {
                            if ((end.Value >= from && end.Value <= to)
                                || (end.Value >= to))
                            {
                                indexSelected = i;
                            }
                        }
                        else if (start.HasValue)
                        {
                            if ((start.Value >= from && start.Value <= to)
                                || (start.Value <= from))
                            {
                                indexSelected = i;
                            }
                        }
                    }
                    selectedOnes.Add(withRut[indexSelected]);
                }

            }
            return selectedOnes;
        }




    }
}
