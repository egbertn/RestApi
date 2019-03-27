using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using TvMazeScraper.Entities.Converter;

namespace TvMazeScraper.Entities
{
    public class Actor
    {        
        public int Id { get; set; }
        public string Name { get; set; }
        [JsonConverter(typeof(YMDConverter))]
        public DateTime BirthDate { get; set; }

        [JsonIgnore]
        public IList<TvShowActor> TvShowActors { get; set; }
    }
}