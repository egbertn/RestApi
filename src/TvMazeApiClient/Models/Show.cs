namespace TvMazeApi.Models
{
    public class Show
    {
        public int Id { get; set; }
        public string Name { get; set; }

        public static implicit operator Show( int  id)
        {
            return new Show() { Id = id };
        }
        // there are many other properties defined on show by the tvmaze api
        // but we ignore those for this assignment
    }
}