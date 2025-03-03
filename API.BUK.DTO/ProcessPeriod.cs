using System;
using System.Collections.Generic;
using System.Text;

namespace API.BUK.DTO
{
    public class ProcessPeriod
    {
        public int id { get; set; }
        public string month { get; set; }
        public string status { get; set; }

        public override string ToString()
        {
            return "ID: " + id +
                " Mes: " + month +
                " Status: " + status;
        }
    }
}
