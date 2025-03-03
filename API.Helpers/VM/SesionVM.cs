using System;
using System.Collections.Generic;
using System.Text;

namespace API.Helpers.VM
{
    public class SesionVM
    {
        public string GvKey { get; set; }
        public string GvUrl { get; set; }
        public string Empresa { get; set; }
        public string Url { get; set; }
        public string Url2 { get; set; }
        public string BukKey { get; set; }
        public string BukKey2 { get; set; }
        public string Pais { get; set; }
        public int FechaCorte { get; set; }
        public int DesfaseInasistencias { get; set; }
        public int DesfaseHorasExtras { get; set; }
        public int DesfaseHorasNoTrabajadas { get; set; }
        public int? SincronizarCorreo { get; set; }
        public bool EnviaHNT { get; set; }
        public bool EnviaHHEE { get; set; }
        public bool EnviaAusencia { get; set; }
        public bool SincronizaArticulos22 { get; set; }
        public string TipoDesfaseInasistencias { get; set; }
        public string TipoDesfaseHorasExtras { get; set; }
        public string TipoDesfaseHorasNoTrabajadas { get; set; }
        public string CargoEmpleo { get; set; }

        public object Clone()
        {
            return this.MemberwiseClone();
        }
    }
}
