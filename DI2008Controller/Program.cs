using System;
using System.Collections.Generic;
using System.Text;
using LibUsbDotNet;
using LibUsbDotNet.Main;
using System.Linq;
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
        

        static void Main(string[] args)
        {
            Waef.Example.Waef();
            Console.ReadLine();
        }

        public void Connect()
        {
            var Devices = UsbDevice.AllDevices;
            var Finder = new UsbDeviceFinder(Devices[0].Vid, Devices[0].Pid);
            DI_2008 = UsbDevice.OpenUsbDevice(Finder);

            if (!DI_2008.IsOpen) 
            {
                DI_2008.Open();
            }

                                  
            Writer = DI_2008.OpenEndpointWriter(WriteEndpointID.Ep01);
            Reader = DI_2008.OpenEndpointReader(ReadEndpointID.Ep01);

            InternalFunctions.Write("stop"); //Make sure the device wasnt left in a scan state

            DeviceInfo.Serial = InternalFunctions.Write("info 6");
            DeviceInfo.FirmwareVersion = InternalFunctions.Write("info 2");
            DeviceInfo.PID = DI_2008.UsbRegistryInfo.Pid;
            DeviceInfo.VID = DI_2008.UsbRegistryInfo.Vid;
        }
        public void Disconnect()
        {
            DI_2008.Close(); 
        }
        public void ConfigureChannels()
        {
            var ConfigCommands = new List<string>();
            int ScanListPosition = 0;

            foreach (PropertyInfo Property in Channels.GetType().GetProperties())
            {
                if (Channels.GetType().GetProperty(Property.Name).GetValue(Channels) != null)
                {
                    ChannelConfiguration Configuration = (ChannelConfiguration)Channels.GetType().GetProperty(Property.Name).GetValue(Channels);

                    var ChannelType = Regex.Match(Property.Name, @"Analog").Value == "Analog" ? "Analog" : "Digital";
                    var Channel = Regex.Match(Property.Name, @"\d+").Value;
                    int ChannelID = Channel.Length > 0 ? Convert.ToInt32(Channel) : 0;
                    ChannelID = ChannelType == "Digital" ? ChannelID + 8 : ChannelID;

                    var Command = Functions.GetScanListCommand(ScanListPosition, ChannelID, Configuration);
                    ConfigCommands.Add(Command);

                    Channel ChannelConfig = new Channel();
                    ChannelConfig.ChannelConfiguration = Configuration;
                    ChannelConfig.ChannelID = (ChannelID)ChannelID;

                    CurrentConfig.Add(ChannelConfig);

                    ScanListPosition -= -1;
                }
            }

            foreach (string Command in ConfigCommands)
            {
                InternalFunctions.Write(Command);
            }
        }
    }
}
