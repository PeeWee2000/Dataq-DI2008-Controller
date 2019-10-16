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

            var Test = Dataq.Functions.GetDigitalChannelStates(20);


            Dataq.Connect();            

            Dataq.Channels.Analog0 = ChannelConfiguration.KTypeTC;
            Dataq.Channels.Analog1 = ChannelConfiguration.STypeTC;
            Dataq.Channels.Analog2 = ChannelConfiguration.STypeTC;
            Dataq.Channels.Analog3 = ChannelConfiguration.STypeTC;
            Dataq.Channels.Analog4 = ChannelConfiguration.STypeTC;
            Dataq.Channels.Analog5 = ChannelConfiguration.STypeTC;
            Dataq.Channels.Analog6 = ChannelConfiguration.STypeTC;
            //Dataq.Channels.Analog7 = ChannelConfiguration._25v;

            //Dataq.Channels.Digital0 = ChannelConfiguration.DigitalOutput;
            //Dataq.Channels.Digital1 = ChannelConfiguration.DigitalOutput;
            //Dataq.Channels.Digital2 = ChannelConfiguration.DigitalOutput;




            Dataq.ConfigureChannels();
            Dataq.Functions.SetLedColor(LEDColor.Magenta);

            var Waef = Dataq.Functions.Write("stop");
            Waef = Dataq.Functions.Write("din");
            
            
            
            Dataq.Functions.StartAcquiringData();


            


            ReadRecord InstantaneousRead = new ReadRecord();

            Reader = new Thread(() =>
            {
                while (true)
                { 
                    var Data = Dataq.Functions.ReadData();

                    lock (Dataq)
                    {
                        InstantaneousRead = Data;
                    }
                }
            });

            Reader.Start();

            while(InstantaneousRead.Analog0 == null) 
            { Thread.Sleep(100); }


            while (true)
            {
                try
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine(Math.Round(InstantaneousRead.Analog0.Value.Value, 2));


                    Console.ForegroundColor = ConsoleColor.Blue;
                    Console.WriteLine(InstantaneousRead.DigitalStates);
                }
                catch { }
                Thread.Sleep(100);
            }
        }
    }
}
