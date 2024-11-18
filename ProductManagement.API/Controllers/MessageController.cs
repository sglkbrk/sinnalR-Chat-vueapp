using Microsoft.AspNetCore.Mvc;
using ProductManagement.Domain.Models;
using Microsoft.AspNetCore.Authorization;
using ProductManagement.Application.Services;
namespace ProductManagement.API.Controllers
{

    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class MessageController : Controller
    {
        private readonly MessageService _messageService;
        public MessageController(MessageService messageService)
        {
            _messageService = messageService;
        }
        [HttpGet]
        [Route("GetAll")]
        public async Task<IActionResult> GetAll()
        {
            var result = _messageService.GetAll();
            return Ok(result);
        }

        [HttpGet]
        [Route("GetById/{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var result = _messageService.GetMessageById(id);
            return Ok(result);
        }

        [HttpPost]
        [Route("AddMessage")]
        public async Task<IActionResult> AddMessage([FromBody] Message message)
        {
            await _messageService.AddMessageAsync(message);
            return Ok(message);
        }

        [HttpPut]
        [Route("UpdateMessage")]
        public async Task<IActionResult> UpdateMessage([FromBody] Message message)
        {
            _messageService.UpdateMessage(message);
            return Ok(message);
        }

        [HttpDelete]
        [Route("DeleteMessage/{id}")]
        public async Task<IActionResult> DeleteMessage(int id)
        {
            _messageService.DeleteMessage(id);
            return Ok();
        }

        [HttpGet]
        [Route("GetMyMessages/{receiverId}/{page}/{pageSize}")]
        public async Task<IActionResult> GetMyMessages(int receiverId, int page = 1, int pageSize = 50)
        {
            var currentUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (currentUserId == null) return BadRequest();
            var result = _messageService.GetMyMessages(int.Parse(currentUserId ?? "0"), receiverId, page, pageSize);
            return Ok(result);
        }


    }
}