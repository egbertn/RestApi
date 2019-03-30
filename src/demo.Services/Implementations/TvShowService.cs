using demo.Services.Interfaces;
using System;
using System.Linq;
using System.Threading.Tasks;
using TvMazeApi;
using TvMazeScraper.Entities;
using TvMazeScraper.Models;
using System.Diagnostics;

namespace demo.Services.Implementations
{
    public class TvShowService : ITvShowService
    {
        private readonly TvMazeClient _tvMazeClient;
        private const int PAGE_SIZE = 50;
        // other option was to use InMemorySql but this is much more easy
        public TvShowService(TvMazeClient tvMazeClient)
        {
            _tvMazeClient = tvMazeClient;
        }
      
        public async Task<TvsShowsResponse> GetShowsWithActorAsync(int pageNumber, int pageSize = PAGE_SIZE)
        {
            if (pageNumber < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(pageNumber), "Must be greater or equal to 0");
            }
            if (pageSize > TvMazeClient.PAGE_SIZE)
            {
                throw new ArgumentOutOfRangeException(nameof(pageSize), $"Must not exceed {(double)TvMazeClient.PAGE_SIZE}");
            }
            if (pageSize % PAGE_SIZE != 0)
            {
                throw new ArgumentOutOfRangeException(nameof(pageSize), $"must be dividable by {PAGE_SIZE}");
            }


            var virtualPageSize = (pageNumber * pageSize) / (double)TvMazeClient.PAGE_SIZE;
            var newTvShows = await _tvMazeClient.GetShowsAsync((int)Math.Floor(virtualPageSize));


            var virtualSkip = ((pageNumber) * pageSize) % TvMazeClient.PAGE_SIZE;
            var response = new TvsShowsResponse(newTvShows.Skip(virtualSkip)
                // could be done using AutoMapper
                .Select(s => new TvShow()
                {
                    TvShowId = s.Id,
                    Name = s.Name
                }
                ).Take(pageSize));
            foreach (var tvShow in response.Shows.AsParallel())
            {
                Trace.TraceInformation($"Page: {pageNumber} show:{tvShow.Id} get cast");
                var cast = await _tvMazeClient.GetCastAsync(tvShow.Id);

                tvShow.Cast = cast.Select(s => new Actor()
                {
                    Name = s.Person.Name,
                    Id = s.Person.Id,
                    BirthDate = s.Person.Birthday

                }).Distinct().OrderByDescending(d => d.BirthDate).ToArray();
            }

            return response;
        }
    }
}