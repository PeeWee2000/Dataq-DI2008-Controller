using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace DI2008Controller
{
    public class Functions
    {
        Thread Reader;
        private Output Data = new Output();
        private bool StopRequest = false;

        public void SetLedColor(LEDColor Color)
        {
            string Command = "led " + (int)Color;
            Write(Command);
        }

        public string Write(string Command)
        {
            if (Command.EndsWith("\r") == true)
            {
                Command = Command.Remove(Command.Length - 1, 1);
            }
            byte[] BytesToWrite = ASCIIEncoding.ASCII.GetBytes(Command + "\r");
            DI2008.Writer.Write(BytesToWrite, 3000, out var BytesWritten);

            var readBuffer = new byte[64];
            DI2008.Reader.Read(readBuffer, 3000, out var BytesRead);

            var Output = Encoding.ASCII.GetString(readBuffer);
            Output = Output.Replace(Command, "");
            Output = Output.Replace(" ", "");
            Output = Output.Replace("\0", "");
            Output = Output.Replace("\r", "");
            return Output;
        }

        public void StartAcquiringData()
        {
            Write("start 0");


            int BitsPerCycle = 0;

            foreach (var EnabledChannel in DI2008.CurrentConfig)
            {
                BitsPerCycle += 16;
            }

            Reader = new Thread(() =>
            {
                while (StopRequest == false)
                { 
                    var readBuffer = new byte[64]; //Needs additional math to accomodate digital channels
                    DI2008.Reader.Read(readBuffer, 10000, out var BytesRead);
                    DI2008.Reader.Read(readBuffer, 10000, out var BytesRead2);

                    int i = 0;
                    var ADCValues = new List<int>();
                    for (i = 0; i < readBuffer.Count(); i += 2)
                    {
                        short ADC = BitConverter.ToInt16(readBuffer, i);
                        if (ADC != 0)
                        { ADCValues.Add(ADC); }
                    }


                    for (i =0; i < DI2008.CurrentConfig.Count; i -= -1)
                    {
                        double ActualValue = 0;

                        var ChannelType = DI2008.CurrentConfig[i].ChannelConfiguration;
                        var ChannelName = DI2008.CurrentConfig[i].ChannelID.ToString();

                        if(ChannelType.ToString().Contains("TC"))
                        {
                            ActualValue = GetTemperature(ADCValues[i], ChannelType);
                        }
                        else
                        {
                            ActualValue = GetVoltage(ADCValues[i], ChannelType);
                        }

                        var ChannelData = new Data();
                        ChannelData.ChannelConfiguration = ChannelType;
                        ChannelData.Value = ActualValue;

                        Data.GetType().GetProperty(ChannelName).SetValue(Data, ChannelData);
                    }


                }
            });

            Reader.Start();
        }

        private double GetVoltage(int ADC, ChannelConfiguration ChannelType)
        {
            return ADC;
        }

        public void StopAcquiringData()
        {
            StopRequest = true; 
            Write("stop");
        }
        public Output ReadData()
        {
            lock (Data)
            { return Data; }           
        }





        public static double GetTemperature(int ADC, ChannelConfiguration ThermocoupleType)
        {
            //Degrees Celsius = (m*V) + B 
            //V is the ADC value and m/B are constants relative to the thermocouple type
            //Reference the DI-2008 documentation on Page 18 for more details
            double mValue = 0;
            int BValue = 0;

            switch (ThermocoupleType)
            {
                case ChannelConfiguration.BTypeTC:
                    mValue = 0.023956;
                    BValue = 1035;
                    break;
                case ChannelConfiguration.ETypeTC:
                    mValue = 0.018311;
                    BValue = 400;
                    break;
                case ChannelConfiguration.JTypeTC:
                    mValue = 0.021515;
                    BValue = 495;
                    break;
                case ChannelConfiguration.KTypeTC:
                    mValue = 0.023987;
                    BValue = 586;
                    break;
                case ChannelConfiguration.NTypeTC:
                    mValue = 0.022888;
                    BValue = 550;
                    break;
                case ChannelConfiguration.RTypeTC:
                    mValue = 0.02774;
                    BValue = 859;
                    break;
                case ChannelConfiguration.STypeTC:
                    mValue = 0.02774;
                    BValue = 859;
                    break;
                case ChannelConfiguration.TTypeTC:
                    mValue = 0.009155;
                    BValue = 100;
                    break;
            }

            double Temperature = mValue * ADC + BValue;

            return Temperature;
        }

        //Binary string builder function based on the table on page 8 of the DI-2008 documentation
        public static string GetScanListCommand(int ListPosition, int ChannelID, ChannelConfiguration ChannelConfiguration)
        {
            int Config = (int)ChannelConfiguration;

            string Command = "slist " + ListPosition.ToString() + " ";

            string Mode = Config >= 13 && Config <= 20 ? "1" : "0";
            string Range = Config >= 7 && Config <= 12 ? "1" : "0";

            string Scale = "";
            Scale += (Config == 1 || Config == 7 || Config == 18) ? "101" : "";
            Scale += (Config == 2 || Config == 8 || Config == 17) ? "100" : "";
            Scale += (Config == 3 || Config == 9 || Config == 16) ? "011" : "";
            Scale += (Config == 4 || Config == 10 || Config == 15) ? "010" : "";
            Scale += (Config == 5 || Config == 11 || Config == 14) ? "001" : "";
            Scale += (Config == 6 || Config == 12 || Config == 13) ? "000" : "";
            Scale += (Config == 19) ? "110" : "";
            Scale += (Config == 20) ? "111" : "";
            Scale += (Config >= 21 && Config != 23) ? "000" : "";
            Scale += (Config == 23) ? "000" : "";  //Need to implement the rate calculations


            string Channel = Convert.ToString(ChannelID, 2);
            while (Channel.Length < 8)
            { Channel = "0" + Channel; }

            int Settings = Convert.ToInt32((Mode + Range + Scale + Channel), 2);
            Command = Command + Settings;

            return Command;
        }
    }
}
