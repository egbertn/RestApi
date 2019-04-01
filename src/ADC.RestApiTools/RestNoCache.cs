using RestSharp;

namespace ADC.RestApiTools
{
    /// <summary>
    /// bogus implementation
    /// </summary>
    public sealed class RestNoCache : IRestMemoryCache
    {
        public void CheckAndStoreInCache(IRestClient client, IRestResponse response)
        {
            
        }

        public void CheckCacheByUri(IRestClient client, IRestRequest request, out CacheEntry entry)
        {
            entry = new CacheEntry();
        }

        public IRestResponse RestResponseFromCache(IRestClient client, IRestRequest request, Method method = Method.GET)
        {
            return null;
        }

        public IRestResponse<T> RestResponseFromCache<T>(IRestClient client, IRestRequest request, Method method = Method.GET)
        {
            return null;
        }

        public void SetRestResponseFromCache(IRestClient client, IRestResponse response, IRestRequest request, Method method = Method.GET)
        {
             
        }

        public void SetRestResponseFromCache<T>(IRestClient client, IRestResponse<T> response, IRestRequest request, Method method = Method.GET)
        {
          
        }
    }
}
