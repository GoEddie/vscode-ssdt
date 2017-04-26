using System;
using System.Collections.Generic;
using SSDTWrap.Messages;

namespace SSDTWrap
{
    public static class ContextBag
    {
        public static readonly Dictionary<Guid, Context> Contexts = new Dictionary<Guid, Context>();
    }
}