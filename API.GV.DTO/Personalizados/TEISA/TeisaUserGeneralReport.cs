using System;

namespace API.GV.DTO.Personalizados.TEISA
{
    public class TeisaUserGeneralReport
    {
        public string Rut { get; set; }
        public string Nombre { get; set; }
        public string Empresa { get; set; }
        public string Areas { get; set; }
        public TimeSpan HorasTrabajadas { get; set; }
        public TimeSpan Ausencias { get; set; }
        public TimeSpan AusenciasJustificadas { get; set; }
        public int Faltas { get; set; }
        public int Colacion { get; set; }
        public int ColacionAlargue { get; set; }
        public int Cena { get; set; }
        public int Licencias { get; set; }
        public TimeSpan Hhee75 { get; set; }
        public TimeSpan Hhee50 { get; set; }
        public TimeSpan Hhee100 { get; set; }
        public TimeSpan HheeTurnoLlamadoPrimerDia { get; set; }
        public TimeSpan HheeTurnoLlamadoSegundoDia { get; set; }
        public TimeSpan Feriado50 { get; set; }
        public TimeSpan Feriado75 { get; set; }
        public TimeSpan Feriado100 { get; set; }
        public int TurnoLlamado { get; set; }
        public TimeSpan HorasTurnoLlamado { get; set; }
        public int BonoNocturno { get; set; }
        public bool BonoAsistencia { get; set; }
        public bool BonoVacaciones { get; set; }
        public string Comentario { get; set; }
        public int Vacaciones { get; set; }
        public int? RazonSocial { get; set; }
        public int Movilizacion { get; set; }
        public int Teletrabajo { get; set; }
        public TimeSpan TotalHHEE50 { get; set; }
        public TimeSpan TotalHHEE75 { get; set; }
        public TimeSpan TotalHHEE100 { get; set; }
    }
}
