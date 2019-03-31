using RestSharp;
using RestSharp.Deserializers;
using RestSharp.Serialization.Json;
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
    public sealed class SmarterRestClient: RestClient, IDeserializer
    {
        /// <summary>
        /// tells the client that the response came from memory-cache
        /// So, the RequestUri is not the same as the ResponseUri
        /// </summary>
        private const string MEMORY_CACHE_URI = "about:cache?device=memory";
        public SmarterRestClient()
        {
            SetHandler();
        }
        public SmarterRestClient(Uri baseUri): base(baseUri)
        {
            SetHandler();
        }
        public SmarterRestClient(string baseUri): base(baseUri)
        {
            SetHandler();
        }



        T IDeserializer.Deserialize<T>(IRestResponse response)
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
                    AbsoluteExpiration = offset
                };

                RestSharpExtensions.Cache.Value.Set(hash, value, options);
            }
            //todo handle XML
            if (contentType.MediaType.Equals( "application/json", StringComparison.InvariantCultureIgnoreCase))
            {
                return new JsonSerializer().Deserialize<T>(response);
            }
            return new XmlDeserializer().Deserialize<T>(response);
        }

        private void SetHandler()
        {
            this.AddHandler("application/xml", () => this);
            this.AddHandler("text/xml", () => this);
            this.AddHandler("application/json", () => this);
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
        public override IRestResponse Execute(IRestRequest request)
        {
            return base.Execute(request, request.Method);
        }
        //no server round trip, valid only if we are allowed to read from cache
        private IRestResponse IRestResponseFromCache(IRestRequest request, Method method= Method.GET)
        {
            if (method != Method.GET || request.Method != Method.GET)  return null;
            CheckCacheByUri(request, out CacheEntry etag);

            // the data is there, we allowed cache so that takes precedence
            if (etag.HasExpires == true)
            {
                // this part is tricky, since we emulate the RestRequest serialize
                var contentEncoding = etag.ContentEncoding != null ? Encoding.UTF8.GetString(etag.ContentEncoding) : "utf-8";
                return new RestResponse()
                {
                    RawBytes = etag.Data,
                    ContentEncoding = contentEncoding,
                    Content = Encoding.GetEncoding(contentEncoding).GetString(etag.Data),
                    ContentLength = etag.Data.LongLength,
                    StatusCode = HttpStatusCode.NotModified,
                    ResponseStatus = ResponseStatus.None,
                    Request = request,
                    ContentType = etag.ContentType != null ? Encoding.UTF8.GetString(etag.ContentType) : null,
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
                var retVal = new RestResponse<T>()
                {
                    RawBytes = etag.Data,
                    ContentEncoding = contentEncoding,
                    Content = Encoding.GetEncoding(contentEncoding).GetString(etag.Data),                   
                    ContentLength = etag.Data.LongLength,
                    StatusCode = HttpStatusCode.NotModified,
                    ResponseStatus = ResponseStatus.None,
                    Request = request,
                    ContentType = etag.ContentType != null ? Encoding.UTF8.GetString(etag.ContentType) : null,
                    ResponseUri = new Uri(MEMORY_CACHE_URI)
                };
                var contentType = new ContentType(retVal.ContentType);
                if (contentType.MediaType.Equals("application/json", StringComparison.InvariantCultureIgnoreCase))
                {
                    var ser = new JsonSerializer();

                    retVal.Data = ser.Deserialize<T>(retVal);
                }
                else
                {
                    var xml = new XmlDeserializer();
                    if (!string.IsNullOrEmpty(request.DateFormat))
                        xml.DateFormat = request.DateFormat;

                    if (!string.IsNullOrEmpty(request.XmlNamespace))
                        xml.Namespace = request.XmlNamespace;
                    retVal.Data = xml.Deserialize<T>(retVal);
                }
                return retVal;
            }
            return null;
        }
        
        private void SetRestResponseFromCache(IRestResponse response, IRestRequest request, Method method = Method.GET)
        {
            if (method != Method.GET || request.Method != Method.GET) return ;
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
            response.Content = Encoding.GetEncoding(contentEncoding).GetString(etag.Data);
            response.ContentLength = etag.Data.LongLength;
            response.ContentType = etag.ContentType != null ? Encoding.UTF8.GetString(etag.ContentType) : null;
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
            response.Content = Encoding.GetEncoding(contentEncoding).GetString(etag.Data);
            response.ContentLength = etag.Data.LongLength;
            response.ContentType = etag.ContentType != null ? Encoding.UTF8.GetString(etag.ContentType) : null;
            response.ResponseUri = new Uri(MEMORY_CACHE_URI);
            var contentType = new ContentType(response.ContentType);
            if (contentType.MediaType.Equals("application/json", StringComparison.InvariantCultureIgnoreCase))
            {
                var ser = new JsonSerializer();

                response.Data = ser.Deserialize<T>(response);
            }
            else
            {   var xml  = new XmlDeserializer();
                if (!string.IsNullOrEmpty(request.DateFormat))
                    xml.DateFormat = request.DateFormat;

                if (!string.IsNullOrEmpty(request.XmlNamespace))
                    xml.Namespace = request.XmlNamespace;
                response.Data = xml.Deserialize<T>(response);
            }
        }
        public override IRestResponse Execute(IRestRequest request, Method httpMethod)
        {
            var resp = IRestResponseFromCache(request, httpMethod);
            
            var httpResponse = resp ?? base.Execute(request, httpMethod);
            // the server told us the resource has not been modified
            // so get it from our cache
            if (httpResponse.ServerSaysReadFromCache())
            {
                SetRestResponseFromCache(httpResponse, request, httpMethod);
            }
            return httpResponse;
        }
        public override IRestResponse<T> Execute<T>(IRestRequest request)
        {
            var resp = IRestResponseFromCache<T>(request, request.Method);

            var httpResponse = resp ?? base.Execute<T>(request);

            if (httpResponse.ServerSaysReadFromCache())
            {
                SetRestResponseFromCache(httpResponse, request, request.Method);
            }
            return httpResponse;
        }
        public override IRestResponse<T> Execute<T>(IRestRequest request, Method httpMethod)
        {
            var resp = IRestResponseFromCache<T>(request, httpMethod);

            var httpResponse = resp ?? base.Execute<T>(request, httpMethod);

            if (httpResponse.ServerSaysReadFromCache())
            {
                SetRestResponseFromCache(httpResponse, request, httpMethod);
            }
            return httpResponse;
        }

        
        public override Task<IRestResponse> ExecuteGetTaskAsync(IRestRequest request)
        {
            var resp =  IRestResponseFromCache(request, Method.GET);

            var httpResponse = resp != null ? Task.FromResult(resp) :  base.ExecuteGetTaskAsync(request);
            if (httpResponse.Result.ServerSaysReadFromCache())
            {
                SetRestResponseFromCache(httpResponse.Result, request, Method.GET);
            }
            return httpResponse;
        }
        public override Task<IRestResponse> ExecuteGetTaskAsync(IRestRequest request, CancellationToken token)
        {
            var resp = IRestResponseFromCache(request, request.Method);

            var httpResponse = resp != null ? Task.FromResult(resp) : base.ExecuteGetTaskAsync(request, token);
            if (httpResponse.Result.ServerSaysReadFromCache())
            {
                SetRestResponseFromCache(httpResponse.Result, request, request.Method);
            }
            return httpResponse;
            //return base.ExecuteGetTaskAsync(request, token);
        }
        public override Task<IRestResponse<T>> ExecuteGetTaskAsync<T>(IRestRequest request)
        {
            var resp = IRestResponseFromCache<T>(request, Method.GET);

            var httpResponse = resp != null ? Task.FromResult(resp) : base.ExecuteGetTaskAsync<T>(request);
            if (httpResponse.Result.ServerSaysReadFromCache())
            {
                SetRestResponseFromCache(httpResponse.Result, request, Method.GET);
            }
            return httpResponse;
        }

        public override Task<IRestResponse<T>> ExecuteGetTaskAsync<T>(IRestRequest request, CancellationToken token)
        {
            var resp = IRestResponseFromCache<T>(request, Method.GET);

            var httpResponse = resp != null ? Task.FromResult(resp) : base.ExecuteGetTaskAsync<T>(request, token);
            if (httpResponse.Result.ServerSaysReadFromCache())
            {
                SetRestResponseFromCache(httpResponse.Result, request, Method.GET);
            }
            return httpResponse;
        }
        public override Task<IRestResponse> ExecuteTaskAsync(IRestRequest request)
        {
            var resp = IRestResponseFromCache(request, request.Method);

            var httpResponse = resp != null ? Task.FromResult(resp) : base.ExecuteTaskAsync(request);
            if (httpResponse.Result.ServerSaysReadFromCache())
            {
                SetRestResponseFromCache(httpResponse.Result, request, request.Method);
            }
            return httpResponse;
        }
        public override Task<IRestResponse> ExecuteTaskAsync(IRestRequest request, CancellationToken token)
        {
            var resp = IRestResponseFromCache(request, request.Method);

            var httpResponse = resp != null ? Task.FromResult(resp) : base.ExecuteTaskAsync(request, token);
            if (httpResponse.Result.ServerSaysReadFromCache())
            {
                SetRestResponseFromCache(httpResponse.Result, request, request.Method);
            }
            return httpResponse;
        }
        public override Task<IRestResponse> ExecuteTaskAsync(IRestRequest request, CancellationToken token, Method httpMethod)
        {
            var resp = IRestResponseFromCache(request, httpMethod);

            var httpResponse = resp != null ? Task.FromResult(resp) : base.ExecuteTaskAsync(request, token, httpMethod);
            if (httpResponse.Result.ServerSaysReadFromCache())
            {
                SetRestResponseFromCache(httpResponse.Result, request, httpMethod);
            }
            return httpResponse;
        }

        public override Task<IRestResponse<T>> ExecuteTaskAsync<T>(IRestRequest request)
        {
            var resp = IRestResponseFromCache<T>(request, request.Method);

            var httpResponse = resp != null ? Task.FromResult(resp) : base.ExecuteTaskAsync<T>(request);
            if (httpResponse.Result.ServerSaysReadFromCache())
            {
                SetRestResponseFromCache(httpResponse.Result, request, request.Method);
            }
            return httpResponse;
        }

        public override Task<IRestResponse<T>> ExecuteTaskAsync<T>(IRestRequest request, CancellationToken token)
        {
            var resp = IRestResponseFromCache<T>(request, request.Method);

            var httpResponse = resp != null ? Task.FromResult(resp) : base.ExecuteTaskAsync<T>(request, token);
            if (httpResponse.Result.ServerSaysReadFromCache())
            {
                SetRestResponseFromCache(httpResponse.Result, request, request.Method);
            }
            return httpResponse;
        }
        public override Task<IRestResponse<T>> ExecuteTaskAsync<T>(IRestRequest request, CancellationToken token, Method httpMethod)
        {
            var resp = IRestResponseFromCache<T>(request, httpMethod);

            var httpResponse = resp != null ? Task.FromResult(resp) : base.ExecuteTaskAsync<T>(request, token, httpMethod);
            if (httpResponse.Result.ServerSaysReadFromCache())
            {
                SetRestResponseFromCache(httpResponse.Result, request, httpMethod);
            }
            return httpResponse;
        }
        public override Task<IRestResponse<T>> ExecuteTaskAsync<T>(IRestRequest request, Method httpMethod)
        {
            var resp = IRestResponseFromCache<T>(request, httpMethod);

            var httpResponse = resp != null ? Task.FromResult(resp) : base.ExecuteTaskAsync<T>(request, httpMethod);
            if (httpResponse.Result.ServerSaysReadFromCache())
            {
                SetRestResponseFromCache(httpResponse.Result, request, httpMethod);
            }
            return httpResponse;
        }
    }
}