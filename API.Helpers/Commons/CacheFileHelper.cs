using API.Helpers.VM;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace API.Helpers.Commons
{
    public static class CacheFileHelper
    {
        private static readonly string DEFAULT_FOLDER = Directory.GetCurrentDirectory() + "\\Cache";
        private static readonly TimeSpan DEFAULT_LIFE_TIME = new TimeSpan(0, 45, 0);
        public static void CheckCache()
        {
            List<string> filesAlive = Directory.EnumerateFiles(DEFAULT_FOLDER).ToList();

            foreach (string file in filesAlive)
            {
                TimeSpan fileAliveTime = DateTime.UtcNow - File.GetCreationTimeUtc(file);
                if (fileAliveTime > DEFAULT_LIFE_TIME)
                {
                    File.Delete(file);
                }
            }
        }

        public static void CheckDirectory()
        {
            if (!Directory.Exists(DEFAULT_FOLDER))
            {
                Directory.CreateDirectory(DEFAULT_FOLDER);
            }
        }

        public static void WriteToFile(object toWrite, string fileName)
        {
            string json = JsonConvert.SerializeObject(toWrite);
            File.WriteAllText(DEFAULT_FOLDER + Path.DirectorySeparatorChar + fileName, json);
        }

        public static string GetFileName(SesionVM Empresa)
        {
            string companyName = Empresa.Empresa;
            if (Empresa.Empresa.Contains("-"))
            {
                companyName = Empresa.Empresa.Split("-").First();
            }
            return string.Format("{0}{1}{2}{3}.json", Empresa.BukKey, Empresa.GvKey, companyName, Empresa.CargoEmpleo);
        }

        public static bool CheckFile(string fileName)
        {
            return File.Exists(DEFAULT_FOLDER + Path.DirectorySeparatorChar + fileName);
        }

        public static T GetCacheContent<T>(string fileName)
        {
            return JsonConvert.DeserializeObject<T>(File.ReadAllText(DEFAULT_FOLDER + Path.DirectorySeparatorChar + fileName));
        }
    }
}
