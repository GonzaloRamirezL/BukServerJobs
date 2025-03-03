using System;
using System.Collections.Generic;
using System.Text;

namespace API.Helpers.VM.Consts
{
    public static class LogConstants
    {
        public static string ID_SISTEMA_EXTERNO = "BUK";
        public static string ModuloUsuarios = "USER";
        public static string ModuloPermisos = "TIMEOFF";
        public static string ModuloAsistencia = "ATTENDANCE";
        public static string ResultadoOK = "OK";
        public static string ResultadoError = "ERROR";
        public static string ResultadoNoEsperado = "NER";
        // Modules
        public static string general = "general";
        public static string user = "usuario";
        public static string timeOff = "permiso";
        public static string hnt = "hnt";
        public static string hhee = "hhee";
        public static string absences = "ausencia";
        public static string cutOffDate = "fecha de corte";
        public static string period = "periodo";
        // Actions
        public static string error_add = "Error al crear";
        public static string error_edit = "Error al editar";
        public static string error_activate = "Error al activar";
        public static string error_deactivate = "Error al desactivar";
        public static string error_move = "Error al mover";
        public static string add = "Creado";
        public static string edit = "Editado";
        public static string delete = "Eliminado";
        public static string activate = "Activado";
        public static string deactivate = "Desactivado";
        public static string move = "Movido";
        public static string get = "Obtener/Procesar";
        public static string no_cutoff = "Sin Fecha de Corte";
    }
}
