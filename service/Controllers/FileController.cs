using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using DemoFileService.Repositories;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace DemoFileService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FileController : ControllerBase
    {

        private readonly IFileRepository fileRepository;

        public FileController(IFileRepository fileRepository)
        {
            this.fileRepository = fileRepository;
        }

        [HttpPost]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status201Created)]
        public async Task<ActionResult> PostFile(IFormFile data, CancellationToken cancelToken)
        {
            using(var stream = data.OpenReadStream())
            {
                if(await fileRepository.Save(data.FileName, stream, cancelToken))
                {
                    return Created(data.FileName, null);
                }
                
                return Ok();
            }
        }

        [HttpGet]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<string>>> GetListing(CancellationToken cancelToken)
        {
            var results = await fileRepository.GetFileListing(cancelToken);
            if(results.Any()) {
                return Ok(results);
            }

            return NoContent();
        }

        [HttpGet("id/{fileName}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult> GetFile(string fileName, CancellationToken cancelToken)
        {
            var ms = new MemoryStream();
            if(await fileRepository.GetFile(fileName, ms, cancelToken))
            {
                ms.Seek(0, SeekOrigin.Begin);
                return File(ms, "application/octet-stream", fileName);
            }
            return NotFound();
        }

        [HttpDelete("id/{fileName}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> DeleteFile(string fileName, CancellationToken cancelToken)
        {
            return Ok(await fileRepository.DeleteFile(fileName, cancelToken));
        }
    }
}