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
            _tvMazeClient = new TvMazeClient();
        }
        [Fact]
        public async Task ShowCastRequestSucceeds()
        {
            var castResponse = await _tvMazeClient.GetCastAsync(1);
            Assert.True(castResponse.First().Person.Name == "Rachelle Lefevre");
        }


    }
}
