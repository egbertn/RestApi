using RestSharp;

namespace ADC.RestApiTools
{
    public sealed class MemoryCachedRestSharp : ISmartRestSharp
    {
        public IRestClient Instance(string baseUri)
        {
            var client = new SmarterRestClient(baseUri);
            client.AddCacheHandler(new RestMemoryCache());
            return client;
        }
    }
}
