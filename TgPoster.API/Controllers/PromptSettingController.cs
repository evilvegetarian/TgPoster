using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace TgPoster.API.Controllers;

[Authorize]
[ApiController]
public class PromptSettingController(ISender sender) : ControllerBase
{
	
}