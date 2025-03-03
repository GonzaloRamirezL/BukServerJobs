using System;
using System.Collections.Generic;
using System.Text;

namespace API.BUK.DTO.Consts
{
    public class Enums
    {
    }

    public static class AbsenceMacroType
    {
        public const string Licencia = "licence";
        public const string Ausencia = "absence";
        public const string SinGoce = "leave";
        public const string ConGoce = "paid_leave";

    }

    public static class AbsenceTypeID
    {

        public const int Ausencia = 2;
        public const int WithoutStandardAbsence = 0;


    }

    public static class NonWorkedHoursType
    {
        public const int General = 1;
        public const int Adelantos = 2;

    }

    public static class OvertimeIdentifiers
    {
        public const int percent85 = 5;
        public const int percent75 = 3;
        public const int percent100 = 1;
        public const int percent150 = 6;
        public const int percentminus55 = 7;
        public const int percentminus46 = 8;
        public const int percentminus70 = 9;
        public const int percent30 = 10;
        public const int percent50 = 2;

    }

    public static class EmployeeStatus
    {
        public const string Activo = "activo";
        public const string Desactivo = "inactivo";
        public const string Pendiente = "pendiente";
    }

    public static class ColombiaSurChargeIdentifiers
    {
        public const int HoraExtraDiurnaOrdinarias = 1;
        public const int HorasExtraNocturnaOrdinarias = 2;
        public const int HorasExtraDiurnaDominical = 3;
        public const int HorasExtraNocturnaDominical = 5;
        public const int HorasExtraDiurnaFestiva = 4;
        public const int HorasExtraNocturnaFestiva = 6;
        public const int RecargoNocturnoOrdinario = 7;
        public const int RecargoDiurnoDominicalNoCompensado = 8;
        public const int RecargoNocturnoDominicalNoCompensado = 10;
        public const int RecargoDiurnoFestivoNoCompensado = 9;
        public const int RecargoNocturnoFestivoNoCompensado = 11;
        public const int RecargoDiurnoDominicalCompensado = 12;
        public const int RecargoNocturnoDominicalCompensado = 13;
        public const int RecargoDiurnoFestivoCompensado = 14;
        public const int RecargoNocturnoFestivoCompensado = 15;
    }





    public static class ProcessPeriodsStatus
    {
        public const string Abierto = "abierto";
        public const string Cerrado = "cerrado";

    }

    public static class BUKMacroAbsenceTypes
    {
        public const string Licencia = "licence";
        public const string Permiso = "permission";
        public const string Inasistencia = "absence";
        public const string ConGoce = " con goce de sueldo";
        public const string SinGoce = " sin goce de sueldo";
    }

    public static class StandardTypes
    {
        public const string BukLicencia = "Licencia Médica";
        public const string GVLicencia = "Licencia Médica Estándar";
        public const string BukPermisoConGoce = "Permiso con goce de sueldo";
        public const string GVPermisoConGoce = "Permiso con Goce";
        public const string BukPermisoSinGoce = "Permiso sin goce de sueldo";
        public const string GVPermisoSinGoce = "Permiso sin Goce";
        public const string GVLicenciaMedicaPeru = "Descanso Médico Estándar";
        public const string BukPermisoConGoceColombia = "Permiso con goce de sueldo";
        public const string GVPermisoConGoceColombia = "Remunerada";
        public const string BukPermisoSinGoceColombia = "Permiso sin goce de sueldo";
        public const string GVPermisoSinGoceColombia = "No Remunerada";
        public const string BukMediaJornada = "Media Jornada";
    }

    public static class ColombiaStandardTypes
    {
        public const string BukSuspension = "suspension";
        public const string GVSuspension = "Suspensión";
        public const string BukIncapacidad = "incapacidad";
        public const string GVIncapacidad = "Incapacidad";
        public const string BukMaternidadPaternidad = "maternidad_paternidad";
        public const string GVMaternidad = "Licencia de Maternidad";
        public const string GVPaternidad = "Licencia de Paternidad";
        public const string BukLuto = "luto";
        public const string GVLuto = "Licencia por Luto";
        public const string BukVacaciones = "Vacaciones tomadas";
        public const string GVVacaciones = "Vacaciones";
        public const string BukRemunerada = "remunerada";
        public const string GVRemunerada = "Remunerada";
        public const string BukDomestica = "calamidad_domestica";
        public const string GVDomestica = "Calamidad Doméstica";
        public const string BukNoRemunerada = "no_remunerada";
        public const string GVNoRemunerada = "No Remunerada";
        public const string BukDomingo = "domingo_compensatorio";
        public const string GVDomingo = "Domingo compensatorio";
        public const string BukFestivo = "festivo_compensatorio";
        public const string GVFestivo = "Feriado compensatorio";
        public const string BukAborto = "aborto";
        public const string GVAborto = "Licencia de Aborto";
        public const string BukDiaFamilia = "dia_familia";
    }

    public static class BUKStandardAbsence
    {
        public const string kind = "ausencia";
        public const string code = "ausencia";
        public const string name = "Ausencia";
        public const string description = "Ausencia Injustificada";
        public const bool with_pay = false;
    }

    public static class BUKArticulos22Job
    {
        public const string SinJornada = "sin_jornada";

    }

    public static class SuspensionsType
    {
        public const string ActoAutoridad = "acto_autoridad";
        public const string ReduccionJornada = "reduccion_jornada";
        public const string SuspensionTemporal = "suspension_temporal";
        public const string SuspensionGeneral = "Suspension";

    }

    public static class SuspensionsGVType
    {
        public const string ActoAutoridad = "Suspension (Acto Autoridad)";
        public const string SuspensionTemporal = "Suspension (Temporal)";
        public const string SuspensionGeneral = "Suspension";

    }

    public static class CustomAtributesEmployee
    {
        public const string SincronizarGeovictoria = "SINCRONIZAR GEOVICTORIA";
        public const string SiSincronizar = "SI";
        public const string NoSincronizar = "NO";
        public const string SinDefinirSincronizar = "SIN DEFINIR";
        public const string SincronizarCorreoGV = "SINCRONIZAR_CORREO_GV";
        public const string CostCenterGV = "COST_CENTER_GV";
    }

    public static class AdverbValues
    {
        public const string Si = "SI";
        public const string No = "NO";
        public const string AdministrativePichara = "ADMINISTRATIVO";
        public const string SalesPichara = "VENTAS";
    }

    public static class WorkingScheduleType
    {
        public const string ExentaArt22 = "exenta_art_22";
        public const string OrdinariaArt22 = "ordinaria_art_22";
        public const string ParcialArt40 = "parcial_art_40_bis";
    }

    public static class JornadaTrabajo
    {
        public const string FullTime = "Full Time";
        public const string PartTime = "Part Time";
    }

    public static class CasaIdeasHomologacionPermisosParciales
    {
        public readonly static TimeSpan FullTime = new TimeSpan(4, 30, 0);
        public readonly static TimeSpan PartTime = new TimeSpan(2, 0, 0);
    }
    public static class SincronizarCorreo
    {
        public const int NoSincronizarCorreos = 0;
        public const int SincronizarCorreosCorporativos = 1;
        public const int SincronizarCorreosPersonales = 2;
    }
}
