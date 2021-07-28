using System.Buffers;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using MyLab.AsyncProcessor.Api.Services;
using MyLab.AsyncProcessor.Sdk.DataModel;
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
            var id = await _logic.RegisterNewRequestAsync(request.RequestId);
            await _logic.SendRequestToProcessorAsync(id, request);

            return Ok(id);
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
        public async Task<IActionResult> CompleteWithError([FromRoute] string id, [FromBody] ProcessingError error)
        {
            await _logic.CompleteWithErrorAsync(id, error);

            return Ok();
        }

        [ErrorToResponse(typeof(RequestNotFoundException), HttpStatusCode.NotFound)]
        [HttpPost("{id}/status/step/processing")]
        public async Task<IActionResult> MakeRequestProcessing([FromRoute] string id)
        {
            await _logic.SetRequestStepAsync(id, ProcessStep.Processing);

            return Ok();
        }

        [ErrorToResponse(typeof(RequestNotFoundException), HttpStatusCode.NotFound)]
        [HttpPost("{id}/status/step/completed")]
        public async Task<IActionResult> MakeRequestCompeted([FromRoute] string id)
        {
            await _logic.SetRequestStepAsync(id, ProcessStep.Completed);

            return Ok();
        }

        [ErrorToResponse(typeof(RequestNotFoundException), HttpStatusCode.NotFound)]
        [ErrorToResponse(typeof(UnsupportedMediaTypeException), HttpStatusCode.UnsupportedMediaType)]
        [HttpPut("{id}/result")]
        [Consumes("application/json", "application/octet-stream")]
        public async Task<IActionResult> CompleteWithResult([FromRoute] string id)
        {
            using var reader = new StreamReader(Request.BodyReader.AsStream());
            var content = await reader.ReadToEndAsync();

            var mimeType = Request.ContentType;

            if (mimeType == null)
                return new UnsupportedMediaTypeResult();

            await _logic.CompleteWithResultAsync(id, Encoding.UTF8.GetBytes(content), mimeType);

            return Ok();
        }

        [ErrorToResponse(typeof(RequestNotFoundException), HttpStatusCode.NotFound)]
        [ErrorToResponse(typeof(RequestResultNotReadyException), HttpStatusCode.BadRequest)]
        [HttpGet("{id}/result")]
        [Produces("application/octet-stream", "application/json")]
        public async Task<IActionResult> GetResult([FromRoute] string id)
        {
            var res =  await _logic.GetResultAsync(id);

            return res.ToActionResult();
        }
    }
}
