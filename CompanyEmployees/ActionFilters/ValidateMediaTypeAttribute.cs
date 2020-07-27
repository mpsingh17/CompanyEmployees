using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Net.Http.Headers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CompanyEmployees.ActionFilters
{
    public class ValidateMediaTypeAttribute : IActionFilter
    {
        public void OnActionExecuted(ActionExecutedContext context)
        { }

        public void OnActionExecuting(ActionExecutingContext context)
        {
            var isAcceptHeaderPresent = context.HttpContext.Request.Headers.ContainsKey("Accept");
            if (!isAcceptHeaderPresent)
            {
                context.Result = new BadRequestObjectResult("Accept header is missing.");
                return;
            }

            var mediaType = context.HttpContext.Request.Headers["Accept"].FirstOrDefault();
            if (!MediaTypeHeaderValue.TryParse(mediaType, out MediaTypeHeaderValue mediaTypeHeaderValue))
            {
                context.Result = new BadRequestObjectResult("Media type not present. Please add Accept header with required media type.");
                return;
            }

            context.HttpContext.Items.Add("AcceptHeaderMediaType", mediaTypeHeaderValue);
        }
    }
}
