using System;
using System.Collections.Generic;
using System.Linq;

namespace DI2008Controller
{
    class Calculations
    {
        public static int CurrentChannel = 0;

        public static List<Tuple<int, int>> ConvertToADCValues(byte[] RawData)
        {
            int TimesToLoop = RawData.Count();
            var ADCValues = new List<Tuple<int, int>>();


            
            for (int i = 0; i < TimesToLoop; i += 2)
            {
                


                //This is inefficent and causes good reads to be ignored when there are less than 7 channels enabled, if you need higher frequency readings this will need to be optimized
                if (ADCValues.Count <= DI2008.EnabledAnalogChannels)
                { 
                    if (CurrentChannel < DI2008.EnabledAnalogChannels && CurrentChannel >= 0)
                    {
                        byte[] BytePair = new byte[2];
                        BytePair[0] = RawData[i];
                        BytePair[1] = RawData[i + 1];
                        short ADCValue = BitConverter.ToInt16(BytePair, 0);
                        ADCValues.Add(new Tuple<int, int>(CurrentChannel, ADCValue));                   

                    }
                    if (CurrentChannel == DI2008.EnabledAnalogChannels) //If all analog channels have been read, handle the digital readout then start over
                    {
                            ADCValues.Add(new Tuple<int, int>((DI2008.EnabledAnalogChannels), RawData[i + 1]));
                            
                    }
                }

                CurrentChannel = CurrentChannel == DI2008.EnabledAnalogChannels  ? 0 : CurrentChannel + 1;

            }

            ADCValues = ADCValues.OrderBy(z => z.Item1).ToList();
            return ADCValues;
        }
        public static double ConvertADCtoVoltage(int ADC, ChannelConfiguration ChannelType)
        {
            return ADC;
        }
        public static double ConvertADCtoCelsius(int ADC, ChannelConfiguration ThermocoupleType)
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
        public static string GetAnalogChannelCommand(int ListPosition, int ChannelID, ChannelConfiguration ChannelConfiguration)
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

            if ((int)ChannelConfiguration > 20)
            { Command = ""; }

            return Command;
        }


        public static int GetDigitalIOCommand(int Channel, ChannelConfiguration ChannelConfiguration)
        {
            Channel += 1;
            int Command = 0;
            if (ChannelConfiguration == ChannelConfiguration.DigitalOutput && (int)ChannelConfiguration >= 25)
            {
                Command = 1;
                for (int i = 1; i < Channel; i++)
                { Command *= 2; }
            }


            return Command;
        }



        public static List<Tuple<int, bool>> GetDigitalChannelStates(int State)
        {
            int DigitalChannels = 7;
            int TotalBits = (int)Math.Pow(2, DigitalChannels) - 1;
            List<Tuple<int, bool>> ChannelStates = new List<Tuple<int, bool>>();
            int BitMultiplier = 1;

            for (int i = 0; i < DigitalChannels; i++)
            {
                bool Status = ((Convert.ToByte(State) & BitMultiplier) == 0) ? false : true;
                var ChannelState = new Tuple<int, bool>(i, Status);
                ChannelStates.Add(ChannelState);
                BitMultiplier *= 2;
            }

            return ChannelStates;
        }
    }
}
