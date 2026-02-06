using Identity.Application.Features.GetProfile;
using Identity.Application.Features.RegisterUser;
using Identity.Application.Features.UpdateProfile;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Identity.Api.Controllers;

/// <summary>
/// Identity and user profile management endpoints
/// </summary>
[ApiController]
[Route("api/identity")]
public class IdentityController : ControllerBase
{
    private readonly RegisterUserHandler _registerUserHandler;
    private readonly GetProfileHandler _getProfileHandler;
    private readonly UpdateProfileHandler _updateProfileHandler;

    public IdentityController(
        RegisterUserHandler registerUserHandler,
        GetProfileHandler getProfileHandler,
        UpdateProfileHandler updateProfileHandler)
    {
        _registerUserHandler = registerUserHandler;
        _getProfileHandler = getProfileHandler;
        _updateProfileHandler = updateProfileHandler;
    }

    /// <summary>
    /// Register a new user in the system
    /// </summary>
    /// <param name="request">User registration details including email and name</param>
    /// <returns>The newly created user's ID</returns>
    /// <response code="200">User successfully registered</response>
    /// <response code="400">Invalid registration data or user already exists</response>
    /// <response code="401">Missing or invalid authentication token</response>
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

    /// <summary>
    /// Get the authenticated user's profile information
    /// </summary>
    /// <returns>User profile details</returns>
    /// <response code="200">Profile retrieved successfully</response>
    /// <response code="401">Missing or invalid authentication token</response>
    /// <response code="404">User profile not found</response>
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
    /// <summary>
    /// Update the authenticated user's profile
    /// </summary>
    /// <param name="request">Updated profile details</param>
    /// <returns>No content on success</returns>
    /// <response code="200">Profile updated successfully</response>
    /// <response code="400">Invalid profile data</response>
    /// <response code="401">Missing or invalid authentication token</response>
    /// <response code="404">User profile not found</response>
    [HttpPut("me")]
    [Authorize]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest request)
    {
        var auth0Id = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(auth0Id)) return Unauthorized();

        var command = new UpdateProfileCommand(auth0Id, request.FirstName, request.LastName);
        var result = await _updateProfileHandler.Handle(command);

        if (result.IsFailure)
        {
            return result.Error.Code == "User.NotFound"
                ? NotFound(result.Error)
                : BadRequest(result.Error);
        }

        return Ok();
    }
}

/// <summary>
/// User registration request
/// </summary>
/// <param name="Email">User's email address</param>
/// <param name="FirstName">User's first name</param>
/// <param name="LastName">User's last name</param>
public record RegisterRequest(string Email, string FirstName, string LastName);

/// <summary>
/// Update profile request
/// </summary>
/// <param name="FirstName">User's first name</param>
/// <param name="LastName">User's last name</param>
public record UpdateProfileRequest(string FirstName, string LastName);
