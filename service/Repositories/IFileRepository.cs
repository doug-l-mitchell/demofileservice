using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace DemoFileService.Repositories
{
    public interface IFileRepository
    {
        Task<bool> Save(string fileName, Stream fileStream, CancellationToken cancelToken);

        Task<IEnumerable<string>> GetFileListing(CancellationToken token);

        Task<bool> GetFile(string fileName, Stream outputStream, CancellationToken token);

        Task<bool> DeleteFile(string fileName, CancellationToken cancelToken);
    }
}