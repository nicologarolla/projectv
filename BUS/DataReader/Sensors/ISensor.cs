﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataReader.Sensors
{
    interface ISensor
    {
        string ToJson(bool fermo, string ora);
    }
}
