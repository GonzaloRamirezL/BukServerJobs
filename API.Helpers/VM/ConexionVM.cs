using System;
using System.Collections.Generic;
using System.Text;

namespace API.Helpers.VM
{
    public class ConexionVM
    {
        public string GvKey { get; set; }
        public string GvUrl { get; set; }
        public List<CompanyVM> Empresas { get; set; }
    }
}
