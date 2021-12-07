using Microsoft.AspNetCore.Mvc;

namespace MyLab.AsyncProcessor.Api.Models
{
    public class RequestDetailsQueryDto
    {
        [FromQuery(Name = "include_resp")]
        public bool IncludeResponse { get; set; }
    }
}