using System;
using System.Collections.Generic;
using System.Text;

namespace API.Helpers.VM
{
    public class PositionVM
    {
        public bool? CARGO_PRIORITARIO { get; set; }
        public bool CRITICO { get; set; }
        public string DESCRIPCION_CARGO { get; set; }
        public string ESTADO_CARGO { get; set; }
        public string IDENTIFICADOR { get; set; }
    }
}
