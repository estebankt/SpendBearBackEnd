using Identity.Application.Features.GetProfile;
using Identity.Application.Features.RegisterUser;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Identity.Api.Controllers;

[ApiController]
[Route("api/identity")]
public class IdentityController : ControllerBase
{
    private readonly RegisterUserHandler _registerUserHandler;
    private readonly GetProfileHandler _getProfileHandler;

    public IdentityController(RegisterUserHandler registerUserHandler, GetProfileHandler getProfileHandler)
    {
        _registerUserHandler = registerUserHandler;
        _getProfileHandler = getProfileHandler;
    }

    [HttpPost("register")]
    [Authorize]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        var auth0Id = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(auth0Id)) return Unauthorized();

        var command = new RegisterUserCommand(auth0Id, request.Email, request.FirstName, request.LastName);
        
        var result = await _registerUserHandler.Handle(command);
        
        if (result.IsFailure)
        {
            return BadRequest(result.Error);
        }

        return Ok(new { UserId = result.Value });
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> GetProfile()
    {
        var auth0Id = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(auth0Id)) return Unauthorized();

        var query = new GetProfileQuery(Auth0UserId: auth0Id);
        var result = await _getProfileHandler.Handle(query);

        if (result.IsFailure)
        {
            return NotFound(result.Error);
        }

        return Ok(result.Value);
    }
}

public record RegisterRequest(string Email, string FirstName, string LastName);
