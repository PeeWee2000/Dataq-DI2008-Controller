using DI2008Controller;
using System;
using System.Diagnostics;
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

            Dataq.Channels.Analog0 = ChannelConfiguration._10v;
            Dataq.Channels.Analog1 = ChannelConfiguration._10v;
            Dataq.Channels.Analog2 = ChannelConfiguration.STypeTC;
            Dataq.Channels.Analog3 = ChannelConfiguration.STypeTC;
            Dataq.Channels.Analog4 = ChannelConfiguration.STypeTC;
            Dataq.Channels.Analog5 = ChannelConfiguration.STypeTC;
            Dataq.Channels.Analog6 = ChannelConfiguration.STypeTC;
            //Dataq.Channels.Analog7 = ChannelConfiguration.STypeTC;

            Dataq.Channels.Digital0 = ChannelConfiguration.DigitalInput;
            Dataq.Channels.Digital1 = ChannelConfiguration.DigitalOutput;
            //Dataq.Channels.Digital2 = ChannelConfiguration.DigitalOutput;

            //Dataq.Channels.Analog0 = ChannelConfiguration.KTypeTC; // Column Head
            //Dataq.Channels.Analog1 = ChannelConfiguration.KTypeTC; // Reflux Jacket
            //Dataq.Channels.Analog2 = ChannelConfiguration.KTypeTC; // Condenser Jacket
            //Dataq.Channels.Analog3 = ChannelConfiguration.KTypeTC; // Coolant Reservoir
            //Dataq.Channels.Analog4 = ChannelConfiguration._100mv; // System Pressure
            //Dataq.Channels.Analog5 = ChannelConfiguration._100mv; // System Amperage
            //Dataq.Channels.Analog6 = ChannelConfiguration._100mv;

            //Dataq.Channels.Digital0 = ChannelConfiguration.DigitalInput; // Still Low Switch
            //Dataq.Channels.Digital1 = ChannelConfiguration.DigitalInput; // Still High Switch
            ////Dataq.Channels.Digital2 = ChannelConfiguration.DigitalInput; // RV Low Switch
            ////Dataq.Channels.Digital3 = ChannelConfiguration.DigitalInput; // RV High Swtich


            Dataq.ConfigureChannels();

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



            while (InstantaneousRead.Analog0 == null) 
            { Thread.Sleep(100); }

            //ColorChanger.Start();
            Dataq.Functions.SetLedColor(LEDColor.Magenta);

            Console.CursorVisible = false;
            while (true)
            {
                try
                {
                    Console.SetCursorPosition(0, 0);
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.Write(Math.Round(InstantaneousRead.Analog0.Value.Value, 2).ToString("0.00") + " ");

                    Console.ForegroundColor = ConsoleColor.DarkGreen;
                    Console.Write(Math.Round(InstantaneousRead.Analog1.Value.Value, 4) + " ");

                    Console.ForegroundColor = ConsoleColor.White;
                    Console.Write(" 1:" + InstantaneousRead.Digital0 + " 2:" + InstantaneousRead.Digital1 + " 3:" + InstantaneousRead.Digital2 + " 4:" + InstantaneousRead.Digital3 + "                             \r\n") ;

                    Console.WriteLine(DateTime.Now);

                    Debug.WriteLine(Math.Round(InstantaneousRead.Analog0.Value.Value, 2).ToString("0.00") + " " + Math.Round(InstantaneousRead.Analog1.Value.Value, 4) + " " + " 1:" + InstantaneousRead.Digital0 + " 2:" + InstantaneousRead.Digital1 + " 3:" + InstantaneousRead.Digital2 + " 4:" + InstantaneousRead.Digital3);

                    Thread.Sleep(100);
                }
                catch { }
                //Thread.Sleep(100);
            }
        }
    }
}
