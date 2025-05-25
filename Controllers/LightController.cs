using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartHome.Services;

namespace SmartHome.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class LightController : ControllerBase
    {
        private readonly ILightService _lightService;
        private readonly ILogger<LightController> _logger;

        public LightController(ILightService lightService, ILogger<LightController> logger)
        {
            _lightService = lightService;
            _logger = logger;
        }

        [HttpPost("toggle")]
        public IActionResult Toggle([FromBody] bool turnOn)
        {
            try
            {
                if (turnOn)
                {
                    _lightService.TurnOn();
                    _logger.LogInformation("Light turned ON by {User}", User.Identity.Name);
                }
                else
                {
                    _lightService.TurnOff();
                    _logger.LogInformation("Light turned OFF by {User}", User.Identity.Name);
                }

                return Ok(new
                {
                    success = true,
                    isOn = _lightService.IsLightOn,
                    user = User.Identity.Name,
                    timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling light state");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpGet("status")]
        public IActionResult Status()
        {
            try
            {
                return Ok(new
                {
                    isOn = _lightService.IsLightOn,
                    lastUpdated = DateTime.UtcNow // Add if tracking state changes
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting light status");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }
    }
}