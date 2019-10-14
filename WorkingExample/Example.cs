using DI2008Controller;
using System;
using System.Threading;

namespace Waef
{
    class Example
    {
        private static Thread Reader;  //Continuously grabs data

        public static void Main()
        {
            var Dataq = new DI2008();

            //try { Dataq.Disconnect(); } catch { }
            Dataq.Connect();            

            Dataq.Channels.Analog0 = ChannelConfiguration.KTypeTC;
            Dataq.Channels.Analog1 = ChannelConfiguration.STypeTC;
            Dataq.Channels.Analog2 = ChannelConfiguration.STypeTC;
            Dataq.Channels.Analog3 = ChannelConfiguration.STypeTC;
            Dataq.Channels.Analog4 = ChannelConfiguration.STypeTC;
            Dataq.Channels.Analog5 = ChannelConfiguration.STypeTC;
            Dataq.Channels.Analog6 = ChannelConfiguration.STypeTC;
            //Dataq.Channels.Analog7 = ChannelConfiguration._25v;

            Dataq.ConfigureChannels();
            Dataq.Functions.SetLedColor(LEDColor.Magenta);
            Dataq.Functions.StartAcquiringData();


            //var DataList = new List<Output>();
            ReadRecord InstantaneousRead = new ReadRecord();

            Reader = new Thread(() =>
            {
                while (true)
                { 
                    var Data = Dataq.Functions.ReadData();

                    lock (Dataq)
                    {
                        InstantaneousRead = Data;
                        //DataList.Add(Data);
                    }
                }
            });

            Reader.Start();

            while(InstantaneousRead.Analog0 == null)
            { Thread.Sleep(100); }


            while (true)
            {
                //foreach (var Property in InstantaneousRead.GetType().GetProperties())
                //{
                //    string Unit;
                //    double Value;

                //    var ChannelData = Property.GetType().GetProperty("Value");
                //    Value = (double)ChannelData.GetType().GetProperty("Value").GetValue(ChannelData);
                //    Unit = (string)ChannelData.GetType().GetProperty("Unit").GetValue(ChannelData);

                //}

                try
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine(Math.Round(InstantaneousRead.Analog0.Value.Value, 2));
                    Console.ForegroundColor = ConsoleColor.Blue;
                    Console.WriteLine(Math.Round(InstantaneousRead.Analog1.Value.Value, 2));
                    //Console.ForegroundColor = ConsoleColor.DarkGreen;
                    //Console.WriteLine(Math.Round(InstantaneousRead.Analog2.Value.Value, 2));
                }
                catch { }
                //Console.WriteLine(Math.Round(InstantaneousRead.Analog0.Value.Value, 2) + ", " + InstantaneousRead.Analog1.Value.Value + ", " + InstantaneousRead.Analog2.Value.Value);

                Thread.Sleep(100);
            }
        }
    }
}
