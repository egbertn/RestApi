using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.Extensions.Caching.Memory;
using RestSharp;

namespace ADC.RestApiTools
{
    public static class RestSharpExtensions
    {
        private static readonly IMemoryCache _cache = new MemoryCache(new MemoryCacheOptions() { ExpirationScanFrequency = TimeSpan.FromMinutes(5)  });

        public static T GetDataByHashFromRequest<T>(this RestRequest request, string baseUrl)
        {//unique request, not necesarily a valid uri
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }
            if (request.Resource == null)
            {
                throw new InvalidOperationException("request first must be initialized");
            }
            if (string.IsNullOrEmpty(baseUrl))
            {
                throw new ArgumentNullException(nameof(baseUrl));
            }
            var hash =  (baseUrl + request.Resource + (request.Parameters.Count > 0 ? string.Join("", request.Parameters.Select(s =>
              string.Concat(s.Name, s.ContentType, s.Value.ToString()))) : "")).GetHashCode();

            if (_cache.TryGetValue(hash, out (string eTag, string modifiedSince, bool hasExpires, object data) etag))
            {
                request.AddHeader("If-None-Match", etag.eTag);
                request.AddHeader("If-Last-Modified", etag.modifiedSince);
            }
            // the data is there, we allowed cache so that takes precedence
            if (etag.hasExpires == true)
                return (T)etag.data;
            else
                return default(T);
        }
        private static int IndexOfParam(IList<Parameter> parameters, string name)
        {
            var idx = parameters.Count;
            while (idx-- != 0)
            {
                if (parameters[idx].Name.Equals(name, StringComparison.InvariantCultureIgnoreCase))
                {
                    return idx;
                }
            }
            return -1;
        }
        public static void SetDataByRequest(this RestRequest request, string baseUrl, IList<Parameter> headers, object data)
        {
            var hash = (baseUrl + request.Resource + (request.Parameters.Count > 0 ? string.Join("", request.Parameters.Select(s =>
                    string.Concat(s.Name, s.ContentType, s.Value.ToString()))) : "")).GetHashCode();

            var etagIdx = IndexOfParam(headers, "ETAG");
            var expiresIdx = IndexOfParam(headers, "Expires");

            // todo also get e.g. Expires: Mon, 11 Jan 2010 13:29:35 GMT
            // or Cache-Control: max-age=600
            var lastModifiedIdex = IndexOfParam(headers, "Last-Modified");
            var cacheControl = IndexOfParam(headers, "Cache-Control"); //max-age=<seconds>
            if (etagIdx < 0 && lastModifiedIdex < 0 && cacheControl < 0)
            {
                return;
            }
            DateTimeOffset? offset = null;
            if (expiresIdx >= 0)
            {
                var directive = (string)headers[expiresIdx].Value;
                if (DateTimeOffset.TryParseExact(directive, new string[] { "o", "r", "u", "s" }, null, DateTimeStyles.None, out DateTimeOffset dto))
                {
                    offset = dto;
                }
            }
            TimeSpan? expiresRelative = null;
            if (cacheControl >= 0 && offset == null)
            {
                var directive = (string)headers[cacheControl].Value;
                if (directive.IndexOf("max-age", StringComparison.InvariantCultureIgnoreCase) >= 0)
                {
                    var parts = directive.Split('=');
                    if (parts.Length == 2 && double.TryParse(parts[1].Trim(), out double expiresSeconds))
                    {
                        expiresRelative = TimeSpan.FromSeconds(expiresSeconds);
                    }
                }
            }
            var lastMod = lastModifiedIdex >= 0 ? (string)headers[lastModifiedIdex].Value : default(string);
            var expires = expiresIdx >= 0 ? (string)headers[expiresIdx].Value : default(string);
            var etagValue = etagIdx >= 0 ? (string)headers[etagIdx].Value : default(string);

            if (expiresRelative != null)
            {
                _cache.Set(hash, (etagValue, lastMod, true, data), expiresRelative.Value);
            }
            else if (offset != null)
            {
                _cache.Set(hash, (etagValue, lastMod, true, data), offset.Value);
            }
            // cache but ETAG must be set
            else
            {
                _cache.Set(hash, (etagValue, lastMod, false, data));
            }
        }
    }
}
