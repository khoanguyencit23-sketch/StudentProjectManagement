using Microsoft.AspNetCore.Mvc;
using StudentProjectManagement.Application.Common;
using StudentProjectManagement.Application.Common.Interfaces;

namespace StudentProjectManagement.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public abstract class BaseApiController : ControllerBase
{
    protected BaseApiController(ICurrentUserService currentUserService)
    {
        CurrentUser = currentUserService;
    }

    protected ICurrentUserService CurrentUser { get; }

    protected IActionResult OkResponse<T>(
        T? data,
        string message = "Request completed successfully.",
        string code = "SUCCESS")
    {
        return Ok(ApiResponse<T>.SuccessResponse(data, message, code));
    }

    protected IActionResult OkResponse()
    {
        return OkResponse<object?>(null);
    }

    protected IActionResult CreatedResponse<T>(
        T data,
        string message = "Resource created successfully.",
        string code = "CREATED")
    {
        return StatusCode(StatusCodes.Status201Created,
            ApiResponse<T>.SuccessResponse(data, message, code));
    }

    protected IActionResult BadRequestResponse(
        string message = "The request is invalid.",
        object? error = null,
        string code = "BAD_REQUEST")
    {
        return BadRequest(ApiResponse<object?>.FailureResponse(code, message, error));
    }

    protected IActionResult UnauthorizedResponse(
        string message = "Authentication is required.")
    {
        return Unauthorized(ApiResponse<object?>.FailureResponse("UNAUTHORIZED", message));
    }
}
