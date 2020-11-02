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
    }
}
