using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ObjectStorageExplorer.Model
{
    public class OracleObjectStoreFile
    {
        public string FileName
        {
            get;
            set;
        }
        public DateTime LastModified
        {
            get;
            set;
        }

        public long Size
        {
            get;
            set;
        }

        public string BucketName
        {
            get;
            set;
        }

        public string DownloadUrl
        {
            get;
            set;
        }
    }

    public class StorageItem
    {
        public string Name { get; set; }
        public string Path { get; set; }
        public bool IsLoaded { get; set; }
        public DateTime? LastModified { get; set; }
        public long? Size { get; set; }
        public string DownloadUrl { get; set; }
        public ObservableCollection<StorageItem> Children { get; set; } = new ObservableCollection<StorageItem>();
    }

    public class OracleStorageConfig
    {
        public string TenantId { get; set; }
        public string UserId { get; set; }
        public string Fingerprint { get; set; }
        public string PrivateKey { get; set; }
        public string Region { get; set; }
        public string BucketName { get; set; }
        public string NamespaceName { get; set; }  // Add Namespace here
    }
}
