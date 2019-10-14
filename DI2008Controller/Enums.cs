namespace DI2008Controller
{
    public enum ChannelReportMode
    {
        LastPoint = 0,
        Average = 1,
        Maximum = 2,
        Minimum = 3
    }
    public enum ChannelConfiguration
    {
        _10mv = 1,
        _25mv = 2,
        _50mv = 3,
        _100mv = 4,
        _250mv = 5,
        _500mv = 6,
        _1000mv = 7,
        _2500mv = 8,
        _5v = 9,
        _10v = 10,
        _25v = 11,
        _50v = 12,
        BTypeTC = 13,
        ETypeTC = 14,
        JTypeTC = 15,
        KTypeTC = 16,
        NTypeTC = 17,
        RTypeTC = 18,
        STypeTC = 19,
        TTypeTC = 20,
        Event = 21,
        Record = 22,
        Rate = 23,
        Count = 24,
        DigitalIO = 25
    }
    public enum ChannelID
    {
        Analog0 = 0,
        Analog1 = 1,
        Analog2 = 2,
        Analog3 = 3,
        Analog4 = 4,
        Analog5 = 5,
        Analog6 = 6,
        Analog7 = 7,
        Digital0 = 8,
        Digital1 = 9,
        Digital2 = 10,
        Digital3 = 11,
        Digital4 = 13,
        Digital5 = 14,
        Digital6 = 15
    }
    public enum ScanRate
    {
        KHz50 = 1,
        KHz20 = 2,
        KHz10 = 3,
        KHz5 = 4,
        KHz2 = 5,
        KHz1 = 6,
        Hz500 = 7,
        Hz200 = 8,
        Hz100 = 9,
        Hz50 = 10,
        Hz20 = 11,
        Hz10 = 12
    }
    public enum LEDColor
    {
        Off = 0,
        Blue = 1,
        Green = 2,
        Cyan = 3,
        Red = 4,
        Magenta = 5,
        Yellow = 6,
        White = 7
    }
}
