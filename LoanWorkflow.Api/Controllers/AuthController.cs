using LoanWorkflow.Api.Features.Auth;
using LoanWorkflow.Api.Services.ApplicationServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LoanWorkflow.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class AuthController : ControllerBase
{
    private readonly IAuthApplicationService _service;
    public AuthController(IAuthApplicationService service){ _service = service; }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginRequestDto dto)
    {
        var traceId = HttpContext.TraceIdentifier;
        var response = await _service.LoginAsync(dto, traceId, HttpContext.RequestAborted);
        if(response.ResponseCode == Features.Constants.ResponseCodes.SUCCESS)
            return Ok(response);
        if(response.ResponseCode == Features.Constants.ResponseCodes.VALIDATION_ERROR)
            return BadRequest(response);
        return Unauthorized(response);
    }
}
