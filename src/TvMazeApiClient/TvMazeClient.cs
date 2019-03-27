using RestSharp;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using TvMazeApi.Models;

namespace TvMazeApi
{
    public class TvMazeClient
    {
        static public readonly int PAGE_SIZE = 250;

        private const int RateLimitSleepTimerSecs = 1;
        private const int HttpStatusCodeReachedRateLimit = 429;
        private const string _baseUrl = "http://api.tvmaze.com";

        /// <summary>
        /// Get list of tv shows from TvMaze. 
        /// Shows are paginated by pages where each page is 250 rows
        /// </summary>
        /// <param name="pageNumber">pagenumber</param>
        /// <returns>list of tv shows</returns>
        public async Task<IEnumerable<Show>> GetShowsAsync(int pageNumber)
        {
            if (pageNumber < 0) throw new ArgumentOutOfRangeException(nameof(pageNumber));
            var client = new RestClient(_baseUrl);
            var request = new RestRequest("shows", Method.GET);
            request.AddQueryParameter("page", pageNumber.ToString());

            var isRateLimited = false;
            do
            {
                isRateLimited = false;
                var response = await client.ExecuteTaskAsync<IEnumerable<Show>>(request);
                if (response.IsSuccessful)
                {
                    return response.Data;
                }

                if (response.StatusCode == HttpStatusCode.NotFound)
                {
                    return new Show[0];
                }
                //API calls are rate limited to allow at least 20 calls every 10 seconds per IP address
                if (response.StatusCode == (HttpStatusCode)HttpStatusCodeReachedRateLimit)
                {
                    isRateLimited = true;
                    Thread.Sleep(TimeSpan.FromSeconds(RateLimitSleepTimerSecs));
                }
            } while (isRateLimited);
            return new Show[0];
        }

        /// <summary>
        /// Get actors for a show from TvMaze
        /// </summary>
        /// <param name="tvShow">the tv show</param>
        /// <returns>list of actors for this show</returns>

        public async Task<IEnumerable<Actor>> GetCastAsync(Show tvShow)
        {
            if (tvShow == null) throw new ArgumentNullException(nameof(tvShow));
            var client = new RestClient(_baseUrl);
            var request = new RestRequest($"shows/{tvShow.Id}/cast", Method.GET);

            var isRateLimited = false;
            do
            {
                isRateLimited = false;
                var response = await client.ExecuteTaskAsync<IEnumerable<Actor>>(request);
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    return response.Data;
                }

                if (response.StatusCode == HttpStatusCode.NotFound)
                {
                    return new Actor[0];
                }

                if (response.StatusCode == (HttpStatusCode)HttpStatusCodeReachedRateLimit)
                {
                    Console.WriteLine("--rate limit--");
                    isRateLimited = true;
                    Thread.Sleep(TimeSpan.FromSeconds(RateLimitSleepTimerSecs));
                }
            } while (isRateLimited);
            return new Actor[0];
        }
    }
}