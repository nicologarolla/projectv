using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DataReader.Sensors
{
    class VirtualSensors : IGenericSensors, ISensor
    {
        private static Random rnd = new Random();
        static double initialnum = 0;
        static double initialac = 0;
        //public void SetTemperature(decimal temperature)
        //{ }

        public string Apertura()
        {
            double ran = rnd.Next(0, 2);
            if(initialac==ran && ran==0)
            {
                return null;

            }
            else
            {
                initialac = ran;
                return ran.ToString();
            }
        }

        public string ContaPersone()
        {
            if (initialac == 1)
            {
                //Conta Persone
                double ran1 = rnd.Next(-6, 6);
                //if (ran1 == 0)
                //{
                //    ran1 = 1;
                //}
                double num = initialnum + ran1;
                if (num < 0)
                {
                    num = 0;
                }
                initialnum = num;
                return initialnum.ToString();
            }
            else
            {
                return null;
            }
        }

        public string[] Posizione()
        {
            //int delay = rnd.Next(0, 5000);
            //System.Threading.Thread.Sleep(delay);

            using(StreamReader file = File.OpenText(@"C:\Users\PC\Desktop\prvgraf\BUS\DataReader\Sensors\track.json"))
            using (JsonTextReader reader = new JsonTextReader(file))
            {
                JObject o2 = (JObject)JToken.ReadFrom(reader);

                var listObj = o2.GetValue("trackData");


                string lat = listObj[0].Value<string>("lat");
                string[] list = new string[2];

                foreach (JToken trackToken in listObj.Children())
                {
                    list[0] = trackToken.Value<string>("lat").ToString();
                    list[1] = trackToken.Value<string>("lon").ToString();
                    //Console.WriteLine(trackToken.Value<string>("lon"));
                    
                }
            
                return list;
            }



/*
            string[] list = new string[2];
             double ran = Convert.ToDouble(rnd.Next(-99, 100)) / 10000000;
            list[0] = (45.9536714 + ran).ToString();

            ran = Convert.ToDouble(rnd.Next(-99, 100)) / 10000000;
            list[1] = (12.6855359 + ran).ToString();


            return list;*/
        }

        public string Id()
        {
            return rnd.Next(1,100).ToString();
        }


        public string ToJson()
        {
            string[] list = new string[5];
            list[0] = Apertura();
            string oraapertura = (System.DateTime.UtcNow.Ticks-System.DateTime.Parse("01/01/1970 00:00:00").Ticks).ToString();
            list[1] = ContaPersone();
            list[2] = Posizione()[0];
            list[3] = Posizione()[1];
            string oraposizione = (System.DateTime.UtcNow.Ticks-System.DateTime.Parse("01/01/1970 00:00:00").Ticks).ToString();
            list[4] = Id();
            return "{" +
                "\"Apertura\":\"" + list[0] + "\"," +
                "\"Conta_Persone\":\"" + list[1] + "\"," +
                "\"Latitudine\":\"" + list[2] + "\"," +
                "\"Longitudine\":\"" + list[3] + "\"," +
                "\"IdVeicolo\":\"" + list[4] + "\"," +
                "\"DataOraBus\":\"" + oraposizione + "\"," +
                "\"DataOraPorte\":\"" + oraapertura +"\""+
                "}";
        }
    }
}
