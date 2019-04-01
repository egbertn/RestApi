using Microsoft.Extensions.Caching.Memory;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Net;

namespace ADC.RestApiTools
{
    public static class RestSharpExtensions
    {
        public static void ConfigHttpCache(long maxMemorySize, TimeSpan? expirationScanFrequency = null, TimeSpan? slidingExpiration = null)
        {
            MaxMemorySize = maxMemorySize;
            ExpirationScanFrequency = expirationScanFrequency;
            SlidingExpiration = slidingExpiration;
        }
        private static long? MaxMemorySize;
        private static TimeSpan? ExpirationScanFrequency;
        internal static TimeSpan? SlidingExpiration;
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
        /// Indicates that this response is suited to be cached
        /// States OK, Created and NonAuthoritativeInformation are.
        /// </summary>
        /// <param name="response"></param>      
        internal
        static bool CanBeCached(this IRestResponse response)
        {
            // the only statuscodes that assumingly can be combined with a full cache of the data
            // partial content e.g. should not be cached 
            switch(response.StatusCode)
            {
                case HttpStatusCode.NonAuthoritativeInformation: // it's from another cache server (proxy?)
                case HttpStatusCode.OK:
                case HttpStatusCode.Created:
                    return true;
                default:
                    return false;
            }
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
