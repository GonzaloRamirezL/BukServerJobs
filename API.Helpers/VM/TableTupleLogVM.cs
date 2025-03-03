using System;
using System.Collections.Generic;
using System.Text;

namespace API.Helpers.VM
{
    public class TableTupleLogVM
    {
        public string ID_SISTEMA_EXTERNO { get; set; }
        public string ID_EMPRESA { get; set; }
        public string Modulo { get; set; }
        public string Resultado { get; set; }
        public string LogObject { get; set; }
        public string Mensaje { get; set; }
    }
}
