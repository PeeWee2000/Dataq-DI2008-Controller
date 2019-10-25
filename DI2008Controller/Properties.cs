namespace DI2008Controller
{
    public struct Channel
    {
        public ChannelID ChannelID;
        public ChannelConfiguration ChannelConfiguration;
        public int Frequency;
    }

    public class Channels
    {
        public ChannelConfiguration? Analog0 { get; set; }
        public  ChannelConfiguration? Analog1 { get; set; }
        public  ChannelConfiguration? Analog2 { get; set; }
        public  ChannelConfiguration? Analog3 { get; set; }
        public  ChannelConfiguration? Analog4 { get; set; }
        public  ChannelConfiguration? Analog5 { get; set; }
        public  ChannelConfiguration? Analog6 { get; set; }
        public  ChannelConfiguration? Analog7 { get; set; }
        public  ChannelConfiguration? Digital0 { get; set; }
        public  ChannelConfiguration? Digital1 { get; set; }
        public  ChannelConfiguration? Digital2 { get; set; }
        public  ChannelConfiguration? Digital3 { get; set; }
        public  ChannelConfiguration? Digital4 { get; set; }
        public  ChannelConfiguration? Digital5 { get; set; }
        public ChannelConfiguration? Digital6 { get; set; }

    }

    public class DeviceInfo
    {
        public  string Serial { get; set; }
        public  string FirmwareVersion { get; set; }
        public  int PID { get; set; }
        public  int VID { get; set; }
    }    
}
