using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ObjectStorageExplorer.Model
{
    public class SettingsModel
    {
        public string OptionType { get; set; } // Either "ODT" or "S3"

        // ODT.NET Compat Fields
        public string TenantId { get; set; }
        public string UserId { get; set; }
        public string Fingerprint { get; set; }
        public string PrivateKey { get; set; }
        public string Uri { get; set; }
        public string Region { get; set; }
        public string BucketName { get; set; }
        public string NamespaceName { get; set; }

        // S3 Compat Fields
        public string GatewayUrl { get; set; }
    }
}
