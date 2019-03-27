using System.Collections.Generic;
using System.Linq;
using TvMazeScraper.Entities;

namespace TvMazeScraper.Models
{
    public class TvShowWithCast
    {
        public int Id { get; set; }
        public string Name { get; set; }

        public IEnumerable<Actor> Cast { get; set; }

        public TvShowWithCast(TvShow show)
        {
            Id = show.TvShowId;
            Name = show.Name;
            Cast = show.TvShowActors?.Select(e => e.Actor).ToArray();
        }
    }
}