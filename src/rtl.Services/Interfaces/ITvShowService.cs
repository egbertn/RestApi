using System.Threading.Tasks;
using TvMazeScraper.Models;

namespace rtl.Services.Interfaces
{
    /// <summary>
    /// defines Service Interface for TV Show
    /// </summary>
    public interface ITvShowService
    {
        Task<TvsShowsResponse> GetShowsWithActorAsync(int pageNumber, int pageSize);
    }
}