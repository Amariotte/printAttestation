using System.Text;

namespace InteroperabiliteProject.Middleware
{
    public class InterceptRequest
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<InterceptRequest> _logger;

        public InterceptRequest(RequestDelegate next, ILogger<InterceptRequest> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            context.Request.EnableBuffering();

            var requestBody = await ReadRequestBodyAsync(context.Request);

            _logger.LogInformation($"Request: Method [ {context.Request.Method} ] {Environment.NewLine} Path :[ {context.Request.Path} ] {Environment.NewLine} Header :[ {GetHeadersString(context.Request.Headers)} ] {Environment.NewLine} Contenue Requette [ {requestBody} ]");

            context.Request.Body.Position = 0;

            await _next(context);

            var ret = new StreamReader(context.Response.Body).ReadToEndAsync();
            _logger.LogInformation($"Response: Method [ {context.Response.StatusCode} ] {Environment.NewLine}  BODY :[ {ret} ]");
        }

        private async Task<string> ReadRequestBodyAsync(HttpRequest request)
        {
            request.Body.Position = 0;
            using (var reader = new StreamReader(request.Body, Encoding.UTF8, detectEncodingFromByteOrderMarks: false, bufferSize: 1024, leaveOpen: true))
            {
                var body = await reader.ReadToEndAsync();
                return body;
            }
        }


        private string GetHeadersString(IHeaderDictionary headers)
        {
            var headersStringBuilder = new StringBuilder();
            foreach (var header in headers)
            {
                headersStringBuilder.AppendLine($"{header.Key}: {header.Value}");
            }
            return headersStringBuilder.ToString();
        }


    }
}
