using LibUsbDotNet.Main;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace DI2008Controller
{
    public class Functions
    {
        private ReadRecord Data = new ReadRecord();
        private int CurrentDigitalStates = 0;

        /// <summary>
        /// Starts a background thread that continuously updates an internal variable that can be read via the ReadData function
        /// </summary>
        public void StartAcquiringData()
        {
            WriteASCII("start 0");
            DI2008.Reader.ReadBufferSize = 64;
            DI2008.Reader.DataReceived += (ProcessReceievedData);
            DI2008.Reader.DataReceivedEnabled = true;
        }

        /// <summary>
        /// Stops the background thread that continuously updates an internal variable that can be read via the ReadData function
        /// </summary>
        public void StopAcquiringData()
        {
            DI2008.Reader.DataReceivedEnabled = false;
            WriteASCII("stop");
        }

        private void ProcessReceievedData(object sender, EndpointDataEventArgs e)
        {
            byte[] BytesReceived = e.Buffer.Take(32).ToArray();

            var ADCValues = Calculations.ConvertToADCValues(BytesReceived);
             
            WriteValues(ADCValues);                
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
            WriteASCII(Command);
        }

        public void EnableChannel(ChannelID Channel)
        {
            if (Channel.ToString().Contains("Digital"))
            {
                if (Data.Analog0 != null)
                {
                    int ActualChannelID = (int)Channel - 7;
                    int Command = CurrentDigitalStates;

                    int BitPosition = 1;
                    for (int i = 1; i < ActualChannelID; i++)
                    { BitPosition *= 2; }

                    Command = (byte)(Command & BitPosition);
                    if (Command < 128)
                    { 
                        Write("dout " + Command);
                    }
                }
            }
        }

        public void DisableChannel(ChannelID Channel)
        {
            if (Channel.ToString().Contains("Digital"))
            {
                if (Data.Analog0 != null)
                {
                    int ActualChannelID = (int)Channel - 7;
                 

                    int BitPosition = 1;
                    for (int i = 1; i < ActualChannelID; i++)
                    { BitPosition *= 2; }

                    if (BitPosition < CurrentDigitalStates)
                    {
                        int Command = CurrentDigitalStates - BitPosition;

                        Write("dout " + Command);
                    }
                }
            }
        }

        /// <summary>
        /// Write an ASCII command directly to the DI2008 and return the response, if any.
        /// </summary>
        public string Write(string Command)
        {
            WriteASCII(Command);

            if (DI2008.Reader.DataReceivedEnabled == false)
            { 
                var Response = ReadBytes();

                var Output = Encoding.ASCII.GetString(Response);
              
                Output = Output.Replace("\0", "");
                Output = Output.Replace("\r", "");

                return Output;
            }
            else
            { return "Scan in process, sent commands are not echoed"; }

        }

        void WriteBytes(byte[] BytesToWrite)
        {
            var Error = DI2008.Writer.Write(BytesToWrite, 3000, out _);
            if (Error != 0)
            { throw new Exception(Error.ToString()); }
            Thread.Sleep(100);
        }
        void WriteASCII(string ASCII)
        {
            ASCII.Replace("\r", ""); 
            byte[] BytesToWrite = ASCIIEncoding.ASCII.GetBytes(ASCII + "\r");
            WriteBytes(BytesToWrite);
        }

        byte[] ReadBytes()
        {
            int ByteCount;
            byte[] Buffer = new byte[32];

            var Error = DI2008.Reader.Read(Buffer, 3000, out ByteCount);            
            if (Error != 0)
            {
                //throw new Exception("Error trying to read from buffer, if this persists reset the device ", new Exception(Error.ToString()));
            }


            if (Encoding.ASCII.GetString(Buffer).ToString().Contains("stop 01") == true)
            {
                Thread.Sleep(1000);
                WriteASCII("start");
            }

            Buffer = Buffer.Take(ByteCount).ToArray();

            return Buffer;
        }

        private void WriteValues(List<Tuple<int, decimal>> ADCValues)
        {
            decimal ActualValue = 0;
            var ChannelData = new Data();

            var Readings = new List<Data>();


            for (int i = 0; i < DI2008.EnabledAnalogChannels; i++)
            {               
                var ChannelType = DI2008.CurrentConfig[i].ChannelConfiguration;
                var ChannelName = DI2008.CurrentConfig[i].ChannelID.ToString();
                
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
                //else if ((int)ChannelType >= 25)
                //{ }
                //else
                //{ throw new NotImplementedException(); } //Add logic for reading counter, frequency digital inputs here

                ChannelData.ChannelConfiguration = ChannelType;
                ChannelData.Value = ActualValue;

                Readings.Add(ChannelData);
            }

            while (Readings.Count < 8)
            { Readings.Add(new Data()); }

            Data.Analog0 = Readings[0];
            Data.Analog1 = Readings[1];
            Data.Analog2 = Readings[2];
            Data.Analog3 = Readings[3];
            Data.Analog4 = Readings[4];
            Data.Analog5 = Readings[5];
            Data.Analog6 = Readings[6];
            Data.Analog7 = Readings[7];

            int DigitalStatusByte = Convert.ToInt32(ADCValues[DI2008.EnabledAnalogChannels].Item2);
            CurrentDigitalStates = DigitalStatusByte;
            var DigitalReadings = Calculations.GetDigitalChannelStates(DigitalStatusByte);

            Data.Digital0 = DigitalReadings[0].Item2 == false ? DigtitalState.High : DigtitalState.Low;
            Data.Digital1 = DigitalReadings[1].Item2 == false ? DigtitalState.High : DigtitalState.Low;
            Data.Digital2 = DigitalReadings[2].Item2 == false ? DigtitalState.High : DigtitalState.Low;
            Data.Digital3 = DigitalReadings[3].Item2 == false ? DigtitalState.High : DigtitalState.Low;
            Data.Digital4 = DigitalReadings[4].Item2 == false ? DigtitalState.High : DigtitalState.Low;
            Data.Digital5 = DigitalReadings[5].Item2 == false ? DigtitalState.High : DigtitalState.Low;
            Data.Digital6 = DigitalReadings[6].Item2 == false ? DigtitalState.High : DigtitalState.Low;
        }
    }
}
