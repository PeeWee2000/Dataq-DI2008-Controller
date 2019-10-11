using DI2008Controller;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using System.Windows.Threading;
using System.Linq;
using System;

namespace Waef
{
    class Example
    {
        private static Thread Reader;  //Continuously grabs data


        public static void Waef()
        {
            var Dataq = new DI2008();

            Dataq.Connect();            

            Dataq.Channels.Analog0 = ChannelConfiguration.KTypeTC;
            Dataq.Channels.Analog1 = ChannelConfiguration._1000mv;
            //Dataq.Channels.Analog2 = ChannelConfiguration.STypeTC;
            //Dataq.Channels.Analog3 = ChannelConfiguration._10mv;
            //Dataq.Channels.Analog4 = ChannelConfiguration._500mv;
            //Dataq.Channels.Analog5 = ChannelConfiguration._1000mv;
            //Dataq.Channels.Analog6 = ChannelConfiguration._5v;
            //Dataq.Channels.Analog7 = ChannelConfiguration._25v;

            Dataq.ConfigureChannels();
            Dataq.Functions.SetLedColor(LEDColor.Magenta);
            Dataq.Functions.StartAcquiringData();

            var waef = Dataq;

            var DataList = new List<Output>();
            Output InstantaneousRead = new Output();

            Reader = new Thread(() =>
            {
                while (true)
                { 
                    var Data = Dataq.Functions.ReadData();

                    lock (DataList)
                    {
                        InstantaneousRead = Data;
                        //DataList.Add(Data);
                    }
                }
            });

            Thread.Sleep(5000);
            Reader.Start();
            Thread.Sleep(5000);

            while (true)
            {
               Console.WriteLine(Math.Round(InstantaneousRead.Analog0.Value.Value, 2) + InstantaneousRead.Analog0.Value.Unit);
                Console.WriteLine(Math.Round(InstantaneousRead.Analog1.Value.Value, 2));
                Thread.Sleep(100);
            }




            var Waef2 = Dataq.DeviceInfo.FirmwareVersion;

            Dataq.Functions.Write("stop");
            

        }

    }
}
