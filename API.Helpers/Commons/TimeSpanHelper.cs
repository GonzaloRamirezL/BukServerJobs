using System;
using System.Collections.Generic;
using System.Text;

namespace API.Helpers.Commons
{
    public static class TimeSpanHelper
    {
        public static string TimeSpanToHHMM(TimeSpan ts)
        {
            string ret = "";
            string h, m;
            int hr, dhr, totHR;

            m = ts.Minutes.ToString("00");


            if (ts.Days > 0)
            {
                hr = ts.Hours;
                dhr = ts.Days * 24;
                totHR = hr + dhr;
            }
            else
            {
                totHR = ts.Hours;
            }
            h = totHR.ToString("00");



            ret = h + ":" + m;

            return ret;
        }

        public static TimeSpan HHmmToTimeSpan(string hora)
        {
            //el string debe venir como "hh:mm"
            try
            {
                if (hora == null)
                    return new TimeSpan(0, 0, 0);
                if (hora == "--:--")
                    return new TimeSpan(0, 0, 0);

                string[] data = hora.Split(':');
                int hh = Convert.ToInt32(data[0]);
                int mm = Convert.ToInt32(data[1]);

                TimeSpan t = new TimeSpan(hh, mm, 0);
                return t;
            }
            catch
            {
                return new TimeSpan(0, 0, 0);
            }
        }

        public static TimeSpan RedondearHoras(TimeSpan entrada)
        {
            TimeSpan salida = entrada - TimeSpan.FromMinutes(entrada.Minutes);
            if (entrada.Minutes >= 15 && entrada.Minutes < 45)
            {
                salida += TimeSpan.FromMinutes(30);
            }
            else if (entrada.Minutes >= 45)
            {
                salida += TimeSpan.FromMinutes(60);
            }
            return salida;
        }

        public static string ImprimirHoraDecimal(TimeSpan ts, bool truncate)
        {
            return TimeSpanToDouble(ts, truncate).ToString();
        }

        public static double TimeSpanToDouble(TimeSpan ts, bool truncate)
        {
            if (truncate)
            {
                return Math.Truncate(100 * (ts.TotalSeconds / 3600.0)) / 100;
            }
            else
            {
                return Math.Round(ts.TotalSeconds / 3600.0, 2);
            }
        }
    }
}
