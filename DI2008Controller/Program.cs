using LibUsbDotNet;
using LibUsbDotNet.Main;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;

namespace DI2008Controller
{


    public class DI2008
    {        
        private static UsbDevice DI_2008; 
        public static UsbEndpointWriter Writer;
        public static UsbEndpointReader Reader;
        public static List<Channel> CurrentConfig = new List<Channel>();
        public static Functions InternalFunctions = new Functions();
        public Functions Functions = new Functions();
        public Channels Channels = new Channels();
        public DeviceInfo DeviceInfo = new DeviceInfo();
        //public List<DeviceInfo> AvailableDevices { get; set; }

        /// <summary>
        /// This class is designed to conceptually represent a physical DI2008 as closely as possible. It can be connected, disconnected,
        /// configured per channel, set the led color and spit data out fast AF
        /// </summary>
        public DI2008()
        {
            //UsbRegDeviceList Devices = UsbDevice.AllDevices;
            
            //foreach (UsbDevice device in Devices)
            //{
            //    var FoundDevice = new DeviceInfo();
            //    //FoundDevice.Serial = device.UsbRegistryInfo.


            //    //AvailableDevices.Add(FoundDevice);
            //}
        }


        /// <summary>
        /// Connect to the first available Dataq -- Useful if there will only ever be 1 Dataq per PC
        /// </summary>
        public void Connect()
        {
            AppDomain.CurrentDomain.ProcessExit += new EventHandler(OnApplicationExit);

            var Devices = UsbDevice.AllDevices;
            var Finder = new UsbDeviceFinder(Devices[0].Vid, Devices[0].Pid);
            DI_2008 = UsbDevice.OpenUsbDevice(Finder);

            if (!DI_2008.IsOpen) 
            {
                DI_2008.Open();
            }

                                  
            Writer = DI_2008.OpenEndpointWriter(WriteEndpointID.Ep01);
            Reader = DI_2008.OpenEndpointReader(ReadEndpointID.Ep01);

            InternalFunctions.Write("stop"); //Make sure the device wasnt left in a scan state and clear all channels
            //InternalFunctions.Write("slist 0 0");

            DeviceInfo.Serial = InternalFunctions.Write("info 6");
            DeviceInfo.FirmwareVersion = InternalFunctions.Write("info 2");
            DeviceInfo.PID = DI_2008.UsbRegistryInfo.Pid;
            DeviceInfo.VID = DI_2008.UsbRegistryInfo.Vid;
        }
        /// <summary>
        /// Connect a dataq based on its serial number -- Useful if there will be multiple Dataqs on a PC at one time
        /// </summary>
        /// <param name="SerialNumber"></param>
        public void Connect(string SerialNumber)
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
                        var Command = Functions.GetAnalogChannelCommand(ScanListPosition, ChannelID, Configuration);
                        ConfigCommands.Add(Command);
                        ScanListPosition -= -1;
                    }
                    else if (ChannelType == "Digital")
                    {
                        DigitalChannelCommand += Functions.GetDigitalIOCommand(ChannelID, Configuration);
                    }

                    
                }
            }
            ConfigCommands.Add("endo " + DigitalChannelCommand);

            foreach (string Command in ConfigCommands)
            {
                InternalFunctions.Write(Command);
            }
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
