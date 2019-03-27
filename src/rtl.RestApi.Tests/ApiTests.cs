using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using System;
using System.Net.Http;
using System.Threading.Tasks;
using TvMazeScraper.Models;
using Xunit;

namespace rtl.RestApi.Tests
{
    //End to End tests
    public class ApiTests
    {
      
        private TestServer server;
        public ApiTests()
        {
            var builder = new WebHostBuilder();
            builder.UseStartup<Startup>();
            server = new TestServer(builder);
        }
        [Theory]
        [InlineData(0, 50)]
        public async Task ShowsControllerMustSucceed(int page, int pageSize)
        {
            var requestString = $"Shows/{page}/{pageSize}";
            var request = server.CreateRequest(requestString);
            var result = await request.GetAsync();
            Assert.True(result.IsSuccessStatusCode, requestString + " failed");
            var response = (await result.Content.ReadAsAsync<TvsShowsResponse>());
            Assert.Equal(response?.Shows.Count ?? 0,pageSize);
            // every parameter must be inspected
            Assert.Collection(response.Shows, f => { Assert.True(f.Id > 0, "It seems a TvShow has invalid data"); });
        }
    }
}
