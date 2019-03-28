using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using TvMazeScraper.Entities;
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
        [Theory]
        [InlineData(-1, 50)]
        public async Task ShowsControllerFailsWithBadRequest(int page, int pageSize)
        {
            var requestString = $"Shows/{page}/{pageSize}";
            var request = server.CreateRequest(requestString);
            var result = await request.GetAsync();
            Assert.True(result.StatusCode == System.Net.HttpStatusCode.BadRequest, requestString + " should return 400");
         
        }
        [Fact]
        public void ActorsCorrectJoin()
        {
            var actors = new[] 
            {
                new Actor() { Id = 1, BirthDate = new DateTime(2000, 1, 1), Name = "Peter Pan" },
                new Actor() { Id = 1, BirthDate = new DateTime(2000, 1, 1), Name = "Peter Pan" },
                new Actor() { Id = 2, BirthDate = new DateTime(1950, 1, 10), Name = "Tinklebell" }            
            }.Distinct().OrderByDescending(o => o.BirthDate).ToArray();
            Assert.True(actors.Length == 2);
            Assert.True(actors[0].BirthDate == new DateTime(2000, 1, 1));
        }
    }
}
