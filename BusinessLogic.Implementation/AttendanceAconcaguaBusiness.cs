using API.BUK.DTO;
using API.Helpers.VM;
using BusinessLogic.Interfaces;
using BusinessLogic.Interfaces.VM;
using System.Collections.Generic;

namespace BusinessLogic.Implementation
{
    public class AttendanceAconcaguaBusiness : AttendanceBusiness, IAttendanceBusiness
    {
        public override void Sync(SesionVM Empresa, ProcessPeriod periodo, List<PeriodConfiguration> configs, CompanyConfiguration companyConfiguration)
        {
            // envío de datos de asistencia para empleados indefinidos
            base.Sync(Empresa, periodo, configs, companyConfiguration);

            // envío de datos de asistencia para empleados temporales
            SesionVM empresaNoIndefinidos = (SesionVM)Empresa.Clone();
            empresaNoIndefinidos.FechaCorte = 31;
            base.Sync(empresaNoIndefinidos, periodo, configs, companyConfiguration);
        }
    }
}
