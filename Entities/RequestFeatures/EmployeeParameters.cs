﻿using System;
using System.Collections.Generic;
using System.Text;

namespace Entities.RequestFeatures
{
    public class EmployeeParameters : RequestParameters
    {
        public uint MinAge { get; set; }
        public uint MaxAge { get; set; } = uint.MaxValue;

        public bool ValidAgeRange => MaxAge > MinAge;
    }
}
