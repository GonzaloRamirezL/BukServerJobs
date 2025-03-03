using API.Helpers.VM;
using API.Helpers.VM.Consts;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;

namespace API.Helpers.Commons
{
    public static class CompanyHelper
    {
        private readonly static string path = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
        public static List<SesionVM> getFileInfo(string filename)
        {
            List<SesionVM> empresas = new List<SesionVM>();
            string file = path + "\\" + filename;
            Console.WriteLine("LEYENDO EMPRESAS");
            try
            {
                List<string> lines = File.ReadAllLines(file, Encoding.UTF8).ToList();
                foreach (string line in lines)
                {
                    string[] param = line.Split(';');
                    SesionVM empresa = new SesionVM();
                    empresa.Empresa = param[FileReaderConsts.CompanyNamePosition];
                    var Url = param[FileReaderConsts.BUKURLPosition].Split('|');
                    empresa.Url = Url[0];
                    empresa.Url2 = Url.Count() > 1 ? Url[1] : "";
                    var Bukkey = param[FileReaderConsts.BUKTokenPosition].Split('|');
                    empresa.BukKey2 = Bukkey.Count() > 1 ? Bukkey[1] : "";
                    empresa.BukKey = Bukkey[0];
                    empresa.GvUrl = param[FileReaderConsts.GVURLPosition];
                    empresa.GvKey = param[FileReaderConsts.GVTokenPosition];
                    empresa.Pais = param[FileReaderConsts.CountryPosition];
                    empresa.FechaCorte = int.Parse(param[FileReaderConsts.ProcessEndDayPosition]);
                    empresa.DesfaseInasistencias = int.Parse(param[FileReaderConsts.AbsenceDelayPosition]);
                    empresa.DesfaseHorasExtras = int.Parse(param[FileReaderConsts.OvertimeDelayPosition]);
                    empresa.DesfaseHorasNoTrabajadas = int.Parse(param[FileReaderConsts.NonWorkedHoursDelayPosition]);
                    if (param[FileReaderConsts.SyncArt22Position] == FileReaderConsts.SyncArt22Yes)
                    {
                        empresa.SincronizaArticulos22 = true;

                    }
                    else
                    {
                        empresa.SincronizaArticulos22 = false;
                    }
                    int count = param.Length;
                    if (count >= FileReaderConsts.HasAbsenceDelayTypePosition)
                    {
                        if (param[FileReaderConsts.AbsenceDelayTypePosition] == FileReaderConsts.DelayInDays)
                        {
                            empresa.TipoDesfaseInasistencias = FileReaderConsts.DelayInDays;
                        }
                        else
                        {
                            empresa.TipoDesfaseInasistencias = FileReaderConsts.DelayInMonths;
                        }
                        if (count >= FileReaderConsts.HasOvertimeDelayTypePosition)
                        {
                            if (param[FileReaderConsts.OvertimeDelayTypePosition] == FileReaderConsts.DelayInDays)
                            {
                                empresa.TipoDesfaseHorasExtras = FileReaderConsts.DelayInDays;
                            }
                            else
                            {
                                empresa.TipoDesfaseHorasExtras = FileReaderConsts.DelayInMonths;
                            }
                            if (count >= FileReaderConsts.HasNonWorkedHoursDelayTypePosition)
                            {
                                if (param[FileReaderConsts.NonWorkedHoursDelayTypePosition] == FileReaderConsts.DelayInDays)
                                {
                                    empresa.TipoDesfaseHorasNoTrabajadas = FileReaderConsts.DelayInDays;
                                }
                                else
                                {
                                    empresa.TipoDesfaseHorasNoTrabajadas = FileReaderConsts.DelayInMonths;
                                }

                            }
                            else
                            {
                                empresa.TipoDesfaseHorasNoTrabajadas = FileReaderConsts.DelayInMonths;
                            }
                        }
                        else
                        {
                            empresa.TipoDesfaseHorasExtras = FileReaderConsts.DelayInMonths;
                            empresa.TipoDesfaseHorasNoTrabajadas = FileReaderConsts.DelayInMonths;
                        }
                    }
                    else
                    {
                        empresa.TipoDesfaseInasistencias = FileReaderConsts.DelayInMonths;
                        empresa.TipoDesfaseHorasExtras = FileReaderConsts.DelayInMonths;
                        empresa.TipoDesfaseHorasNoTrabajadas = FileReaderConsts.DelayInMonths;
                    }
                    if (count >= FileReaderConsts.HasJobPosition)
                    {
                        empresa.CargoEmpleo = param[FileReaderConsts.JobPosition];
                    }

                    int syncEmail;
                    if (int.TryParse(param[FileReaderConsts.SyncEmail], out syncEmail))
                    {
                       empresa.SincronizarCorreo = syncEmail;
                    }
                    else
                    {
                        string valueToFind = "syncEmail";
                        string syncEmailValue = null;
                        foreach (string item in param)
                        {
                            string[] partes = item.Split(new char[] { ':' }, StringSplitOptions.RemoveEmptyEntries);
                            if (partes.Length == 2)
                            {
                                string clave = partes[0].Trim();
                                string valor = partes[1].Trim();

                                if (clave == valueToFind)
                                {
                                    syncEmailValue = valor;
                                    break;
                                }
                            }
                        }
                        if (syncEmailValue != null){
                            empresa.SincronizarCorreo = int.Parse(syncEmailValue);
                        }
                        else{
                            empresa.SincronizarCorreo = null;
                        }
                    }
                    


                    empresas.Add(empresa);
                    Console.WriteLine("EMPRESA LEIDA: " + param[0] + " CON PARAMETROS BUKURL: " + param[1] + " BUKKEY: " + param[2] + " GVURL: " + param[3] + " GVKEY: " + param[4] + " PAIS: " + param[5] + " FECHA CORTE: " + param[6] + " DESFASE INASISTENCIAS: " + param[7] + " DESFASE HHEE: " + param[8] + " DESFASE HNT: " + param[9]);
                }
            }
            catch (Exception e)
            {
                InsightHelper.logException(e, "BUK-GENERAL");
                Console.WriteLine("ERROR!!! " + e.Message);

            }

            return empresas;
        }


    }
}
