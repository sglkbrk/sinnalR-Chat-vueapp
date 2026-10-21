using Microsoft.AspNetCore.Mvc;
using ProductManagement.Domain.Models;
using Microsoft.AspNetCore.Authorization;
using ProductManagement.Application.Services;
using ProductManagement.Application.DTO;
using System.Security.Claims;

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
            if (result == null) return NotFound();
            return Ok(result);
        }

        [HttpPost]
        [Route("AddMessage")]
        public async Task<IActionResult> AddMessage([FromBody] Message message)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(currentUserId)) return Unauthorized();

            message.SenderId = int.Parse(currentUserId);
            message.Timestamp = DateTime.UtcNow;
            await _messageService.AddMessageAsync(message);
            return Ok(message);
        }

        [HttpPut]
        [Route("UpdateMessage")]
        public async Task<IActionResult> UpdateMessage([FromBody] Message message)
        {
            await _messageService.UpdateMessage(message);
            return Ok(message);
        }

        [HttpDelete]
        [Route("DeleteMessage/{id}")]
        public async Task<IActionResult> DeleteMessage(int id)
        {
            await _messageService.DeleteMessage(id);
            return Ok();
        }

        [HttpGet]
        [Route("GetMyMessages/{receiverId}/{page}/{pageSize}")]
        public async Task<IActionResult> GetMyMessages(int receiverId, int page = 1, int pageSize = 50)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (currentUserId == null) return BadRequest();
            var result = await _messageService.GetMyMessagesAsync(int.Parse(currentUserId), receiverId, page, pageSize);
            return Ok(result);
        }

        // Phase 4: Mesaj Düzenleme
        [HttpPut]
        [Route("edit/{id}")]
        public async Task<IActionResult> EditMessage(int id, [FromBody] MessageEditDto dto)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(currentUserId)) return Unauthorized();

            if (string.IsNullOrWhiteSpace(dto.Content))
                return BadRequest("Mesaj içeriği boş olamaz.");

            try
            {
                var updated = await _messageService.EditMessageAsync(id, int.Parse(currentUserId), dto.Content);
                if (updated == null) return NotFound("Mesaj bulunamadı.");
                return Ok(_messageService.MapToResponseDto(updated));
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbid(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        // Phase 4: Benden Sil
        [HttpDelete]
        [Route("me/{id}")]
        public async Task<IActionResult> DeleteForMe(int id)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(currentUserId)) return Unauthorized();

            var success = await _messageService.DeleteMessageForMeAsync(id, int.Parse(currentUserId));
            if (!success) return NotFound();
            return Ok(new { success = true });
        }

        // Phase 4: Herkesten Sil
        [HttpDelete]
        [Route("everyone/{id}")]
        public async Task<IActionResult> DeleteForEveryone(int id)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(currentUserId)) return Unauthorized();

            try
            {
                var success = await _messageService.DeleteMessageForEveryoneAsync(id, int.Parse(currentUserId));
                if (!success) return NotFound();
                return Ok(new { success = true });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbid(ex.Message);
            }
        }

        // Phase 4: Sohbet Geçmişini Temizle
        [HttpDelete]
        [Route("clear/{friendUserId}")]
        public async Task<IActionResult> ClearChat(int friendUserId)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(currentUserId)) return Unauthorized();

            await _messageService.ClearChatAsync(int.Parse(currentUserId), friendUserId);
            return Ok(new { success = true });
        }

        // Phase 4: Mesaj Tepkisi (Reaction) Ekle / Kaldır
        [HttpPost]
        [Route("reaction/{id}")]
        public async Task<IActionResult> SetReaction(int id, [FromBody] MessageReactionDto dto)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(currentUserId)) return Unauthorized();

            var reactions = await _messageService.SetReactionAsync(id, int.Parse(currentUserId), dto.Reaction);
            return Ok(new { reactions });
        }
    }
}
