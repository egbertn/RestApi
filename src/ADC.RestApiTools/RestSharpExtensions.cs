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
        private static readonly IMemoryCache Cache = new MemoryCache(new MemoryCacheOptions() { ExpirationScanFrequency = TimeSpan.FromMinutes(5)  });
        private static readonly TimeSpan ExpiresDefault = TimeSpan.FromHours(24);
        private static int GetHashFromUri(string baseUrl, IList<Parameter> parameters)
        {
            return string.Concat(baseUrl, parameters.Count > 0 ? string.Join("", parameters.Select(s =>
              string.Concat(s.Name, s.ContentType, s.Value.ToString()))) : "").GetHashCode();
        }
        public static T GetDataByHashFromRequest<T>(this IRestRequest request, string baseUrl)
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
            var hash = GetHashFromUri(baseUrl, request.Parameters);

            if (Cache.TryGetValue(hash, out (string eTag, string modifiedSince, bool hasExpires, object data) etag))
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
        private static int IndexOfParam(this IList<Parameter> parameters, string name)
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
        public static void SetDataByRequest(this IRestRequest request, string baseUrl, IList<Parameter> headers, object data)
        {
            var hash = GetHashFromUri(baseUrl, request.Parameters);
            var etagIdx = headers.IndexOfParam( "ETAG");
            var expiresIdx = headers.IndexOfParam( "Expires");

            // todo also get e.g. Expires: Mon, 11 Jan 2010 13:29:35 GMT
            // or Cache-Control: max-age=600
            var lastModifiedIdex = headers.IndexOfParam("Last-Modified");
            var cacheControl = headers.IndexOfParam( "Cache-Control"); //max-age=<seconds>
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
                //Cache-Control: public,max-age=31536000
                if (directive.IndexOf("max-age", StringComparison.InvariantCultureIgnoreCase) >= 0)
                {
                    var parts = directive.Split('=');
                    if (parts[0].Contains(',')) //may be private or public
                    {
                        var isPrivate = parts[0].Split(',')[0].StartsWith("private", StringComparison.InvariantCultureIgnoreCase);
                        if (isPrivate)
                        {
                            parts = new string[0];//disable no caching at server side
                        }
                    }
                    // no need for trimming
                    if (parts.Length == 2 && double.TryParse(parts[1], out double expiresSeconds))
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
                Cache.Set(hash, (etagValue, lastMod, true, data), expiresRelative.Value);
            }
            else if (offset != null)
            {
                Cache.Set(hash, (etagValue, lastMod, true, data), offset.Value);
            }
            // cache but ETAG must be set
            else
            {
                //avoid stuffing memory 24 hours seems a good time unless you have billions of rows from wich billions are requested daily
                // monitor memory
                Cache.Set(hash, (etagValue, lastMod, false, data), ExpiresDefault);
            }
        }
    }
}
