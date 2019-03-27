namespace TvMazeScraper.Models
{
    public class TvShowsErrorResponse
    {
        public TvShowsErrorResponse(string message)
        {
            Message = message;
        }
        public string Message { get;  }
    }
}
