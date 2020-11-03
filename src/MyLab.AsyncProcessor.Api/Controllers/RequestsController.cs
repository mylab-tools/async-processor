using System.Buffers;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using MyLab.AsyncProcessor.Api.Services;
using MyLab.AsyncProcessor.Sdk;
using MyLab.WebErrors;

namespace MyLab.AsyncProcessor.Api.Controllers
{
    [Route("v1/[controller]")]
    [ApiController]
    public class RequestsController : ControllerBase
    {
        private readonly Logic _logic;

        public RequestsController(Logic logic)
        {
            _logic = logic;
        }

        [HttpPost]
        public async Task<IActionResult> Create(CreateRequest request)
        {
            var newId = await _logic.RegisterNewRequestAsync();
            _logic.SendRequestToProcessor(newId, request);

            return Ok(newId);
        }

        [ErrorToResponse(typeof(RequestNotFoundException), HttpStatusCode.NotFound)]
        [HttpGet("{id}/status")]
        public async Task<IActionResult> GetStatus([FromRoute]string id)
        {
            var status = await _logic.GetStatusAsync(id);

            if (status == null) 
                return NotFound();

            return Ok(status);
        }

        [ErrorToResponse(typeof(RequestNotFoundException), HttpStatusCode.NotFound)]
        [HttpPut("{id}/status/biz-step")]
        public async Task<IActionResult> UpdateBizStep([FromRoute] string id, [FromBody]string bizStep)
        {
            await _logic.SetBizStepAsync(id, bizStep);

            return Ok();
        }

        [ErrorToResponse(typeof(RequestNotFoundException), HttpStatusCode.NotFound)]
        [HttpPut("{id}/status/error")]
        public async Task<IActionResult> SetError([FromRoute] string id, [FromBody] ProcessingError error)
        {
            await _logic.GetStatusAsync(id);

            return Ok();
        }

        [ErrorToResponse(typeof(RequestNotFoundException), HttpStatusCode.NotFound)]
        [ErrorToResponse(typeof(UnsupportedMediaTypeException), HttpStatusCode.UnsupportedMediaType)]
        [HttpPut("{id}/result")]
        [Consumes("application/json", "text/plain", "application/octet-stream")]
        public async Task<IActionResult> SetResultJson([FromRoute] string id)
        {
            var content = await Request.BodyReader.ReadAsync();
            var mimeType = Request.ContentType;

            await _logic.SetResultAsync(id, content.Buffer.ToArray(), mimeType);

            return Ok();
        }

        [ErrorToResponse(typeof(RequestNotFoundException), HttpStatusCode.NotFound)]
        [HttpGet("{id}/result")]
        [Produces("application/octet-stream", "text/plain", "application/json")]
        public async Task<IActionResult> GetResult([FromRoute] string id)
        {
            var res = await _logic.GetResultAsync(id);

            return Ok(res.ToHttpContent());
        }
    }
}
