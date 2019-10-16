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

        public void StopAcquiringData()
        {
            StopRequest = true;
        }

        /// <summary>
        /// Returns the last value(s) read from the Dataq based on which channels were enabled
        /// </summary>
        /// <returns></returns>
        public ReadRecord ReadData()
        {
            lock (Data)
            { return Data; }
        }

        public void SetLedColor(LEDColor Color)
        {
            string Command = "led " + (int)Color;
            Write(Command);
        }

        /// <summary>
        /// Write an ASCII command directly to the DI2008 and return the response, if any.
        /// </summary>
        public string Write(string Command)
        {
            WriteASCII(Command);
            var Response = ReadBytes();

            var TrimmedOutput = Encoding.ASCII.GetString(Response);
            TrimmedOutput = TrimmedOutput.Replace(Command, "");
            string Output;
            if (TrimmedOutput.Length == 0)
            { Output = Encoding.ASCII.GetString(Response); }
            else { Output = TrimmedOutput; }

            Output = Output.Replace("\0", "");
            Output = Output.Replace("\r", "");

            return Output;
        }

        private static void WriteBytes(byte[] BytesToWrite)
        {
            var Error = DI2008.Writer.Write(BytesToWrite, 3000, out _);
            if (Error != 0)
            { throw new Exception(Error.ToString()); }
        }
        private static void WriteASCII(string ASCII)
        {
            if (ASCII.Contains("\r") == true)
            { ASCII.Replace("\r", ""); }

            byte[] BytesToWrite = ASCIIEncoding.ASCII.GetBytes(ASCII + "\r");
            WriteBytes(BytesToWrite);
        }
        private static byte[] ReadBytes()
        {
            byte[] BytesRead = new byte[16];
            var Error = DI2008.Reader.Read(BytesRead, 3000, out _);
            if (Error != 0)
            { throw new Exception(Error.ToString()); }
            return BytesRead;
        }

        /// <summary>
        /// Starts a background thread that continuously updates an internal variable that can be read via the ReadData function
        /// </summary>
        public void StartAcquiringData()
        {
            Reader = new Thread(() =>
            {
                //Need to manually write the start command because capturing the first byte is important for synchronization
                WriteASCII("start 0");

                byte[] readBuffer = new byte[16];

                while (StopRequest == false)
                {
                    readBuffer = ReadBytes();

                    var ADCValues = Calculations.ConvertToADCValues(readBuffer);

                    if (ADCValues.Count == DI2008.EnabledAnalogChannels + 1) //+1 is for the Digital Channel bytes
                    {
                        WriteAnalogValues(ADCValues);

                        int DigitalStatusByte = ADCValues[DI2008.EnabledAnalogChannels].Item2;
                        if (DigitalStatusByte <= 128 && DigitalStatusByte >= 0)
                        {
                            WriteDigitalValues(DigitalStatusByte);
                        }
                    }
                }
                Write("stop");
            });

            Reader.Start();
        }



        private void WriteAnalogValues(List<Tuple<int, int>> ADCValues)
        {
            for (int i = 0; i < DI2008.EnabledAnalogChannels; i -= -1)
            {
                double ActualValue = 0;

                var ChannelType = DI2008.CurrentConfig[i].ChannelConfiguration;
                var ChannelName = DI2008.CurrentConfig[i].ChannelID.ToString();
                var ChannelData = new Data();

                if (ChannelType.ToString().Contains("TC"))
                {
                    ActualValue = Calculations.ConvertADCtoCelsius(ADCValues[i].Item2, ChannelType);
                    ChannelData.Unit = "°C";
                }
                else if ((int)ChannelType <= 12)
                {
                    ActualValue = Calculations.ConvertADCtoVoltage(ADCValues[i].Item2, ChannelType);
                    ChannelData.Unit = (int)ChannelType <= 8 ? "mV" : "V";
                }
                else if ((int)ChannelType >= 25)
                { }
                else
                { throw new NotImplementedException(); } //Add logic for reading counter, frequency digital inputs here

                ChannelData.ChannelConfiguration = ChannelType;
                ChannelData.Value = ActualValue;

                Data.GetType().GetProperty(ChannelName).SetValue(Data, ChannelData);
            }
        }

        private void WriteDigitalValues(int DigitalStatusByte)
        {
            Data.GetType().GetProperty("DigitalStates").SetValue(Data, DigitalStatusByte);
            var DigitalReadings = Calculations.GetDigitalChannelStates(DigitalStatusByte);
            foreach (var ChannelState in DigitalReadings)
            {
                var State = ChannelState.Item2 == true ? DigtitalState.High : DigtitalState.Low;

                Data.GetType().GetProperty("Digital" + ChannelState.Item1).SetValue(Data, State);
            }
        }

    }
}
