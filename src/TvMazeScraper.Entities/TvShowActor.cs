using Newtonsoft.Json;

namespace TvMazeScraper.Entities
{
    public class TvShowActor
    {
        public int TvShowActorId { get; set; }
        public int TvShowId { get; set; }
        public int ActorId { get; set; }

        [JsonIgnore]
        public virtual TvShow TvShow { get; set; }

        [JsonIgnore]
        public virtual Actor Actor { get; set; }
    }
}