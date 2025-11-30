using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Spending.Application.Features.Categories.CreateCategory;
using Spending.Application.Features.Categories.GetCategories;
using System.Security.Claims;

namespace Spending.Api.Controllers;

[ApiController]
[Route("api/spending/categories")]
[Authorize]
public class CategoriesController : ControllerBase
{
    private readonly CreateCategoryHandler _createCategoryHandler;
    private readonly GetCategoriesHandler _getCategoriesHandler;

    public CategoriesController(
        CreateCategoryHandler createCategoryHandler,
        GetCategoriesHandler getCategoriesHandler)
    {
        _createCategoryHandler = createCategoryHandler;
        _getCategoriesHandler = getCategoriesHandler;
    }

    [HttpPost]
    public async Task<IActionResult> CreateCategory([FromBody] CreateCategoryRequest request)
    {
        var userIdClaim = User.FindFirst("user_id")?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            return Unauthorized(new { Error = "User ID not found in token" });

        var command = new CreateCategoryCommand(request.Name, request.Description);

        // Validate
        var validationResult = CreateCategoryValidator.Validate(command);
        if (validationResult.IsFailure)
            return BadRequest(validationResult.Error);

        var result = await _createCategoryHandler.Handle(command, userId);

        if (result.IsFailure)
            return BadRequest(result.Error);

        return CreatedAtAction(
            nameof(CreateCategory),
            new { id = result.Value.Id },
            result.Value
        );
    }

    [HttpGet]
    public async Task<IActionResult> GetCategories()
    {
        var userIdClaim = User.FindFirst("user_id")?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            return Unauthorized(new { Error = "User ID not found in token" });

        var query = new GetCategoriesQuery();
        var result = await _getCategoriesHandler.Handle(query, userId);

        if (result.IsFailure)
            return BadRequest(result.Error);

        return Ok(result.Value);
    }
}

public record CreateCategoryRequest(string Name, string? Description);
