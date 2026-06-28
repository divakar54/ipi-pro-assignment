using Microsoft.AspNetCore.Http;
using System;

namespace IpiPro.Api.Services
{
    public interface ITenantService
    {
        int LabId { get; }
    }

    public class TenantService : ITenantService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private int? _cachedLabId;

        public TenantService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public int LabId
        {
            get
            {
                if (_cachedLabId.HasValue)
                {
                    return _cachedLabId.Value;
                }

                var httpContext = _httpContextAccessor.HttpContext;
                if (httpContext == null)
                {
                    throw new InvalidOperationException("HttpContext is not available.");
                }

                if (!httpContext.Request.Headers.TryGetValue("X-Lab-Id", out var headerValues))
                {
                    throw new UnauthorizedAccessException("X-Lab-Id header is missing or invalid");
                }

                var headerValue = headerValues.ToString();
                if (!int.TryParse(headerValue, out var parsedLabId))
                {
                    throw new UnauthorizedAccessException("X-Lab-Id header is missing or invalid");
                }

                _cachedLabId = parsedLabId;
                return _cachedLabId.Value;
            }
        }
    }
}
