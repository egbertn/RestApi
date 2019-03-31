using RestSharp;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Globalization;
using System.Text;
using Microsoft.Extensions.Caching.Memory;
using System.Net;
using System.Net.Mime;

namespace ADC.RestApiTools
{
    /// <summary>
    /// wraps RestSharp.RestClient and adds memory caching support whereever 
    /// possible using HTTP header information such as NotModified, Expires, Cache-Control and ETAG
    /// </summary>
    public sealed class SmarterRestClient: RestClient
    {
        /// <summary>
        /// tells the client that the response came from memory-cache
        /// So, the RequestUri is not the same as the ResponseUri
        /// </summary>
        private const string MEMORY_CACHE_URI = "about:cache?device=memory";
        public SmarterRestClient()
        {

        }
        public SmarterRestClient(Uri baseUri): base(baseUri)
        {
 
        }
        public SmarterRestClient(string baseUri): base(baseUri)
        {
   
        }

        private IRestResponse IRestResponseFromCache(IRestRequest request, Method method = Method.GET)
        {
            if (method != Method.GET || request.Method != Method.GET) return null;
            CheckCacheByUri(request, out CacheEntry etag);

            // the data is there, we allowed cache so that takes precedence
            if (etag.HasExpires == true)
            {
                // this part is tricky, since we emulate the RestRequest serialize
                var contentEncoding = etag.ContentEncoding != null ? Encoding.UTF8.GetString(etag.ContentEncoding) : "utf-8";
                var contentType = etag.ContentType != null ? Encoding.UTF8.GetString(etag.ContentType) : null;
                //no need to fill content it is done by a lazy loading function in the getter
                return new RestResponse()
                {
                    RawBytes = etag.Data,
                    ContentEncoding = contentEncoding,
                    ContentLength = etag.Data.LongLength,
                    StatusCode = HttpStatusCode.NotModified,
                    ResponseStatus = ResponseStatus.None,
                    Request = request,
                    ContentType = contentType,
                    ResponseUri = new Uri(MEMORY_CACHE_URI)
                };
            }
            return null;
        }
        /// <summary>
        /// no server roundtrip if from cache is allowed
        /// </summary>
        private IRestResponse<T> IRestResponseFromCache<T>(IRestRequest request, Method method = Method.GET)
        {
            if (method != Method.GET || request.Method != Method.GET) return null;
            CheckCacheByUri(request, out CacheEntry etag);

            // the data is there, we allowed cache so that takes precedence
            if (etag.HasExpires == true)
            {
                // this part is tricky, since we emulate the RestRequest serialize
                var contentEncoding = etag.ContentEncoding != null ? Encoding.UTF8.GetString(etag.ContentEncoding) : "utf-8";
                var raw = new RestResponse()
                {
                    RawBytes = etag.Data,
                    ContentEncoding = contentEncoding,
                    ContentLength = etag.Data.LongLength,
                    StatusCode = HttpStatusCode.NotModified,
                    ResponseStatus = ResponseStatus.None,
                    Request = request,
                    ContentType = etag.ContentType != null ? Encoding.UTF8.GetString(etag.ContentType) : null,
                    ResponseUri = new Uri(MEMORY_CACHE_URI)
                };
              
               return base.Deserialize<T>(raw);
      
            }
            return null;
        }

        private void SetRestResponseFromCache(IRestResponse response, IRestRequest request, Method method = Method.GET)
        {
            if (method != Method.GET || request.Method != Method.GET) return;
            CheckCacheByUri(request, out CacheEntry etag);

            // the data SHOULD be there just let them deal with it.
            if (etag.Data == null)
            {
                return;
            }
            // this part is tricky, since we emulate the RestRequest serialize
            var contentEncoding = etag.ContentEncoding != null ? Encoding.UTF8.GetString(etag.ContentEncoding) : "utf-8";
            var contentType = etag.ContentType != null ? Encoding.UTF8.GetString(etag.ContentType) : null;
            response.RawBytes = etag.Data;
            response.ContentEncoding = contentEncoding;
            response.ContentLength = etag.Data.LongLength;
            response.ContentType = contentType;
            response.ResponseUri = new Uri(MEMORY_CACHE_URI);
        }
        private void SetRestResponseFromCache<T>(IRestResponse<T> response, IRestRequest request, Method method = Method.GET)
        {
            if (method != Method.GET || request.Method != Method.GET) return;
            CheckCacheByUri(request, out CacheEntry etag);

            // the data SHOULD be there just let them deal with it.
            if (etag.Data == null)
            {
                return;
            }
            // this part is tricky, since we emulate the RestRequest serialize
            var contentEncoding = etag.ContentEncoding != null ? Encoding.UTF8.GetString(etag.ContentEncoding) : "utf-8";
            response.RawBytes = etag.Data;
            response.ContentEncoding = contentEncoding;
            response.ContentLength = etag.Data.LongLength;
            response.ContentType = etag.ContentType != null ? Encoding.UTF8.GetString(etag.ContentType) : null;
            response.ResponseUri = new Uri(MEMORY_CACHE_URI);
       
            response.Data = base.Deserialize<T>(response).Data;
            
        }

        void HookDeserialize (IRestResponse response)
        {
            var body = response.RawBytes;
            var uri = new Uri(BaseUrl, BuildUri(response.Request));
            var hash = uri.ToString().GetHashCode();

            var headers = response.Headers;

            var etagIdx = headers.IndexOfParam("ETAG");
            var expiresIdx = headers.IndexOfParam("Expires");

            // todo also get e.g. Expires: Mon, 11 Jan 2010 13:29:35 GMT
            // or Cache-Control: max-age=600
            var lastModifiedIdex = headers.IndexOfParam("Last-Modified");
            var cacheControl = headers.IndexOfParam("Cache-Control"); //max-age=<seconds>
            var contentType = new ContentType(response.ContentType);
            if (etagIdx >= 0 || lastModifiedIdex >= 0 || cacheControl >= 0 || expiresIdx >= 0)
            {
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
                        if (parts[0].IndexOf(',') >= 0) //may be private or public
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
                    Data = body,
                    ContentType = Encoding.UTF8.GetBytes(contentType.MediaType),
                    ContentEncoding = Encoding.UTF8.GetBytes(response.ContentEncoding ?? contentType.CharSet ?? "UTF-8")
                };
                var options = new MemoryCacheEntryOptions
                {
                    Size = value.Data.Length + (value.EtagValue?.Length ?? 0)
                            + (value.LastModified?.Length ?? 0)
                            + (value.ContentType?.Length ?? 0)
                            + (value.ContentEncoding?.Length ?? 0),
                    AbsoluteExpirationRelativeToNow = expiresRelative,
                    AbsoluteExpiration = offset,
                    SlidingExpiration = RestSharpExtensions.SlidingExpiration
                };

                RestSharpExtensions.Cache.Value.Set(hash, value, options);
            }
            
        }

       
        
        private void CheckCacheByUri(IRestRequest request, out CacheEntry entry)
        {
           
            var uri = new Uri(BaseUrl, BuildUri(request));
            var hash = uri.ToString().GetHashCode();
            if (RestSharpExtensions.Cache.Value.TryGetValue(hash, out  entry))
            {
                if (entry.EtagValue != null)
                {
                    request.AddHeader("If-None-Match", Encoding.UTF8.GetString(entry.EtagValue));
                }
                if (entry.LastModified != null)
                {
                    request.AddHeader("If-Last-Modified", Encoding.UTF8.GetString(entry.LastModified));
                }
                return;
            }
            entry = new CacheEntry();
        }
        // this guy must have a seperate dealing since it does NOT deserialize
        public override IRestResponse Execute(IRestRequest request)
        {
          
            var resp = IRestResponseFromCache(request, request.Method);

            var httpResponse = resp ?? base.Execute(request);

            // the server told us the resource has not been modified
            // so get it from our cache
            if (httpResponse.ServerSaysReadFromCache())
            {
                SetRestResponseFromCache(httpResponse, request, request.Method);
            }
            else if (httpResponse.IsSuccessful && request.Method == Method.GET)
            {
                HookDeserialize(httpResponse); //store it
            }
            return httpResponse;
        }
        //no server round trip, valid only if we are allowed to read from cache


        //note no deserialisiation takes place on none Generic IRestResponse
        public override async Task<IRestResponse> ExecuteTaskAsync(IRestRequest request, CancellationToken token)
        {           
            var resp = IRestResponseFromCache(request, request.Method);
            
            var httpResponse = resp ?? await base.ExecuteTaskAsync(request, token);
           
            if (httpResponse.ServerSaysReadFromCache())
            {
                SetRestResponseFromCache(httpResponse, request, request.Method);
            }
            else if (httpResponse.IsSuccessful && request.Method == Method.GET)
            {
                HookDeserialize(httpResponse); //store it
            }
            return httpResponse;
        }


        public override async Task<IRestResponse<T>> ExecuteTaskAsync<T>(IRestRequest request, CancellationToken token)
        {
            request.OnBeforeDeserialization += this.HookDeserialize;
            var resp = IRestResponseFromCache<T>(request, request.Method);        
            var httpResponse = resp ?? await base.ExecuteTaskAsync<T>(request, token);
            request.OnBeforeDeserialization -= this.HookDeserialize;
            if (httpResponse.ServerSaysReadFromCache())
            {
                SetRestResponseFromCache(httpResponse, request, request.Method);
            }
            return httpResponse;
        }

    }
}