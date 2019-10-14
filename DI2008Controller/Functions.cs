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
        private ReadRecord Data = new ReadRecord();
        private bool StopRequest = false;

        public void SetLedColor(LEDColor Color)
        {
            string Command = "led " + (int)Color;
            Write(Command);
        }

        public string Write(string Command)
        {
            LibUsbDotNet.Main.ErrorCode Error = LibUsbDotNet.Main.ErrorCode.Ok;
            int BytesWritten;
            int BytesRead;

            if (Command.EndsWith("\r") == true)
            {
                Command = Command.Remove(Command.Length - 1, 1);
            }
            byte[] BytesToWrite = ASCIIEncoding.ASCII.GetBytes(Command + "\r");
            Error = DI2008.Writer.Write(BytesToWrite, 3000, out BytesWritten);

            if (Error != 0)
            { throw new Exception(Error.ToString()); }

            var readBuffer = new byte[1024];
            Error = DI2008.Reader.Read(readBuffer, 3000, out BytesRead);

            if (Error != 0)
            { throw new Exception(Error.ToString()); }

            var TrimmedOutput = Encoding.ASCII.GetString(readBuffer);
            TrimmedOutput = TrimmedOutput.Replace(Command, "");
            TrimmedOutput = TrimmedOutput.Replace(" ", "");
            TrimmedOutput = TrimmedOutput.Replace("\0", "");
            TrimmedOutput = TrimmedOutput.Replace("\r", "");

            string Output;
            if (TrimmedOutput.Length == 0)
            { Output = Encoding.ASCII.GetString(readBuffer); }
            else { Output = TrimmedOutput; }
            
            return Output;
        }

        public void StartAcquiringData()
        {
            Write("stop");
            Write("start 0");

            Reader = new Thread(() =>
            {
                while (StopRequest == false)
                {
                    int EnabledChannels = DI2008.CurrentConfig.Count;
                    int CurrentChannel = 0;
                    int BytePosition = 0;
                    int BytesRead;
                    int i = 0;
                    List<Tuple<int,Byte>> AllBytesRead = new List<Tuple<int, Byte>>();

                        //The DI2008 appears to spit out no less than 16 bytes at a time, this creates issues when working with odd numbers of channels handled by the loops below
                        byte[] readBuffer = new byte[16];
                        DI2008.Reader.Read(readBuffer, 10000, out BytesRead);
                        foreach (var Value in readBuffer)
                        {

                            AllBytesRead.Add(new Tuple<int, byte>(CurrentChannel, Value));
                            BytePosition++;
                            if (BytePosition == 2)
                            {
                                CurrentChannel++;
                                if (CurrentChannel == EnabledChannels)
                                { CurrentChannel = 0; }
                                BytePosition = 0;
                            }
                        }

                    var ADCValues = new List<int>();
                    for (int x = 0; x < EnabledChannels * 2; x += 2)
                    {
                        List<byte> ValuePair = new List<byte>();
                        ValuePair.Add(AllBytesRead[x].Item2);
                        ValuePair.Add(AllBytesRead[x + 1].Item2);
                        short ADC = BitConverter.ToInt16(ValuePair.ToArray(), 0);
                        ADCValues.Add(ADC);
                    }




                    if (ADCValues.Count == DI2008.CurrentConfig.Count)
                    { 
                        for (i =0; i < DI2008.CurrentConfig.Count; i -= -1)
                        {
                            double ActualValue = 0;

                            var ChannelType = DI2008.CurrentConfig[i].ChannelConfiguration;
                            var ChannelName = DI2008.CurrentConfig[i].ChannelID.ToString();
                            var ChannelData = new Data();

                            if (ChannelType.ToString().Contains("TC"))
                            {
                                ActualValue = GetTemperature(ADCValues[i], ChannelType);
                                ChannelData.Unit = "°C";
                            }
                            else if ((int)ChannelType <= 12)
                            {
                                ActualValue = GetVoltage(ADCValues[i], ChannelType);
                                ChannelData.Unit = (int)ChannelType <= 8 ? "mV" : "V";
                            }
                            else
                            { throw new NotImplementedException(); } //Add logic for reading counter, frequency digital inputs here
                            
                            ChannelData.ChannelConfiguration = ChannelType;
                            ChannelData.Value = ActualValue;

                            Data.GetType().GetProperty(ChannelName).SetValue(Data, ChannelData);
                        }
                    }
                }
                Write("stop");
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
        }
        public ReadRecord ReadData()
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
