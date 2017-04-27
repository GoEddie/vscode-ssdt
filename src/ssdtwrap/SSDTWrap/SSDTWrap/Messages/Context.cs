using System;
using System.Collections.Generic;
using Microsoft.SqlServer.Dac.Model;

namespace SSDTWrap.Messages
{
    public class Context
    {
        public Guid Token;

        public Dictionary<string, object> Settings = new Dictionary<string, object>();
        public List<GenericError> Messages { get; set; }
        public SsdtConfig SsdtSettings { get; set; }
        
    }
}