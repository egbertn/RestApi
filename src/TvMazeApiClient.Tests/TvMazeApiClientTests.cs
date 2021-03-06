using ADC.RestApiTools;
using System;
using System.Linq;
using System.Threading.Tasks;
using TvMazeApi;
using Xunit;

namespace TvMazeApiClient.Tests
{
    public class TvMazeApiClientTests
    {
        //coukd be done using IoC but 3 hours dude

        private readonly TvMazeClient _tvMazeClient;
        public TvMazeApiClientTests()
        {
            var client = new MemoryCachedRestSharp();
            _tvMazeClient = new TvMazeClient(client);
        }
        [Fact]
        public async Task ShowCastRequestSucceeds()
        {
            var castResponse = await _tvMazeClient.GetCastAsync(1);
            Assert.True(castResponse.First().Person.Name == "Rachelle Lefevre");
        }

        [Fact]
        public async Task ShowRequestSucceeds()
        {
            var castResponse = await _tvMazeClient.GetShowsAsync(2);
            Assert.True(castResponse.Any());
        }


    }
}
