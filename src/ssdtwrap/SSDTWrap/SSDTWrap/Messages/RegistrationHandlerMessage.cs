namespace SSDTWrap
{
    public class RegistrationHandlerMessage : ContextSlimMessage
    {
        public string Directory { get; set; }

        public string JsonConfig { get; set; }
    }
}