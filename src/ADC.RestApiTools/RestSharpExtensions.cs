using Microsoft.Extensions.Caching.Memory;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Net;

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
        /// <summary>
        /// indicates a NotModified (304) HTTP status but this is not from memorycache but an authentic server
        /// response
        /// </summary>
        internal
        static bool ServerSaysReadFromCache(this IRestResponse response)
        {
            if (response is null)
            {
                return false;
            }
            return response.StatusCode == HttpStatusCode.NotModified && response.ResponseUri?.Scheme != "about";
        }
    }
}
