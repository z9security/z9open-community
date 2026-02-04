using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using Z9.Spcore.Proto;

namespace Z9.Protobuf
{
    /// <summary>
    /// Connection information for adding an SpCoreControllerState
    /// </summary>
    public class SpCoreControllerStateConnectionInfo
    {
        public Dev Controller { get; set; }
        public ISslSettings SslSettings { get; set; }
        public ISpCoreTakeoverStorage TakeoverStorage { get; set; }
        public X509Certificate2 IdentificationCertificate { get; set; }
        /// <summary>
        /// Licenses intended for the (secondary) controller.  This may be used as an alternative to loading the licenses directly to the device.
        /// This may be used for the "SDK Access" license.
        /// </summary>
        public List<String> Licenses { get; set; } = new List<String>();
    }
}
