using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Coflnet.Sky.Api.Models;
using Coflnet.Sky.Api.Services;
using Coflnet.Sky.Mayor.Client.Api;
using Coflnet.Sky.Mayor.Client.Model;

namespace Coflnet.Hypixel.Controller;

/// <summary>
/// Endpoints for mayor history data
/// </summary>
[ApiController]
[Route("api/mayor")]
[ResponseCache(Duration = 120, Location = ResponseCacheLocation.Any, NoStore = false)]
public class MayorController : ControllerBase
{
    IElectionPeriodsApi mayorService;
    /// <summary>
    /// Creates a new instance of <see cref="KatController"/>
    /// </summary>
    /// <param name="mayorService"></param>
    public MayorController(IElectionPeriodsApi mayorService)
    {
        this.mayorService = mayorService;
    }

    /// <summary>
    /// Return Election results for a specific year
    /// </summary>
    /// <returns></returns>
    [Route("{year}")]
    [HttpGet]
    [ResponseCache(Duration = 120, Location = ResponseCacheLocation.Any, NoStore = false)]
    public async Task<ModelElectionPeriod> GetYear(int year)
    {
        return await mayorService.ElectionPeriodYearGetAsync(year);
    }

    /// <summary>
    /// Get multiple years
    /// </summary>
    /// <returns></returns>
    [Route("")]
    [HttpGet]
    [ResponseCache(Duration = 120, Location = ResponseCacheLocation.Any, NoStore = false, VaryByQueryKeys = new string[] { "from", "to" })]
    public async Task<IEnumerable<ModelElectionPeriod>> GetMultiple(int from, int to)
    {
        return await mayorService.ElectionPeriodRangeFromToGetAsync(from, to);
    }
}

