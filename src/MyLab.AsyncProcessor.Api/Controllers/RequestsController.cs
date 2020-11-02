using System;
using Microsoft.AspNetCore.Mvc;
using MyLab.AsyncProcessor.Api.DataModel;

namespace MyLab.AsyncProcessor.Api.Controllers
{
    [Route("v1/[controller]")]
    [ApiController]
    public class RequestsController : ControllerBase
    {
        [HttpPost]
        public IAsyncResult Create(CreateRequest request)
        {
            return null;
        }

        [HttpGet("{id}/status")]
        public IAsyncResult GetStatus([FromRoute]string id)
        {
            return null;
        }

        [HttpPut("{id}/status/biz-step")]
        public IAsyncResult UpdateBizStep([FromRoute] string id, [FromBody]string bizStep)
        {
            return null;
        }

        [HttpPut("{id}/status/error")]
        public IAsyncResult SetError([FromRoute] string id, [FromBody] ProcessingError error)
        {
            return null;
        }

        [HttpPut("{id}/result")]
        [Consumes("application/json")]
        public IAsyncResult SetResultJson([FromRoute] string id)
        {
            return null;
        }

        [HttpPut("{id}/result")]
        [Consumes("text/plain")]
        public IAsyncResult SetResultText([FromRoute] string id)
        {
            return null;
        }

        [HttpPut("{id}/result")]
        [Consumes("application/octet-stream")]
        public IAsyncResult SetResultBin([FromRoute] string id)
        {
            return null;
        }

        [HttpGet("{id}/result")]
        [Produces("application/octet-stream", "text/plain", "application/json")]
        public IAsyncResult GetResult([FromRoute] string id)
        {
            return null;
        }
    }
}
