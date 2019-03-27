using Newtonsoft.Json;
using System.Collections.Generic;

namespace TvMazeScraper.Entities
{
    public class TvShow
    {
        public int TvShowId { get; set; }

        public string Name { get; set; }

        public long ExternalId { get; set; }

        [JsonIgnore]
        public IList<TvShowActor> TvShowActors { get; set; }
    }
}