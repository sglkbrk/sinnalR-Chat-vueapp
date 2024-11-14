using Microsoft.AspNetCore.Mvc;
using ProductManagement.Domain.Models;
using Microsoft.AspNetCore.Authorization;
using ProductManagement.Application.Services;


namespace ProductManagement.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class FbTokenController : ControllerBase

    {
        private readonly FbTokenService _fbTokenService;

        public FbTokenController(FbTokenService fbTokenService)
        {
            _fbTokenService = fbTokenService;
        }

        [HttpGet]
        public IActionResult GetAllToken()
        {
            var fbTokens = _fbTokenService.GetAllToken();
            return Ok(fbTokens);
        }

        [HttpGet("{userId}")]
        public IActionResult GetTokenByUserId(int userId)
        {
            var fbToken = _fbTokenService.GetTokenByUserId(userId);
            if (fbToken == null)
                return NotFound();
            return Ok(fbToken);
        }

        [HttpPost]
        public IActionResult AddToken([FromBody] FbToken token)
        {
            _fbTokenService.AddToken(token);
            return Ok(token);
        }

        [HttpPut("{id}")]
        public IActionResult UpdateToken(int id, [FromBody] FbToken token)
        {
            if (id != token.Id)
                return BadRequest();

            _fbTokenService.UpdateToken(token);
            return Ok(token);
        }

        [HttpDelete("{userId}")]
        public IActionResult DeleteToken(int userId)
        {
            _fbTokenService.DeleteToken(userId);
            return NoContent();
        }


    }
}