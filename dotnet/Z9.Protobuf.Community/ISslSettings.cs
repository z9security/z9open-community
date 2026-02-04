using System.Collections.Generic;
using System.Security.Authentication;

namespace Z9.Protobuf
{
    public interface ISslSettings
    {
        /// <summary>
        /// Use SSL encryption for network communications
        /// </summary>
        bool UseSslEncryption { get; }

        /// <summary>
        /// Ignore SSL certificate errors
        /// </summary>
        bool IgnoreSslErrors { get; }

        /// <summary>
        /// Map Z9 IP panel address to certificate name
        /// </summary>
        Dictionary<string, string> CertificateMappings { get; }

        /// <summary>
        /// An enum specifying protocols allowed for SSL. Peers using other protocols will be denied.
        /// Usage is as: SslProtocols protocols = SslProtocols.Tls11 | SslProtocols.Tls12
        /// </summary>
        SslProtocols EnabledSslProtocols { get; }
    }
}

