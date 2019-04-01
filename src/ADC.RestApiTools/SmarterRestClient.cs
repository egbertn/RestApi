using RestSharp;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace ADC.RestApiTools
{
    /// <summary>
    /// wraps RestSharp.RestClient and adds memory caching support whereever 
    /// possible using HTTP header information such as NotModified, Expires, Cache-Control and ETAG
    /// </summary>
    public sealed class SmarterRestClient: RestClient
    {

        private IRestMemoryCache _restMemoryCache = new RestNoCache();
        public SmarterRestClient()
        {

        }
        public SmarterRestClient(Uri baseUri): base(baseUri)
        {
            //this.use
        }
        public SmarterRestClient(string baseUri): base(baseUri)
        {
   
        }
        public void AddCacheHandler(IRestMemoryCache memoryCacheHandler)
        {
            this._restMemoryCache = memoryCacheHandler;
        }
        // this guy must have a seperate dealing since it does NOT deserialize
        public override IRestResponse Execute(IRestRequest request)
        {
          
            var resp = _restMemoryCache.RestResponseFromCache(this, request, request.Method);

            var httpResponse = resp ?? base.Execute(request);

            // the server told us the resource has not been modified
            // so get it from our cache
            if (httpResponse.ServerSaysReadFromCache())
            {
                _restMemoryCache.SetRestResponseFromCache(this, httpResponse, request, request.Method);
            }
            else if (httpResponse.CanBeCached() )
            {
                _restMemoryCache.CheckAndStoreInCache(this, httpResponse); //store it
            }
            return httpResponse;
        }

        public override async Task<IRestResponse> ExecuteTaskAsync(IRestRequest request, CancellationToken token)
        {           
            var resp = _restMemoryCache.RestResponseFromCache(this, request, request.Method);
            
            var httpResponse = resp ?? await base.ExecuteTaskAsync(request, token);
           
            if (httpResponse.ServerSaysReadFromCache())
            {
                _restMemoryCache.SetRestResponseFromCache(this, httpResponse, request, request.Method);
            }
            else if (httpResponse.CanBeCached())
            {
                _restMemoryCache.CheckAndStoreInCache(this, httpResponse); //store it
            }
            return httpResponse;
        }

        public override async Task<IRestResponse<T>> ExecuteTaskAsync<T>(IRestRequest request, CancellationToken token)
        {
            var resp = _restMemoryCache.RestResponseFromCache<T>(this, request, request.Method);        
            var httpResponse = resp ?? await base.ExecuteTaskAsync<T>(request, token);
            if (httpResponse.ServerSaysReadFromCache())
            {
                _restMemoryCache.SetRestResponseFromCache(this, httpResponse, request, request.Method);
            }
            else if (httpResponse.CanBeCached())
            {
                _restMemoryCache.CheckAndStoreInCache(this, httpResponse); //store it
            }
            return httpResponse;
        }
    }
}