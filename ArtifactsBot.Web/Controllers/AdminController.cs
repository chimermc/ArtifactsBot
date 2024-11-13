using Microsoft.AspNetCore.Mvc;

namespace ArtifactsBot.Web.Controllers;

[ApiController]
public class AdminController : ControllerBase
{
    /// <summary>
    /// This base route gets called every 5 minutes by Azure App Service's "Always On" setting, as well as during deployment slot swaps,
    /// to ensure that the application is ready to respond to requests and to prevent it from idling out.
    /// </summary>
    [Route("")]
    [HttpGet]
    public IActionResult KeepAlive()
    {
        return Ok();
    }

    /// <summary>
    /// Check how long this instance has been running.
    /// </summary>
    [Route("admin/uptime")]
    [HttpGet]
    public IActionResult GetUptime()
    {
        return Ok(new
        {
            initializedAtUtc = Program.InitializedAt,
            uptime = (DateTime.UtcNow - Program.InitializedAt).ToString(@"dd\.hh\:mm\:ss")
        });
    }
}
