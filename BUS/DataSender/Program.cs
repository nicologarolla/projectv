using System;
using System.IO;
using System.Text;
using CSRedis;
using System.Collections.Generic;
using System.Linq;
using TwinCAT.Ads;
using System.Threading;
using System.Diagnostics;
using System.Net;

namespace DataSender
{
    public class Program
    {
        public static bool wait = false;
        public static bool erserver = false;
        public static bool erbus = false;
        public static bool speed = false;

        //Set Url
        static string ip = Properties.Settings.Default.ServerIpGarolla;
        static string port = Properties.Settings.Default.ServerPort;
        static string api_path = Properties.Settings.Default.ApiPath;
        static string url = "http://" + ip + ":" + port + api_path;
        static string urlToken = "http://" + ip + ":" + port + "/token";
        static void Main(string[] args)
        {
            // configure Redis
            var redis = new RedisClient("127.0.0.1");

            //Esegue autenticazione
            string token = "";
            using (WebClient webClient = new WebClient())
            {
                string credentials = "{\"id\": \"pippo\", \"password\": \"passwordsicura\"}";
                webClient.Headers[HttpRequestHeader.ContentType] = "application/json";
                //webClient.Credentials = new NetworkCredential("pippo","passwordsicura");
                token = webClient.UploadString(urlToken, credentials);
            }

            while (true)
            {
                //read application input
                string[] lines = File.ReadAllLines(Properties.Settings.Default.ExternalAppPath);
                if (lines.Length > 0)
                {
                    File.WriteAllText(Properties.Settings.Default.ExternalAppPath, null);
                    Execute(lines[0]);
                }

                // read from Redis queue
                List<string> redislist = redis.LRange("sensors_data", 0, redis.LLen("sensors_data")+1).ToList();
                WriteBusQueue(redislist);

                if(wait==true)
                {
                    Thread.Sleep(5000);
                }
                else
                {
                    string json = redis.BRPop(30, "sensors_data");

                    // send value to remote API
                    try
                    {
                        using (WebClient webClient = new WebClient())
                        {
                            webClient.Headers[HttpRequestHeader.ContentType] = "application/json";
                            webClient.Headers[HttpRequestHeader.Authorization] = "Bearer " + token;
                            webClient.UseDefaultCredentials = true;
                            webClient.Credentials = new NetworkCredential("pippo", "poppo");
                            string response = webClient.UploadString(url, json);
                            try
                            {
                                if (speed == false)
                                {
                                    Thread.Sleep(1500);
                                }

                                TellTwincat(json);
                                WriteTwincat("GVL.Scoring", true);
                                WriteTwincat("GVL.DataFromServer", json);
                            }
                            catch (Exception e)
                            {
                                Console.WriteLine(e.Message);
                            }
                        }


                        WriteTwincat("GVL.Sending", false);
                        Console.Write("si ");
                        Console.WriteLine();
                    }
                    catch (Exception err)
                    {
                        Console.Write("no ");
                        Console.WriteLine(" : " + err.Message);
                        redis.LPush("sensors_data", json);
                    }
                }
            }
        }





        public static void TellTwincat(string data)
        {
            List<string> Datas = data.Split(',', ':').ToList();
            for (int i = 0; i < Datas.Count; i++)
            {
                string stringa = "";
                object valore = null;
                bool write = false;

                string riga = Datas[i];

                if (riga.Contains("Apertura"))
                {
                    stringa = "GVL.PorteChiuse";

                    if (Datas[i + 1].Contains("0"))
                        valore = false;

                    if (Datas[i + 1].Contains("1"))
                        valore = true;

                    if(!(valore is null))
                    {
                        write = true;
                    }

                }

                if (riga.Contains("Latitudine"))
                {
                    stringa = "GVL.GPS";
                    valore = true;


                    if (!(valore is null))
                        write = true;
                }

                if (riga.Contains("Conta_Persone"))
                {
                    stringa = "GVL.ContaPersone";
                    valore = true;

                    if (!(valore is null))
                        write = true;
                }

                if (write)
                {
                    WriteTwincat(stringa, valore);
                }
            }
        }

        public static void WriteTwincat(string comando, object valore)
        {
            if (!(valore is null))
            {
                using (TcAdsClient client = new TcAdsClient())
                {
                    try
                    {
                        client.Connect(Properties.Settings.Default.AmsNetID, Properties.Settings.Default.AmsNetPort);
                        int handle = client.CreateVariableHandle(comando);

                        if(valore.GetType().FullName.Contains("String"))
                        {
                            string el = valore.ToString();
                            AdsStream stream = new AdsStream(500);
                            AdsBinaryWriter writer = new AdsBinaryWriter(stream);
                            writer.WritePlcString(el, 500, Encoding.Unicode);
                            client.Write(handle, stream);

                            stream.Dispose();
                            writer.Dispose();
                        }
                        else
                        {
                            client.WriteAny(handle, valore);
                        }

                        //client.Dispose();
                        client.DeleteVariableHandle(handle);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                    }
                }
            }
        }


        public static void WriteBusQueue(List<string> list)
        {
            try
            {
                WriteTwincat("GVL.EraseTable", true);

                TcAdsClient client = new TcAdsClient();
                client.Connect(Properties.Settings.Default.AmsNetID, Properties.Settings.Default.AmsNetPort);
                int handle = client.CreateVariableHandle("GVL.DataFromBus");

                foreach (string el in list)
                {
                    AdsStream stream = new AdsStream(500);
                    AdsBinaryWriter writer = new AdsBinaryWriter(stream);
                    writer.WritePlcString(el, 500, Encoding.Unicode);
                    client.Write(handle, stream);
                    stream.Dispose();
                    writer.Dispose();
                    Thread.Sleep(10);
                }

                client.DeleteVariableHandle(handle);
                client.Dispose();
            }
            catch
            { }
        }


        public static void Execute(string el)
        {
            int time=5000;

            if (el.Contains("time"))
            {
                string[] el1 = el.Split(' ');

                el = el1[0];
                time = Convert.ToInt32(el1[1]);
            }

            switch (el)
            {
                case ("wait stop"): { wait = false; break; }
                case ("wait start"): { wait = true; break; }
                case ("speed start"): { speed = true; break; }
                case ("speed stop"): { speed = false; break; }
                case ("clear server"):
                    {
                        try
                        {
                            WriteTwincat("GVL.EraseSTable", true);
                        }
                        catch
                        { }

                        break;
                    }
                case ("clear vehicle"):
                    {
                        try
                        {
                            WriteTwincat("GVL.EraseTable", true);
                        }
                        catch
                        { }

                        break;
                    }
                case ("clear redis"):
                    {
                        var redis = new RedisClient("127.0.0.1");
                        redis.Del("sensors_data");
                        redis.Dispose();
                        break;
                    }
                case ("time"):
                    {
                        string path = @"C:\Users\PC\Desktop\prvgraf\BUS\EXTERNALTIME.txt";
                        File.WriteAllText(path, time.ToString());
                        break;
                    }
            }
        }
    }
}
