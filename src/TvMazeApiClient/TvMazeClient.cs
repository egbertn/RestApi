
using ADC.RestApiTools;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
            var data = request.GetDataByHashFromRequest< IEnumerable<Show>>(_baseUrl); 
            if (data != null)
            {
                return data;
            }
            var isRateLimited = false;
            do
            {
                isRateLimited = false;
                
                var response = await client.ExecuteTaskAsync<IEnumerable<Show>>(request);
                
                switch (response.StatusCode)
                {
                    case HttpStatusCode.NotModified:
                        return request.GetDataByHashFromRequest<IEnumerable<Show>>(_baseUrl);
                    default:
                    case HttpStatusCode.NotFound:
                        return new Show[0];
                    case HttpStatusCode.OK:
                        request.SetDataByRequest(_baseUrl, response.Headers, response.Data);
                        return response.Data;
                    case (HttpStatusCode)HttpStatusCodeReachedRateLimit:
                        isRateLimited = true;
                 //API calls are rate limited to allow at least 20 calls every 10 seconds per IP address
                       Thread.Sleep(TimeSpan.FromSeconds(RateLimitSleepTimerSecs));
                       break;
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
            var data = request.GetDataByHashFromRequest<IEnumerable<Actor>>(_baseUrl);
            if (data != null)
            {
                return data;
            }
            var isRateLimited = false;
            do
            {
                isRateLimited = false;
                var response = await client.ExecuteTaskAsync<IEnumerable<Actor>>(request);

                switch(response.StatusCode)
                {
                    case HttpStatusCode.NotModified:
                        return request.GetDataByHashFromRequest< IEnumerable<Actor>>(_baseUrl);
                       
                    case HttpStatusCode.OK:
                        request.SetDataByRequest(_baseUrl, response.Headers, response.Data);
                        return response.Data;
                      
                    case HttpStatusCode.NotFound:
                        return new Actor[0];
                      
                    case (HttpStatusCode)HttpStatusCodeReachedRateLimit:
                        Trace.TraceInformation("--rate limit--");
                        isRateLimited = true;
                        Thread.Sleep(TimeSpan.FromSeconds(RateLimitSleepTimerSecs));
                        break;
                }
                
            } while (isRateLimited);
            return new Actor[0];
        }
    }
}