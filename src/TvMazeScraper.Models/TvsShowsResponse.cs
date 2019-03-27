using System.Collections.Generic;
using System.Linq;
using TvMazeScraper.Entities;

namespace TvMazeScraper.Models
{
    public class TvsShowsResponse
    {
        public IReadOnlyList<TvShowWithCast> Shows { get; }

        public TvsShowsResponse(IEnumerable<TvShow> shows)
        {
            Shows = shows.Select(s => new TvShowWithCast(s)).ToArray();
        }
    }
}