using System.Collections.Generic;

namespace SSDTWrap
{
    public class ReferencesResponse : ContextSlimMessage
    {
        public List<Reference> References { get; set; }
    }
}