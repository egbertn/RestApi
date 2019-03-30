using ADC.RestApiTools.Models;
using Microsoft.Extensions.Caching.Memory;
using RestSharp;
using RestSharp.Deserializers;
using RestSharp.Serialization.Json;
using System;
using System.Globalization;
using System.Text;

namespace ADC.RestApiTools
{
    internal sealed class RestSharpDeserializer : IDeserializer
    {
        private readonly JsonDeserializer _jsonDeserializer;
        internal RestSharpDeserializer()
        {
            _jsonDeserializer = new JsonDeserializer();
        }

        public T Deserialize<T>(IRestResponse response)
        {
            var body = response.RawBytes;
            var hash = response.ResponseUri.ToString().GetHashCode(); //note if redirected, is a problem

            var headers = response.Headers;

            var etagIdx = headers.IndexOfParam("ETAG");
            var expiresIdx = headers.IndexOfParam("Expires");

            // todo also get e.g. Expires: Mon, 11 Jan 2010 13:29:35 GMT
            // or Cache-Control: max-age=600
            var lastModifiedIdex = headers.IndexOfParam("Last-Modified");
            var cacheControl = headers.IndexOfParam("Cache-Control"); //max-age=<seconds>
            if (etagIdx < 0 && lastModifiedIdex < 0 && cacheControl < 0)
            {
                return _jsonDeserializer.Deserialize<T>(response);
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
                    if (parts[0].IndexOf(',') >=0) //may be private or public
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

           
            var value = new CacheEntry()
            {
                EtagValue = etagIdx >= 0 ? Encoding.UTF8.GetBytes(etagValue) : null,
                HasExpires = true,
                LastModified = lastModifiedIdex >= 0 ? Encoding.UTF8.GetBytes(lastMod) : null,
                Data = body
            };
            var options = new MemoryCacheEntryOptions
            {
                Size = value.Data.Length + (value.EtagValue?.Length ?? 0) + (value.LastModified?.Length ?? 0),
                AbsoluteExpirationRelativeToNow = expiresRelative,
                AbsoluteExpiration= offset                
            };
            
            RestSharpExtensions.Cache.Value.Set(hash, value, options);


            return _jsonDeserializer.Deserialize<T>(response);
            
        }
    }
}
