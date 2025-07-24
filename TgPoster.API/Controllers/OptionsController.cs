using Microsoft.AspNetCore.Mvc;
using TgPoster.API.Models;

namespace TgPoster.API.Controllers;

[ApiController]
[Route("api/options")] // Общий роут для всех опций
public class OptionsController : ControllerBase
{
    /// <summary>
    /// Модель для представления значения Enum на фронтенде.
    /// </summary>
    public class EnumViewModel
    {
        /// <summary>
        /// Числовое значение элемента Enum.
        /// </summary>
        public int Value { get; set; }

        /// <summary>
        /// Имя элемента Enum (для отображения пользователю).
        /// </summary>
        public string Name { get; set; }
    }
    
    [HttpGet("message-statuses")]
    public IActionResult GetMessageStatuses()
    {
        var statuses = Enum.GetValues<MessageStatus>()
            .Select(status => new EnumViewModel
            {
                Value = (int)status,
                Name = status.GetName() 
            })
            .ToList();

        return Ok(statuses);
    }

    [HttpGet("message-sort-fields")]
    public IActionResult GetMessageSortFields()
    {
        var sortFields = Enum.GetValues<MessageSortBy>()
            .Select(field => new EnumViewModel
            {
                Value = (int)field,
                Name = field.GetName()
            })
            .ToList();

        return Ok(sortFields);
    }

    [HttpGet("sort-directions")]
    public IActionResult GetSortDirections()
    {
        var directions = Enum.GetValues<SortDirection>()
            .Select(dir => new EnumViewModel
            {
                Value = (int)dir,
                Name = dir.GetName()
            })
            .ToList();

        return Ok(directions);
    }
}