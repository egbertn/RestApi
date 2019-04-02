using RestSharp;

namespace ADC.RestApiTools
{
    /// <summary>
    /// default implementation uses <see cref="Microsoft.Extensions.Caching.Memory.MemoryCache"/>
    /// </summary>
    public interface IRestMemoryCache
    {
        IRestResponse RestResponseFromCache(IRestClient client, IRestRequest request, Method method = Method.GET);
        IRestResponse<T> RestResponseFromCache<T>(IRestClient client, IRestRequest request, Method method = Method.GET);
        void SetRestResponseFromCache(IRestClient client, IRestResponse response, IRestRequest request, Method method = Method.GET);
        void SetRestResponseFromCache<T>(IRestClient client, IRestResponse<T> response, IRestRequest request, Method method = Method.GET);
        void CheckAndStoreInCache(IRestClient client, IRestResponse response);
    }
}