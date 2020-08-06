using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Azure;
using Azure.Storage;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using DemoFileService.Repositories;
using FluentAssertions;
using Moq;
using Xunit;

namespace DemoFileServiceTests
{
    public class FileRepositoryTests
    {
        public class DeleteFile
        {
            [Theory]
            [InlineData(true)]
            [InlineData(false)]
            public async Task ShouldIndicateResultOfStorageAction(bool fileDeleted)
            {
                var azResponse = new Mock<Response<bool>>();
                azResponse.SetupGet(c => c.Value).Returns(fileDeleted);
                var blobClient = new Mock<BlobContainerClient>();
                blobClient.Setup(c => c.DeleteBlobIfExistsAsync(It.IsAny<string>(),
                    It.IsAny<DeleteSnapshotsOption>(),
                    It.IsAny<BlobRequestConditions>(),
                    It.IsAny<CancellationToken>())).ReturnsAsync(azResponse.Object);
                var subject = new FileRepository(blobClient.Object);

                using(var token = new CancellationTokenSource())
                {
                    var response = await subject.DeleteFile("test", token.Token);
                    response.Should().Be(fileDeleted);
                }
            }

            [Fact]
            public async Task ShouldReturnFalseWhenRequestFailedException()
            {
                var blobClient = new Mock<BlobContainerClient>();
                 blobClient.Setup(c => c.DeleteBlobIfExistsAsync(It.IsAny<string>(),
                    It.IsAny<DeleteSnapshotsOption>(),
                    It.IsAny<BlobRequestConditions>(),
                    It.IsAny<CancellationToken>()))
                    .ThrowsAsync(new RequestFailedException("failed"));
                var subject = new FileRepository(blobClient.Object);

                using(var token = new CancellationTokenSource())
                {
                    var response = await subject.DeleteFile("test", token.Token);
                    response.Should().Be(false);
                }
            }
        }

        public class GetFile
        {
            [Fact]
            public async Task ShouldReturnTrueWhenFileExists()
            {
                var azResponse = new Mock<Response<bool>>();
                azResponse.SetupGet(c => c.Value).Returns(true);
                
                var blobClient = new Mock<BlobClient>();
                blobClient.Setup(c => c.ExistsAsync(It.IsAny<CancellationToken>()))
                    .ReturnsAsync(azResponse.Object);

                blobClient.Setup(c => c.DownloadToAsync(It.IsAny<Stream>(),
                                    It.IsAny<CancellationToken>()))
                    .ReturnsAsync(new Mock<Response>().Object);
                
                var blobContainerClient = new Mock<BlobContainerClient>();
                blobContainerClient.Setup(c => c.GetBlobClient(It.IsAny<string>()))
                    .Returns(blobClient.Object);

                var subject = new FileRepository(blobContainerClient.Object);

                using(var ms = new MemoryStream())
                using(var token = new CancellationTokenSource())
                {
                    var response = await subject.GetFile("file", ms, token.Token);
                    response.Should().BeTrue();
                }

            }

            [Fact]
            public async Task ShouldReturnFalseWhenFileDoesNotExist()
            {
                var azResponse = new Mock<Response<bool>>();
                azResponse.SetupGet(c => c.Value).Returns(false);
                
                var blobClient = new Mock<BlobClient>();
                blobClient.Setup(c => c.ExistsAsync(It.IsAny<CancellationToken>()))
                    .ReturnsAsync(azResponse.Object);

                blobClient.Setup(c => c.DownloadToAsync(It.IsAny<Stream>(),
                                    It.IsAny<CancellationToken>()))
                    .ReturnsAsync(new Mock<Response>().Object);
                
                var blobContainerClient = new Mock<BlobContainerClient>();
                blobContainerClient.Setup(c => c.GetBlobClient(It.IsAny<string>()))
                    .Returns(blobClient.Object);

                var subject = new FileRepository(blobContainerClient.Object);

                using(var ms = new MemoryStream())
                using(var token = new CancellationTokenSource())
                {
                    var response = await subject.GetFile("file", ms, token.Token);
                    response.Should().BeFalse();
                }
            }

            [Fact]
            public async Task ShouldReturnFalseWhenRequestFailedException()
            {
                var azResponse = new Mock<Response<bool>>();
                azResponse.SetupGet(c => c.Value).Returns(true);
                
                var blobClient = new Mock<BlobClient>();
                blobClient.Setup(c => c.ExistsAsync(It.IsAny<CancellationToken>()))
                    .ReturnsAsync(azResponse.Object);

                blobClient.Setup(c => c.DownloadToAsync(It.IsAny<Stream>(),
                                    It.IsAny<CancellationToken>()))
                    .ThrowsAsync(new RequestFailedException("failed"));
                
                var blobContainerClient = new Mock<BlobContainerClient>();
                blobContainerClient.Setup(c => c.GetBlobClient(It.IsAny<string>()))
                    .Returns(blobClient.Object);

                var subject = new FileRepository(blobContainerClient.Object);

                using(var ms = new MemoryStream())
                using(var token = new CancellationTokenSource())
                {
                    var response = await subject.GetFile("file", ms, token.Token);
                    response.Should().BeFalse();
                }
            }
        }        

        public class GetFileListing
        {
            [Fact]
            public async Task ShouldReturnEmptyListWhenException()
            {
                var blobClient = new Mock<BlobContainerClient>();
                blobClient.Setup(c => c.GetBlobsAsync(It.IsAny<BlobTraits>(), It.IsAny<BlobStates>(), 
                        It.IsAny<string>(), It.IsAny<CancellationToken>()))
                    .Throws(new RequestFailedException("failed"));
                var subject = new FileRepository(blobClient.Object);

                using(var token = new CancellationTokenSource())
                {
                    var response = await subject.GetFileListing(token.Token);
                    response.Should().BeEmpty();
                }
            }

            public static async IAsyncEnumerator<BlobItem> GetTestValues()
            {
                yield return BlobsModelFactory.BlobItem("file1", false, null);
                yield return BlobsModelFactory.BlobItem("file2", false, null);
                await Task.CompletedTask;
            }
            [Fact]
            public async Task ShouldReturnList()
            {                            
                var pageableMock = new Mock<AsyncPageable<BlobItem>>();
                pageableMock.Setup(c => c.GetAsyncEnumerator(It.IsAny<CancellationToken>()))
                    .Returns(GetFileListing.GetTestValues());
                var blobClient = new Mock<BlobContainerClient>();
                blobClient.Setup(c => c.GetBlobsAsync(It.IsAny<BlobTraits>(), It.IsAny<BlobStates>(), 
                        It.IsAny<string>(), It.IsAny<CancellationToken>()))
                    .Returns(pageableMock.Object);
                var subject = new FileRepository(blobClient.Object);

                using(var token = new CancellationTokenSource())
                {
                    var response = await subject.GetFileListing(token.Token);
                    response.Should().HaveCount(2);
                }                
            }
        }

        public class Save
        {
            [Theory]
            [InlineData(null)]
            [InlineData("")]
            [InlineData(" ")]
            public async Task ShouldThrowWhenInvalidFilename(string name)
            {
                var blobClient = new Mock<BlobContainerClient>();
                var subject = new FileRepository(blobClient.Object);

                using(var ms = new MemoryStream())
                using(var token = new CancellationTokenSource())
                {
                    Func<Task> act = async () => {
                        await subject.Save(name, ms, token.Token);
                    };

                    await act.Should().ThrowExactlyAsync<ArgumentException>();
                }
            }

            [Fact]
            public async Task ShouldReturnTrueWhenUploaded()
            {
                var azResponse = new Mock<Response<BlobContentInfo>>();
                azResponse.SetupGet(c => c.Value).Returns(
                    BlobsModelFactory.BlobContentInfo(new ETag("file"),DateTimeOffset.MinValue, null, null, null, 0));
                var blobClient = new Mock<BlobContainerClient>();
                blobClient.Setup(c => c.UploadBlobAsync("file1", It.IsAny<Stream>(),
                                It.IsAny<CancellationToken>()))
                    .ReturnsAsync(azResponse.Object);
                    
                var subject = new FileRepository(blobClient.Object);
            
                using(var ms = new MemoryStream())
                using(var token = new CancellationTokenSource())
                {
                    var response = await subject.Save("file", ms, token.Token);
                    response.Should().BeTrue();
                }
            }


            [Fact]
            public async Task ShouldReturnFalseWhenStorageException()
            {
                var blobClient = new Mock<BlobContainerClient>();
                blobClient.Setup(c => c.UploadBlobAsync(It.IsAny<string>(), It.IsAny<Stream>(),
                                It.IsAny<CancellationToken>()))
                    .ThrowsAsync(new RequestFailedException("failed"));
                    
                var subject = new FileRepository(blobClient.Object);
            
                using(var ms = new MemoryStream())
                using(var token = new CancellationTokenSource())
                {
                    var response = await subject.Save("file", ms, token.Token);
                    response.Should().BeFalse();
                }
            }
        }
    }
}