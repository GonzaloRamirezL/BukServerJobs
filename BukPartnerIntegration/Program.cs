using API.BUK.DTO;
using API.GV.DTO;
using API.Helpers;
using API.Helpers.Commons;
using API.Helpers.VM;
using API.Helpers.VM.Consts;
using BusinessLogic.Interfaces.VM;
using ModuleBuilder;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BukPartnerIntegration
{
    class Program
    {
        private const int DEFAULT_DEGREE_OF_PARALLELLISM = 10;
        private static List<SesionVM> parameters = CompanyHelper.getFileInfo("parametros.txt");
        
        static void Main(string[] args)
        {
            InsightHelper.logTrace("INICIO DE LA INTEGRACION", "BUK-Personalizacion Multifichas");
            Console.WriteLine("INICIO DE LA INTEGRACION");
            int MAX_PROCESS = 0;
            bool success = Int32.TryParse(ConfigurationHelper.Value("maxParallel"), out int maxProcess);
            MAX_PROCESS = success ? maxProcess : DEFAULT_DEGREE_OF_PARALLELLISM;
            try
            {
                List<Operacion> ops = new List<Operacion>();

                if (args != null && args.Length > 0)
                {
                    SesionVM sesionActiva = parameters.FirstOrDefault(e => e.Empresa == args[0]);
                    FileLogHelper.log(LogConstants.general, LogConstants.get, "", "Empresa: " + sesionActiva.Empresa, null, sesionActiva);
                    FileLogHelper.log(LogConstants.general, LogConstants.get, "", "Argumentos: " + string.Join(',', args), null, sesionActiva);
                    sesionActiva.EnviaHHEE = false;
                    sesionActiva.EnviaAusencia = false;
                    sesionActiva.EnviaHNT = false;
                    Console.WriteLine("INICIO DE LA INTEGRACION");

                    if (args.Length > 1)
                    {
                        foreach (string s in args.Skip(1))
                        {
                            switch (s.ToLower())
                            {
                                case "syncusuarios": if (!ops.Contains(Operacion.USUARIOS)) ops.Add(Operacion.USUARIOS); break;
                                case "syncpermisos": if (!ops.Contains(Operacion.PERMISOS)) ops.Add(Operacion.PERMISOS); break;
                                case "syncasistencia": if (!ops.Contains(Operacion.ASISTENCIA)) ops.Add(Operacion.ASISTENCIA); break;
                                case "hnt": if (ops.Contains(Operacion.ASISTENCIA)) sesionActiva.EnviaHNT = true; break;
                                case "hhee": if (ops.Contains(Operacion.ASISTENCIA)) sesionActiva.EnviaHHEE = true; break;
                                case "ausencias": if (ops.Contains(Operacion.ASISTENCIA)) sesionActiva.EnviaAusencia = true; break;
                                default: continue;
                            }
                        }
                    }
                    else
                    {
                        ops.Add(Operacion.USUARIOS);
                    }
                    ejecutarOperaciones(ops, sesionActiva);
                }
                else
                {
                    InsightHelper.logException(new ArgumentNullException("SIN EMPRESA"), "BUK-GENERAL");
                    Console.WriteLine("ERROR!!! SIN EMPRESA");

                }
            }
            catch (Exception e)
            {                
                InsightHelper.logException(e, "BUK-GENERAL");
                Console.WriteLine("ERROR!!! "+ e.Message);
            }
            InsightHelper.logTrace("FIN DE LA INTEGRACION", "BUK-GENERAL");
            Console.WriteLine("FIN DE LA INTEGRACION");
            InsightHelper.flush();
        }

        static void ejecutarOperaciones(List<Operacion> ops, SesionVM sesionActiva)
        {
            if(ops != null)
            {
                CompanyConfiguration companyConfiguration = CompanyBuilder.GetCompanyConfiguration(sesionActiva);

                foreach (Operacion op in ops)
                {
                    switch (op)
                    {
                        case Operacion.USUARIOS:
                            {
                                FileLogHelper.log(LogConstants.general, LogConstants.get, "", "SINCRONIZANDO USUARIOS", null, sesionActiva);
                                try
                                {
                                    List<ProcessPeriod> activos = companyConfiguration.ProcessPeriodsBusiness.GetActivePeriods(sesionActiva, companyConfiguration);
                                    companyConfiguration.UserBusiness.Sync(sesionActiva, companyConfiguration, activos.Last());
                                }
                                catch (Exception ex)
                                {
                                    InsightHelper.logException(ex, sesionActiva.Empresa);
                                    Console.WriteLine("ERROR!!! " + ex.Message);
                                }
                                
                                Console.WriteLine("FIN SINCRONIZACIÓN USUARIOS");
                                break;
                            }
                        case Operacion.PERMISOS:
                            {
                                FileLogHelper.log(LogConstants.general, LogConstants.get, "", "SINCRONIZANDO PERMISOS", null, sesionActiva);
                                try
                                {
                                    List<ProcessPeriod> activos = companyConfiguration.ProcessPeriodsBusiness.GetActivePeriods(sesionActiva, companyConfiguration);
                                    ParallelOptions pOptions = new ParallelOptions();
                                    pOptions.MaxDegreeOfParallelism = activos != null ? activos.Count : 1;
                                    Parallel.ForEach(activos, pOptions, periodo =>
                                    {
                                        var configs = companyConfiguration.ProcessPeriodsBusiness.GetPeriodsConfiguration(sesionActiva, periodo, companyConfiguration);
                                        companyConfiguration.TimeOffBusiness.Sync(sesionActiva, periodo, configs, companyConfiguration);
                                    });
                                }
                                catch (Exception ex)
                                {
                                    InsightHelper.logException(ex, sesionActiva.Empresa);
                                    Console.WriteLine("ERROR!!! " + ex.Message);
                                }
                                
                                Console.WriteLine("FIN SINCRONIZACIÓN PERMISOS");
                                break;
                            }
                        case Operacion.ASISTENCIA:
                            {
                                FileLogHelper.log(LogConstants.general, LogConstants.get, "", "SINCRONIZANDO ASISTENCIA", null, sesionActiva);
                                try
                                {
                                    List<ProcessPeriod> activos = companyConfiguration.ProcessPeriodsBusiness.GetActivePeriods(sesionActiva, companyConfiguration);
                                    ParallelOptions pOptions = new ParallelOptions();

                                    Parallel.ForEach(activos, pOptions, periodo =>
                                    {
                                        var configs = companyConfiguration.ProcessPeriodsBusiness.GetPeriodsConfiguration(sesionActiva, periodo, companyConfiguration);
                                        companyConfiguration.AttendanceBusiness.Sync(sesionActiva, periodo, configs, companyConfiguration);
                                    });
                                }
                                catch (Exception ex)
                                {
                                    InsightHelper.logException(ex, sesionActiva.Empresa);
                                    Console.WriteLine("ERROR!!! " + ex.Message);
                                }
                                
                                Console.WriteLine("FIN SINCRONIZACIÓN ASISTENCIA");
                                break;
                            }

                        default: break;
                    }
                }
            }
        }


        

    }
}
