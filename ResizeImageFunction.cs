using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;

namespace Larslardan.Function
{
    public class ResizeImageFunction
    {
        [Function("ResizeImage")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequestData req)
        {
            // Parse query string manually (no extra package)
            var queryParams = ParseQuery(req.Url);

            if (!queryParams.TryGetValue("width", out var widthStr) ||
                !int.TryParse(widthStr, out var width) ||
                !queryParams.TryGetValue("height", out var heightStr) ||
                !int.TryParse(heightStr, out var height) ||
                width <= 0 || height <= 0)
            {
                var bad = req.CreateResponse(HttpStatusCode.BadRequest);
                await bad.WriteStringAsync("width and height query params are required and must be positive integers.");
                return bad;
            }

            // Load input image from request body
            using var image = await Image.LoadAsync(req.Body);

            // Resize
            image.Mutate(x => x.Resize(width, height));

            // Prepare response
            var res = req.CreateResponse(HttpStatusCode.OK);
            res.Headers.Add("Content-Type", "image/png");

            // Write output image
            await image.SaveAsPngAsync(res.Body);

            return res;
        }

        private static Dictionary<string, string> ParseQuery(Uri url)
        {
            var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            var query = url.Query;
            if (string.IsNullOrEmpty(query))
                return dict;

            // Trim leading '?'
            query = query.TrimStart('?');

            var pairs = query.Split('&', StringSplitOptions.RemoveEmptyEntries);
            foreach (var pair in pairs)
            {
                var parts = pair.Split('=', 2);
                var key = Uri.UnescapeDataString(parts[0]);
                var value = parts.Length > 1 ? Uri.UnescapeDataString(parts[1]) : "";
                dict[key] = value;
            }

            return dict;
        }
    }
}
