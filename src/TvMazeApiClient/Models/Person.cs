using System;

namespace TvMazeApi.Models
{
    public class Person
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public DateTime Birthday { get; set; }

        // there are many other properties defined on person by the tvmaze api
        // but we ignore those for this assignment
    }
}