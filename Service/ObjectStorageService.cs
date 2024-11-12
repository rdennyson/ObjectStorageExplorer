using ObjectStorageExplorer.Model;
using Oci.Common;
using Oci.Common.Auth;
using Oci.ObjectstorageService;
using Oci.ObjectstorageService.Models;
using Oci.ObjectstorageService.Requests;
using Oci.ObjectstorageService.Responses;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;

namespace ObjectStorageExplorer.Service
{
    public interface IObjectStorageService
    {
        Task LoadFolderItemsAsync(string prefix, ObservableCollection<StorageItem> folders, ObservableCollection<StorageItem> files, bool loadPrefix = true);
        Task<List<StorageItem>> ListObjectsAsync(string prefix = "");
        Task<string> GetSignedDownloadUrlAsync(string objectName);
        Task<string> GetSignedUploadUrlAsync(string objectName);
        Task<string> UploadFileAsync(string fileName, byte[] fileContent);
        Task<string> UploadFileAsync(string fileName, Stream fileContent);
        Task<bool> DeleteObjectAsync(string objectName);
        Task<string> CreateFolder(string folderAndFilePath);
        Task<bool> Connect();
    }
    public class ObjectStorageService : IObjectStorageService
    {
        private readonly ObjectStorageClient _client;
        private readonly string _namespaceName;
        private readonly string _bucketName;
        private readonly Region _region;
        public ObjectStorageService(OracleStorageConfig config)
        {
            // Use SimpleAuthenticationDetailsProvider directly
            var signer = new SimpleAuthenticationDetailsProvider();
            signer.TenantId = config.TenantId;
            signer.UserId = config.UserId;
            signer.Fingerprint = config.Fingerprint;
            signer.PrivateKeySupplier = new PrivateKeySupplier(config.PrivateKey);
            _region = signer.Region = Region.FromRegionId(config.Region);

            _client = new ObjectStorageClient(signer);
            _namespaceName = config.NamespaceName;  // Get namespace from config
            _bucketName = config.BucketName;        // Get bucket name from config
        }

        public async Task<bool> Connect()
        {
            try
            {
                await _client.GetNamespace(new GetNamespaceRequest());
                return true;

            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task LoadFolderItemsAsync(string prefix, ObservableCollection<StorageItem> folders, ObservableCollection<StorageItem> files, bool loadPrefix = true)
        {
            try
            {
                var request = new ListObjectsRequest
                {
                    NamespaceName = _namespaceName,
                    BucketName = _bucketName,
                    Prefix = prefix,
                    Delimiter = "/",
                    Fields = "name,size,timeModified"
                };

                var response = await _client.ListObjects(request);
                files.Clear();
                // Add each file in this folder
                foreach (var p in response.ListObjects.Objects)
                {
                    files.Add(new StorageItem {
                        Name = Path.GetFileName(p.Name),
                        Path = p.Name,
                        LastModified = p.TimeModified,
                        Size = p.Size,
                        DownloadUrl = await GetSignedDownloadUrlAsync(p.Name)
                    });
                }
                if(loadPrefix)
                {
                    // Add each "subfolder" (prefix) in this folder
                    foreach (var prefixObj in response.ListObjects.Prefixes)
                    {
                        int lastSlashIndex = prefixObj.TrimEnd('/').LastIndexOf('/');

                        string name = lastSlashIndex >= 0 ? prefixObj.Substring(lastSlashIndex + 1) : prefixObj;
                        var folder = new StorageItem
                        {
                            Name = name.TrimEnd('/'),
                            Path = prefixObj
                        };
                        // Add a placeholder child to indicate this folder can be expanded
                        folder.Children.Add(null);
                        folders.Add(folder);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to load folder items: {ex.Message}");
            }
        }

        

        public async Task<List<StorageItem>> ListObjectsAsync(string prefix = "")
        {
            List<StorageItem> storageItems = new List<StorageItem>();
            var request = new ListObjectsRequest
            {
                NamespaceName = _namespaceName,
                BucketName = _bucketName,
                Prefix = prefix,
                Fields = "name,size,timeModified"
            };

            var response = await _client.ListObjects(request);
            foreach (var p in response.ListObjects.Objects)
            {
                
                storageItems.Add(new StorageItem
                {
                    Name = Path.GetFileName(p.Name),
                    Path = p.Name,
                    LastModified = p.TimeModified,
                    Size = p.Size,
                    DownloadUrl = await GetSignedDownloadUrlAsync(p.Name)
                });
            }

            return storageItems;
        }

        public async Task<string> GetSignedDownloadUrlAsync(string objectName)
        {
            var request = new CreatePreauthenticatedRequestRequest
            {
                NamespaceName = _namespaceName,  // Use namespace from config
                BucketName = _bucketName,        // Use bucket name from config
                CreatePreauthenticatedRequestDetails = new CreatePreauthenticatedRequestDetails
                {
                    Name = $"PAR-{objectName}-{Guid.NewGuid()}",
                    AccessType = CreatePreauthenticatedRequestDetails.AccessTypeEnum.ObjectRead,
                    ObjectName = objectName,
                    TimeExpires = DateTime.UtcNow.AddDays(1) // 10 minutes expiration
                }
            };

            var response = await _client.CreatePreauthenticatedRequest(request);
            return $"https://objectstorage.{_region.RegionId}.oraclecloud.com{response.PreauthenticatedRequest.AccessUri}";
        }

        public async Task<string> GetSignedUploadUrlAsync(string objectName)
        {
            var request = new CreatePreauthenticatedRequestRequest
            {
                NamespaceName = _namespaceName,  // Use namespace from config
                BucketName = _bucketName,        // Use bucket name from config
                CreatePreauthenticatedRequestDetails = new CreatePreauthenticatedRequestDetails
                {
                    Name = $"PAR-{objectName}-{Guid.NewGuid()}",
                    AccessType = CreatePreauthenticatedRequestDetails.AccessTypeEnum.ObjectWrite,
                    ObjectName = objectName,
                    TimeExpires = DateTime.UtcNow.AddMinutes(10) // 10 minutes expiration
                }
            };

            var response = await _client.CreatePreauthenticatedRequest(request);
            return $"https://objectstorage.{_region.RegionId}.oraclecloud.com{response.PreauthenticatedRequest.AccessUri}";
        }

        public async Task<bool> DeleteObjectAsync(string objectName)
        {
            var request = new DeleteObjectRequest
            {
                NamespaceName = _namespaceName,  // Use namespace from config
                BucketName = _bucketName,        // Use bucket name from config
                ObjectName = objectName
            };

            await _client.DeleteObject(request);
            return true;
        }

        public async Task<string> UploadFileAsync(string fileName, byte[] fileContent)
        {
            using (var memoryStream = new MemoryStream(fileContent))
            {
                // Create a PutObject request
                var putObjectRequest = new PutObjectRequest
                {
                    NamespaceName = _namespaceName,
                    BucketName = _bucketName,
                    ObjectName = fileName,
                    PutObjectBody = memoryStream, // File content as a stream
                    ContentLength = fileContent.Length
                };

                // Perform the upload
                PutObjectResponse putObjectResponse = await _client.PutObject(putObjectRequest);

                // Assuming you need the URL of the uploaded object
                string uploadedObjectUrl = $"https://objectstorage.{_region.RegionId}.oraclecloud.com/n/{_namespaceName}/b/{_bucketName}/o/{fileName}";
                return uploadedObjectUrl;
            }
        }

        public async Task<string> CreateFolder(string folderAndFilePath)
        {
            // Create a PutObjectRequest to upload an empty object
            var putObjectRequest = new PutObjectRequest
            {
                NamespaceName = _namespaceName,
                BucketName = _bucketName,
                ObjectName = folderAndFilePath,
                PutObjectBody = new MemoryStream(), // File content as a stream
                ContentLength = 0
            };

            // Send request
            PutObjectResponse response = await _client.PutObject(putObjectRequest);
            string uploadedObjectUrl = $"https://objectstorage.{_region.RegionId}.oraclecloud.com/n/{_namespaceName}/b/{_bucketName}/o/{folderAndFilePath}";
            return uploadedObjectUrl;
        }

        public async Task<string> UploadFileAsync(string fileName, Stream fileContentStream)
        {
            var putObjectRequest = new PutObjectRequest
            {
                NamespaceName = _namespaceName,
                BucketName = _bucketName,
                ObjectName = fileName,
                PutObjectBody = fileContentStream,
                ContentLength = fileContentStream.Length
            };

            // Perform the upload
            PutObjectResponse putObjectResponse = await _client.PutObject(putObjectRequest);

            // Assuming you need the URL of the uploaded object
            string uploadedObjectUrl = $"https://objectstorage.{_region.RegionId}.oraclecloud.com/n/{_namespaceName}/b/{_bucketName}/o/{fileName}";
            return uploadedObjectUrl;
        }
    }
}