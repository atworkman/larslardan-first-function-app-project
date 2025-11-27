using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.WebUtilities;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;

namespace MyAzureFunctionApp // <- change to match your existing namespace
{
    public class ResizeImageFunction
    {
        private readonly ILogger _logger;

        public ResizeImageFunction(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<ResizeImageFunction>();
        }

        [Function("ResizeImage")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequestData req)
        {
            _logger.LogInformation("ResizeImage function processing a request.");

            // Parse query string
            var queryParams = QueryHelpers.ParseQuery(req.Url.Query);

            if (!queryParams.TryGetValue("width", out var widthValues) ||
                !int.TryParse(widthValues.ToString(), out var width) ||
                !queryParams.TryGetValue("height", out var heightValues) ||
                !int.TryParse(heightValues.ToString(), out var height) ||
                width <= 0 || height <= 0)
            {
                var bad = req.CreateResponse(HttpStatusCode.BadRequest);
                await bad.WriteStringAsync(
                    "Query params 'width' and 'height' are required and must be positive integers.");
                return bad;
            }

            try
            {
                // Load image from body
                using var image = await Image.LoadAsync(req.Body);

                // Resize
                image.Mutate(x => x.Resize(width, height));

                // Return PNG
                var res = req.CreateResponse(HttpStatusCode.OK);
                res.Headers.Add("Content-Type", "image/png");
                await image.SaveAsPngAsync(res.Body);

                return res;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resizing image.");
                var error = req.CreateResponse(HttpStatusCode.BadRequest);
                await error.WriteStringAsync("Invalid image data in request body.");
                return error;
            }
        }
    }
}