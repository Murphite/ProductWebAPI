using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Products.Api.Authentication;

namespace Products.Api.Controllers;

[ApiController]
[Route("api/v1/auth")]
public sealed class AuthController(ITokenService tokenService) : ControllerBase
{
    /// <summary>Exchanges demo credentials for a JWT access token.</summary>
    [HttpPost("token")]
    [AllowAnonymous]
    [EnableRateLimiting(RateLimitPolicies.Auth)]
    [ProducesResponseType<TokenResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public IActionResult CreateToken([FromBody] TokenRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
        {
            return ValidationProblem(new ValidationProblemDetails(new Dictionary<string, string[]>
            {
                ["credentials"] = ["Username and password are required."],
            }));
        }

        var token = tokenService.IssueToken(request.Username, request.Password);
        return token is null
            ? Problem(statusCode: StatusCodes.Status401Unauthorized, title: "Invalid username or password.")
            : Ok(token);
    }
}
