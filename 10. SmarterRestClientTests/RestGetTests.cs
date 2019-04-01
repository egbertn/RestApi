using ADC.RestApiTools;
using RestSharp;
using System;
using System.Net;
using System.Threading.Tasks;
using Xunit;

namespace SmarterRestClientTests
{
    public class RestGetTests
    {

        private SmarterRestClient _restClient;
        public RestGetTests()
        {
            ///cms/api/am/imageFileData/RE1Mu3b?ver=5c31
            _restClient = new SmarterRestClient("https://img-prod-cms-rt-microsoft-com.akamaized.net");
           _restClient.AddCacheHandler(new RestMemoryCache());
        }
        [Fact]
        public async Task ImageIsCached()
        {
            var request = new RestRequest("/cms/api/am/imageFileData/RE1Mu3b?ver=5c31");
            var response = await _restClient.ExecuteTaskAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode );
            Assert.Equal("image/png", response.ContentType);
            // do it a second time
            response = await _restClient.ExecuteTaskAsync(request);
            Assert.Equal(HttpStatusCode.NotModified, response.StatusCode);
            Assert.Equal("image/png", response.ContentType);
            Assert.Equal("about", response.ResponseUri.Scheme); //from cache
        }
    }
}
