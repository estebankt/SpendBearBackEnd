using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Spending.Application.Features.Categories.CreateCategory;
using Spending.Application.Features.Categories.GetCategories;
using System.Security.Claims;
using SpendBear.SharedKernel.Extensions;

namespace Spending.Api.Controllers;

/// <summary>
/// Transaction category management
/// </summary>
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

    /// <summary>
    /// Create a new transaction category
    /// </summary>
    /// <param name="request">Category details including name and optional description</param>
    /// <returns>The newly created category</returns>
    /// <response code="201">Category created successfully</response>
    /// <response code="400">Invalid category data or category name already exists</response>
    /// <response code="401">Missing or invalid authentication token</response>
    [HttpPost]
    public async Task<IActionResult> CreateCategory([FromBody] CreateCategoryRequest request)
    {
        var userId = User.GetUserId();
        if (userId == null)
            return Unauthorized(new { Error = "User ID not found in token" });

        var command = new CreateCategoryCommand(request.Name, request.Description);

        // Validate
        var validationResult = CreateCategoryValidator.Validate(command);
        if (validationResult.IsFailure)
            return BadRequest(validationResult.Error);

        var result = await _createCategoryHandler.Handle(command, userId.Value);

        if (result.IsFailure)
            return BadRequest(result.Error);

        return CreatedAtAction(
            nameof(CreateCategory),
            new { id = result.Value.Id },
            result.Value
        );
    }

    /// <summary>
    /// Get all categories for the authenticated user
    /// </summary>
    /// <returns>List of all user's categories</returns>
    /// <response code="200">Categories retrieved successfully</response>
    /// <response code="401">Missing or invalid authentication token</response>
    [HttpGet]
    public async Task<IActionResult> GetCategories()
    {
        var userId = User.GetUserId();
        if (userId == null)
            return Unauthorized(new { Error = "User ID not found in token" });

        var query = new GetCategoriesQuery();
        var result = await _getCategoriesHandler.Handle(query, userId.Value);

        if (result.IsFailure)
            return BadRequest(result.Error);

        return Ok(result.Value);
    }
}

/// <summary>
/// Request to create a new category
/// </summary>
/// <param name="Name">Category name (must be unique per user)</param>
/// <param name="Description">Optional category description</param>
public record CreateCategoryRequest(string Name, string? Description);
