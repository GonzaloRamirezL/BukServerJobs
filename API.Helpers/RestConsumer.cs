using System;
using Newtonsoft.Json;
using System;
using System.Net.Http;
using System.Text;
using System.Configuration;
using API.Helpers.Commons;
using API.Helpers.VM;

namespace API.Helpers
{
    public class RestConsumer
    {
        private static readonly double TimeOut = double.Parse(ConfigurationManager.AppSettings.Get("timeout"));
        //private static readonly string GVAuthorizationKey = ConfigurationManager.AppSettings.Get("gvAuthorizationKey") ?? "default";
        //private static readonly string BUKAuthorizationKey = ConfigurationManager.AppSettings.Get("bukAuthorizationKey") ?? "default";

        //private static readonly HttpClient ClienteBUK = null;
        //private static readonly HttpClient ClienteGV = null;

        private readonly HttpClient Cliente = null;
        private readonly SesionVM Empresa = new SesionVM();

        //static RestConsumer()
        //{
        //    ClienteBUK = new HttpClient
        //    {
        //        BaseAddress = new Uri(ConfigurationManager.AppSettings.Get("UrlBUK")),
        //        Timeout = TimeSpan.FromMinutes(TimeOut)
        //    };

        //    ClienteBUK.DefaultRequestHeaders.Add("auth_token", BUKAuthorizationKey);
        //    ClienteBUK.DefaultRequestHeaders.Add("Accept", "application/json");

        //    ClienteGV = new HttpClient
        //    {
        //        BaseAddress = new Uri(ConfigurationManager.AppSettings.Get("UrlGV")),
        //        Timeout = TimeSpan.FromMinutes(TimeOut)
        //    };

        //    ClienteGV.DefaultRequestHeaders.Add("authorization", GVAuthorizationKey);

            
        //}

        public RestConsumer(BaseAPI baseAPI, string Url, string Key, SesionVM empresa)
        {
            this.Empresa = empresa;
            switch (baseAPI)
            {
                case BaseAPI.BUK:
                    Cliente  = new HttpClient
                    {
                        BaseAddress = new Uri(Url),
                        Timeout = TimeSpan.FromMinutes(TimeOut)
                    };
                    Cliente.DefaultRequestHeaders.Add("auth_token", Key);
                    Cliente.DefaultRequestHeaders.Add("Accept", "application/json");
                    break;
                case BaseAPI.GV:
                    Cliente = new HttpClient
                    {
                        BaseAddress = new Uri(Url),
                        Timeout = TimeSpan.FromMinutes(TimeOut)
                    };
                    Cliente.DefaultRequestHeaders.Add("authorization", Key);
                    break;
            }
        }

        public T PostResponse<T, U>(string url, U obj)
        {
            HttpResponseMessage response = null;
            
            RetryHelper.Execute(
                () =>
                {
                    response = AsyncHelpers.RunSync<HttpResponseMessage>(() => Cliente.PostAsync(url, CreateHttpContent<U>(obj)));
                    if (response.StatusCode == System.Net.HttpStatusCode.OK)
                    {
                        response.EnsureSuccessStatusCode();
                    }
                    else
                    {
                        Console.WriteLine("StatusCode: " + response.StatusCode + " Content: " + response.Content.ReadAsStringAsync());
                    }
                }
            );

            return CreateHttpContent<T>(response);
        }



        public T PutResponse<T, U>(string url, U obj)
        {
            HttpResponseMessage response = null;
            
            RetryHelper.Execute(
                () =>
                {
                    response = AsyncHelpers.RunSync<HttpResponseMessage>(() => Cliente.PutAsync(url, CreateHttpContent<U>(obj)));
                    if (response.StatusCode == System.Net.HttpStatusCode.OK)
                    {
                        response.EnsureSuccessStatusCode();
                    }
                    else
                    {
                        Console.WriteLine("StatusCode: " + response.StatusCode + " Content: " + response.Content.ReadAsStringAsync());
                    }
                }
            );

            return CreateHttpContent<T>(response);
        }

        public T DeleteResponse<T, U>(string url, U obj)
        {
            HttpResponseMessage response = null;

            var request = new HttpRequestMessage
            {
                Method = HttpMethod.Delete,
                RequestUri = new Uri(Cliente.BaseAddress, url),
                Content = CreateHttpContent<U>(obj)
            };

            RetryHelper.Execute(
                () =>
                {
                    response = AsyncHelpers.RunSync<HttpResponseMessage>(() => Cliente.SendAsync(request));
                    if (response.StatusCode == System.Net.HttpStatusCode.OK)
                    {
                        response.EnsureSuccessStatusCode();
                    }
                    else if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                    {
                        //response.EnsureSuccessStatusCode();
                    }
                    else
                    {
                        Console.WriteLine("StatusCode: " + response.StatusCode + " Content: " + response.Content.ReadAsStringAsync());
                    }
                }
            );

            return CreateHttpContent<T>(response);
        }

        public T GetResponse<T, U>(string url, U obj)
        {
            HttpResponseMessage response = null;

            var request = new HttpRequestMessage
            {
                Method = HttpMethod.Get,
                RequestUri = new Uri(Cliente.BaseAddress, url),
                Content = CreateHttpContent<U>(obj)
            };

            RetryHelper.Execute(
                () =>
                {
                    response = AsyncHelpers.RunSync<HttpResponseMessage>(() => Cliente.SendAsync(request));
                    if (response.StatusCode == System.Net.HttpStatusCode.OK)
                    {
                        response.EnsureSuccessStatusCode();
                    }
                    else
                    {
                        Console.WriteLine("StatusCode: " + response.StatusCode + " Content: " + response.Content.ReadAsStringAsync());
                    }

                }
            );

            return CreateHttpContent<T>(response);
        }

        private T CreateHttpContent<T>(HttpResponseMessage response)
        {

            T ret = default(T);

            if (response.IsSuccessStatusCode)
            {
                string resp = AsyncHelpers.RunSync<string>(() => response.Content.ReadAsStringAsync());
                return JsonConvert.DeserializeObject<T>(resp);
            }
            else if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                try
                {
                    string resp = AsyncHelpers.RunSync<string>(() => response.Content.ReadAsStringAsync());
                    return JsonConvert.DeserializeObject<T>(resp);
                }
                catch (Exception)
                {
                    return ret;
                }
                
            }

            return ret;
        }

        private HttpContent CreateHttpContent<T>(T content)
        {
            var json = JsonConvert.SerializeObject(content);
            return new StringContent(json, Encoding.UTF8, "application/json");
        }
    }

    public enum BaseAPI
    {
        GV,
        BUK
    }

}
