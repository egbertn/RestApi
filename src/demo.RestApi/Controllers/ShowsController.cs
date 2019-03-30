using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using demo.Services.Interfaces;
using System;
using System.Threading.Tasks;
using TvMazeScraper.Models;

namespace demo.RestApi.Controllers
{

    /*
     * It should provide a paginated list of all tv shows containing the id of the TV show and a list of all the cast that are playing in that TV show. 
     */
     ///<summary>
     ///lah di dah
     ///</summary>
    [Route("[controller]")]   
    [ApiExplorerSettings(GroupName = "v1")]    
    public class ShowsController : ControllerBase
    {
        private readonly ITvShowService _tvShowService;
             ///<summary>
     ///lah di dah
     ///</summary>
        public ShowsController(ITvShowService tvShowService)
        {
            _tvShowService = tvShowService;
        }

        /// <summary>
        /// Does stuff
        /// </summary>
        /// <param name="pageNumber">A 0 based value</param>
        /// <param name="pageSize">pagesize between 50 and 250</param>
        [HttpGet("{pageNumber}/{pageSize}")]
        [ProducesResponseType(typeof(TvShowWithCast), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(TvShowsErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(TvShowsErrorResponse), StatusCodes.Status500InternalServerError)]
        [Produces("application/json")]
        public async Task<ActionResult<TvsShowsResponse>> Get(int pageNumber, int pageSize = 50)
        {         

            try
            {
                var shows = await _tvShowService.GetShowsWithActorAsync(pageNumber, pageSize);
                if (shows.Shows.Count == 0)
                {
                    return NotFound();
                }
                return Ok(shows.Shows);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new TvShowsErrorResponse(ex.Message));
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError,new TvShowsErrorResponse( ex.Message ));
            }
            
        }

    }
}