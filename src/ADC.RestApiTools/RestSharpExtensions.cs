using ADC.RestApiTools.Models;
using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json;
using RestSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace ADC.RestApiTools
{
    public static class RestSharpExtensions
    {
        public static void ConfigHttpCache(long maxMemorySize, TimeSpan? expirationScanFrequency = null)
        {
            MaxMemorySize = maxMemorySize;
            ExpirationScanFrequency = expirationScanFrequency;
        }
        private static long? MaxMemorySize;
        private static TimeSpan? ExpirationScanFrequency;

        internal static readonly Lazy<IMemoryCache> Cache = new Lazy<IMemoryCache>(() => new MemoryCache(
              new MemoryCacheOptions()
              {
                  ExpirationScanFrequency = ExpirationScanFrequency ?? TimeSpan.FromMinutes(5),
                  SizeLimit = MaxMemorySize
              })
        );
        private static readonly TimeSpan ExpiresDefault = TimeSpan.FromHours(24);
        internal static int GetHashFromUri(string baseUrl, string resource, IList<Parameter> parameters)
        {
            var uri = new Uri(new Uri(baseUrl), new Uri(  resource, UriKind.Relative));
            
            var builder = new UriBuilder(uri)
            {
                Query = string.Join("&", parameters.Select(s =>
                  string.Concat(Uri.EscapeUriString(s.Name), "=", Uri.EscapeDataString(s.Value.ToString()))))
            };

            return builder.Uri.ToString().GetHashCode();
        }
        /// <summary>
        /// adds HTTP caching ability
        /// </summary>
        /// <param name="restClient"></param>
        public static void RestSharpHandler(this RestClient restClient)
        {
            restClient.AddHandler("application/json", ()=>new RestSharpDeserializer());
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
            var hash = GetHashFromUri(baseUrl, request.Resource, request.Parameters);

            if (Cache.Value.TryGetValue(hash, out CacheEntry etag))
            {
                if (etag.EtagValue != null)
                {
                    request.AddHeader("If-None-Match", Encoding.UTF8.GetString( etag.EtagValue));
                }
                if (etag.LastModified != null)
                {
                    request.AddHeader("If-Last-Modified", Encoding.UTF8.GetString(etag.LastModified));
                }
            }

            // the data is there, we allowed cache so that takes precedence
            if (etag.HasExpires == true)
            {
                
                var deser = new JsonSerializer();
                return deser.Deserialize<T>(new JsonTextReader(new StreamReader(new MemoryStream(etag.Data), Encoding.UTF8)));
            }
            else
                return default(T);
        }
        internal static int IndexOfParam(this IList<Parameter> parameters, string name)
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
     
      
    }
}
