using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.IO;

namespace TeeTime.Services.Logging
{
    public class RequestResponseLoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILoggerService _logger;
        private readonly RecyclableMemoryStreamManager _recyclableMemoryStreamManager;
        private readonly bool _isEnabled;

        public RequestResponseLoggingMiddleware(
            RequestDelegate next,
            IConfiguration configuration)
        {
            _next = next;
            _logger = LoggerFactory.CreateLogger("RequestResponse");
            _recyclableMemoryStreamManager = new RecyclableMemoryStreamManager();
            
            // Check if request/response logging is enabled
            _isEnabled = configuration.GetValue<bool>("Logging:EnableRequestResponseLogging", false);
        }

        public async Task InvokeAsync(HttpContext context)
        {
            if (!_isEnabled)
            {
                await _next(context);
                return;
            }

            // Generate correlation ID for request
            string correlationId = Guid.NewGuid().ToString();
            _logger.SetCorrelationId(correlationId);
            context.Items["CorrelationId"] = correlationId;

            // Log the request
            await LogRequest(context);
            
            // Capture and log the response
            await LogResponse(context);
        }

        private async Task LogRequest(HttpContext context)
        {
            context.Request.EnableBuffering();
            
            var requestPath = context.Request.Path;
            var method = context.Request.Method;
            
            _logger.Info($"HTTP {method} {requestPath} received");

            var contentType = context.Request.ContentType;
            if (contentType != null && ShouldCaptureBody(contentType))
            {
                using var requestStream = _recyclableMemoryStreamManager.GetStream();
                await context.Request.Body.CopyToAsync(requestStream);
                
                var body = ReadStreamInChunks(requestStream);
                _logger.Debug($"Request Body: {body}");
                
                context.Request.Body.Position = 0;
            }
        }

        private async Task LogResponse(HttpContext context)
        {
            var originalBodyStream = context.Response.Body;
            
            await using var responseBody = _recyclableMemoryStreamManager.GetStream();
            context.Response.Body = responseBody;
            
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "An unhandled exception occurred during request processing");
                throw;
            }
            
            context.Response.Body.Seek(0, SeekOrigin.Begin);
            var responseBodyText = await new StreamReader(context.Response.Body).ReadToEndAsync();
            context.Response.Body.Seek(0, SeekOrigin.Begin);
            
            var statusCode = context.Response.StatusCode;
            _logger.Info($"HTTP {statusCode} returned for {context.Request.Method} {context.Request.Path}");
            
            var contentType = context.Response.ContentType;
            if (contentType != null && ShouldCaptureBody(contentType) && !string.IsNullOrEmpty(responseBodyText))
            {
                if (responseBodyText.Length > 1000) // Avoid logging large responses
                {
                    _logger.Debug($"Response Body (truncated): {responseBodyText.Substring(0, 1000)}...");
                }
                else
                {
                    _logger.Debug($"Response Body: {responseBodyText}");
                }
            }
            
            await responseBody.CopyToAsync(originalBodyStream);
        }

        private static string ReadStreamInChunks(Stream stream)
        {
            const int readChunkBufferLength = 4096;
            stream.Seek(0, SeekOrigin.Begin);
            
            using var textWriter = new StringWriter();
            using var reader = new StreamReader(stream);
            
            var readChunk = new char[readChunkBufferLength];
            int readChunkLength;
            
            do
            {
                readChunkLength = reader.ReadBlock(readChunk, 0, readChunkBufferLength);
                textWriter.Write(readChunk, 0, readChunkLength);
            } while (readChunkLength > 0);
            
            return textWriter.ToString();
        }

        private bool ShouldCaptureBody(string contentType)
        {
            if (string.IsNullOrEmpty(contentType))
                return false;
                
            return contentType.Contains("application/json") ||
                   contentType.Contains("application/xml") ||
                   contentType.Contains("text/plain") ||
                   contentType.Contains("text/xml") ||
                   contentType.Contains("application/x-www-form-urlencoded");
        }
    }
} 