using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Azure;
using Azure.Storage.Blobs;

namespace DemoFileService.Repositories
{
    public class FileRepository : IFileRepository 
    {

        private readonly BlobContainerClient blobContainerClient;

        public FileRepository(BlobContainerClient blobContainerClient)
        {
            this.blobContainerClient = blobContainerClient;
        }
        
        public async Task<bool> DeleteFile(string fileName, CancellationToken cancelToken)
        {
            try
            {
                return await this.blobContainerClient.DeleteBlobIfExistsAsync(fileName, cancellationToken: cancelToken);
            }
            catch(RequestFailedException)
            {
                // log this issue
                return false;
            }
        }

        public async Task<bool> GetFile(string fileName, Stream outputStream, CancellationToken token)
        {
            var blobClient = blobContainerClient.GetBlobClient(fileName);
            if(!(await blobClient.ExistsAsync(token)))
            {
                return false;
            }

            try
            {
                await blobClient.DownloadToAsync(outputStream, token);
                return true;
            }
            catch (RequestFailedException)
            {
                // log this
                return false;
            }
        }

        public async Task<IEnumerable<string>> GetFileListing(CancellationToken token)
        {
            try {
            var blobs = new List<string>();
            await foreach(var bi in blobContainerClient.GetBlobsAsync())
            {
                blobs.Add(bi.Name);
            }
            return blobs;
            }
            catch(RequestFailedException)
            {
                return Enumerable.Empty<string>();
            }
        }

        public async Task<bool> Save(string fileName, Stream fileStream, CancellationToken cancelToken)
        {
            if(string.IsNullOrWhiteSpace(fileName))
            {
                throw new ArgumentException("filename");
            }

            try
            {
                await blobContainerClient.UploadBlobAsync(fileName, fileStream, cancelToken);
                return true;
            }
            catch (RequestFailedException)
            {
                // log this
                return false;
            }
        }
    }
}
