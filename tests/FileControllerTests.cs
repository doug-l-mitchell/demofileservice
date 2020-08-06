using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DemoFileService.Controllers;
using DemoFileService.Repositories;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Moq;
using Xunit;

namespace DemoFileServiceTests
{
    public class FileControllerTests
    {
        public class PostFile
        {
            [Theory]
            [InlineData(true)]
            [InlineData(false)]
            public async Task ShouldReturn201WhenPersisted(bool fileExists)
            {
                var repo = new Mock<IFileRepository>();
                repo.Setup(c => c.Save(It.IsAny<string>(), 
                        It.IsAny<Stream>(),
                        It.IsAny<CancellationToken>()))
                    .ReturnsAsync(fileExists);
                var formFile = new Mock<IFormFile>();
                formFile.SetupGet(c => c.FileName).Returns("file1");
                formFile.SetupGet(c => c.Length).Returns(0);
                formFile.Setup(c => c.OpenReadStream()).Returns(new MemoryStream());
                var subject = new FileController(repo.Object);

                using(var token = new CancellationTokenSource())
                {
                    var result = await subject.PostFile(formFile.Object, token.Token);
                    (result as IStatusCodeActionResult).StatusCode.Should().Be(fileExists ? 201 : 200);
                }    
            }
        }

        public class GetListings
        {
            [Fact]
            public async Task ShouldReturn204WhenNoFiles()
            {
                var repo = new Mock<IFileRepository>();
                repo.Setup(c => c.GetFileListing(It.IsAny<CancellationToken>())).ReturnsAsync(new List<string> { "file1", "file2"});
                var subject = new FileController(repo.Object);

                using(var token = new CancellationTokenSource())
                {
                    var result = await subject.GetListing(token.Token);
                    (result.Result as IStatusCodeActionResult).StatusCode.Should().Be(200);
                    ((result.Result as ObjectResult).Value as IEnumerable<string>).Count().Should().Be(2);
                }
            }

            [Fact]
            public async Task ShouldReturn200WithListOfFiles()
            {
                var repo = new Mock<IFileRepository>();
                repo.Setup(c => c.GetFileListing(It.IsAny<CancellationToken>())).ReturnsAsync(Enumerable.Empty<string>());

                var subject = new FileController(repo.Object);
                using(var token = new CancellationTokenSource())
                {
                    var result = await subject.GetListing(token.Token);
                    (result.Result as IStatusCodeActionResult).StatusCode.Should().Be(204);
                }
            }
        }

        public class GetFile
        {
            // should return 404 when file does not exist
            [Fact]
            public async Task ShouldReturn200WithData()
            {
                var repo = new Mock<IFileRepository>();
                repo.Setup(c => c.GetFile(It.IsAny<string>(), It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
                    .Callback<string, Stream, CancellationToken>((n, s, t) => {
                        s.Write(Encoding.ASCII.GetBytes("abcd"), 0, 4);
                    })
                    .ReturnsAsync(true);
                var subject = new FileController(repo.Object);
                using(var token = new CancellationTokenSource())
                {
                    var result = await subject.GetFile("file1", token.Token);
                    (result as FileResult).FileDownloadName.Should().Be("file1");
                }
            }

            [Fact]
            public async Task ShouldReturn404WhenFileDoesNotExist()
            {
                var repo = new Mock<IFileRepository>();
                repo.Setup(c => c.GetFile(It.IsAny<string>(), It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(false);
                var subject = new FileController(repo.Object);
                using(var token = new CancellationTokenSource())
                {
                    var result = await subject.GetFile("file1", token.Token);
                    (result as IStatusCodeActionResult).StatusCode.Should().Be(404);
                }
            }
        }

        public class DeleteFile
        {
            [Theory]
            [InlineData(true)]
            [InlineData(false)]
            public async Task ShouldReturn200WhenFileExists(bool fileExists)
            {
                var repo = new Mock<IFileRepository>();
                repo.Setup(c => c.DeleteFile(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(fileExists);
                var subject = new FileController(repo.Object);
                using(var token = new CancellationTokenSource())
                {
                    var result = await subject.DeleteFile("file1", token.Token);
                    (result as IStatusCodeActionResult).StatusCode.Should().Be(200);
                    (result as ObjectResult).Value.Should().Be(fileExists);
                }
            }
        }
    }
}