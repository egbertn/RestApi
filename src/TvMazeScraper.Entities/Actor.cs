using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using TvMazeScraper.Entities.Converter;

namespace TvMazeScraper.Entities
{
    public class Actor
    {
        public override bool Equals(object obj)
        {
            return (obj is Actor act && act.Id == Id);
        }
        public override int GetHashCode()
        {
            return Id;
        }
        public int Id { get; set; }
        public string Name { get; set; }
        [JsonConverter(typeof(YMDConverter))]
        public DateTime BirthDate { get; set; }

        [JsonIgnore]
        public IList<TvShowActor> TvShowActors { get; set; }
    }
}