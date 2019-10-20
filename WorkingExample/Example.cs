using DI2008Controller;
using System;
using System.Threading;

namespace Waef
{
    class Example
    {
        private static Thread Reader;  //Continuously grabs data
        private static Thread ColorChanger;  //Continuously changes the led color


        public static void Main()
        {
            var Dataq = new DI2008();

            //Dataq.Connect();            

            Dataq.Channels.Analog0 = ChannelConfiguration.KTypeTC;
            Dataq.Channels.Analog1 = ChannelConfiguration.STypeTC;
            //Dataq.Channels.Analog2 = ChannelConfiguration.STypeTC;
            //Dataq.Channels.Analog3 = ChannelConfiguration.STypeTC;
            //Dataq.Channels.Analog4 = ChannelConfiguration.STypeTC;
            //Dataq.Channels.Analog5 = ChannelConfiguration.STypeTC;
            //Dataq.Channels.Analog6 = ChannelConfiguration.STypeTC;
            //Dataq.Channels.Analog7 = ChannelConfiguration._25v;

            Dataq.Channels.Digital0 = ChannelConfiguration.DigitalInput;
            Dataq.Channels.Digital1 = ChannelConfiguration.DigitalOutput;
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



            ColorChanger = new Thread(() =>
            {
                while (true)
                {
                    foreach (LEDColor color in (LEDColor[])Enum.GetValues(typeof(LEDColor)))
                    {
                        Thread.Sleep(1000);
                    
                        lock (Dataq)
                        {
                            Dataq.Functions.SetLedColor(color);
                        }
                    }
                }
            });

            //ColorChanger.Start();


            while (InstantaneousRead.Analog0 == null) 
            { Thread.Sleep(100); }


            while (true)
            {
                try
                {
                    Console.CursorVisible = false;
                    Console.SetCursorPosition(0, 0);
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.Write(Math.Round(InstantaneousRead.Analog0.Value.Value, 2));


                    Console.ForegroundColor = ConsoleColor.Blue;
                    Console.Write(" " + InstantaneousRead.DigitalStates + "\r\n") ;


                    //Thread.Sleep(100);



                }
                catch { }
                //Thread.Sleep(100);
            }
        }
    }
}
