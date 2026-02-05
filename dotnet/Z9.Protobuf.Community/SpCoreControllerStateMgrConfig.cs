using System.Security.Cryptography.X509Certificates;

namespace Z9.Protobuf
{
    public class SpCoreControllerStateMgrConfig
    {
        public SpCoreControllerStateMgrConfig()
        {
        }

        public SpCoreControllerStateMgrConfig(string callbackHostAddress, int listenPort, X509Certificate2 certificate = null)
        {
            CallbackHostAddress = callbackHostAddress;
            ListenPort = listenPort;
            Certificate = certificate;
        }

        public string CallbackHostAddress { get; set; }
        public string CallbackHostAddress_Secondary { get; set; }
        public int ListenPort { get; set; }
        public X509Certificate2 Certificate { get; set; }
        public bool NoTakeover { get; set; }
    }
}
