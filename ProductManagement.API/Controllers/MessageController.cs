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
            _messageService.AddMessage(message);
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
        [Route("GetMyMessages/{senderId}/{receiverId}")]
        public async Task<IActionResult> GetMyMessages(int senderId, int receiverId)
        {
            var result = _messageService.GetMyMessages(senderId, receiverId);
            return Ok(result);
        }


    }
}