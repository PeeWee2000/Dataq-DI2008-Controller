using LibUsbDotNet;
using LibUsbDotNet.Main;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace DI2008Controller
{


    public class DI2008
    {        
        static UsbDevice DI_2008; 
        public static UsbEndpointWriter Writer;
        public static UsbEndpointReader Reader;        
        public static List<Channel> CurrentConfig = new List<Channel>();
        public static int EnabledAnalogChannels;
        public static Functions InternalFunctions = new Functions();
        public  Functions Functions = new Functions();
        public Channels Channels = new Channels();
        public DeviceInfo DeviceInfo = new DeviceInfo();
        //public List<DeviceInfo> AvailableDevices { get; set; }

        /// <summary>
        /// This class is designed to conceptually represent a physical DI2008 as closely as possible. It can be connected, disconnected,
        /// configured per channel, set the led color and spit data out fast AF.
        /// By default the first found DI2008 will automatically be connected to.
        /// </summary>
        public DI2008()
        {
            AppDomain.CurrentDomain.ProcessExit += new EventHandler(OnApplicationExit);

            var Devices = UsbDevice.AllDevices;
            var Finder = new UsbDeviceFinder(Devices[0].Vid, Devices[0].Pid);
            DI_2008 = UsbDevice.OpenUsbDevice(Finder);

            if (!DI_2008.IsOpen)
            {
                DI_2008.Open();
            }

            InitalizeAndVerify();

            DeviceInfo.Serial = InternalFunctions.Write("info 6");
            DeviceInfo.FirmwareVersion = InternalFunctions.Write("info 2");
            InternalFunctions.Write("ps 1");

            DeviceInfo.PID = DI_2008.UsbRegistryInfo.Pid;
            DeviceInfo.VID = DI_2008.UsbRegistryInfo.Vid;
        }


        private static void InitalizeAndVerify()
        {
            bool Success = false;
            while (Success == false)
            {
                Writer = DI_2008.OpenEndpointWriter(WriteEndpointID.Ep01);
                Reader = DI_2008.OpenEndpointReader(ReadEndpointID.Ep01);



                //No clue what these do but they keep the device from hanging on program restarts #TrialAndError
                Writer.Abort();
                Writer.Reset();
                Writer.Flush();
                Reader.Abort();
                Reader.ReadFlush();
                Reader.Flush();
                Reader.Reset();
                InternalFunctions.Write("stop"); //Make sure the device wasnt left in a scan state

                try
                {
                    var DeviceResponding = InternalFunctions.Write("info 0");
                    Success = DeviceResponding.Contains("DATAQ");
                }
                catch { }
            }
        }


        /// <summary>
        /// Connect a dataq based on its serial number -- Useful if there will be multiple Dataqs on a PC at one time
        /// </summary>
        /// <param name="SerialNumber"></param>
        public DI2008(string SerialNumber)
        {
            throw new NotImplementedException();
        }



        public void Disconnect()
        {
            DI_2008.Close(); 
        }
        public void ConfigureChannels()
        {
            var ConfigCommands = new List<string>();
            int ScanListPosition = 0;
            int DigitalChannelCommand = 0;

            foreach (PropertyInfo Property in Channels.GetType().GetProperties())
            {
                if (Channels.GetType().GetProperty(Property.Name).GetValue(Channels) != null)
                {
                    ChannelConfiguration Configuration = (ChannelConfiguration)Channels.GetType().GetProperty(Property.Name).GetValue(Channels);

                    var ChannelType = Regex.Match(Property.Name, @"Analog").Value == "Analog" ? "Analog" : "Digital";
                    var Channel = Regex.Match(Property.Name, @"\d+").Value;
                    int ChannelID = Channel.Length > 0 ? Convert.ToInt32(Channel) : 0;
                    //ChannelID = ChannelType == "Digital" ? ChannelID + 8 : ChannelID;

                    Channel ChannelConfig = new Channel();
                    ChannelConfig.ChannelConfiguration = Configuration;
                    ChannelConfig.ChannelID = (ChannelID)ChannelID;

                    CurrentConfig.Add(ChannelConfig);

                    

                    if (ChannelType == "Analog")
                    {
                        var Command = Calculations.GetAnalogChannelCommand(ScanListPosition, ChannelID, Configuration);
                        ConfigCommands.Add(Command);
                        ScanListPosition -= -1;
                    }
                    else if (ChannelType == "Digital")
                    {
                        DigitalChannelCommand += Calculations.GetDigitalIOCommand(ChannelID, Configuration);
                    }
                }
            }
            ConfigCommands.Add("slist " + ScanListPosition + " 8");
            ConfigCommands.Insert(0, "endo " + DigitalChannelCommand);
            ConfigCommands.Insert(0, "srate " + "28");
            ConfigCommands.Insert(0, "dec " + "1");

            foreach (string Command in ConfigCommands)
            {
                InternalFunctions.Write(Command);
            }
            EnabledAnalogChannels = CurrentConfig.Select(x => x.ChannelConfiguration).Where(x => (int)x <= 20).Count();
        }

        private void OnApplicationExit(object sender, EventArgs e)
        {
            try
            {
                InternalFunctions.Write("stop");
                DI_2008.Close();
            }
            catch { }
        }
    }
}
