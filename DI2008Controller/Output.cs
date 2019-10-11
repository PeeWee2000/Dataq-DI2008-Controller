﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DI2008Controller
{
    public struct Data
    {
        public ChannelConfiguration ChannelConfiguration { get; set; }
        public double Value { get; set; }
        public string Unit { get; set; }
    }

    public class Output 
    {
        public Output() { }

        public  Data? Analog0 { get; set; }
        public  Data? Analog1 { get; set; }
        public  Data? Analog2 { get; set; }
        public  Data? Analog3 { get; set; }
        public  Data? Analog4 { get; set; }
        public  Data? Analog5 { get; set; }
        public  Data? Analog6 { get; set; }
        public  Data? Analog7 { get; set; }
        public  Data? Digital0 { get; set; }
        public  Data? Digital1 { get; set; }
        public  Data? Digital2 { get; set; }
        public  Data? Digital3 { get; set; }
        public  Data? Digital4 { get; set; }
        public  Data? Digital5 { get; set; }
    }
}
