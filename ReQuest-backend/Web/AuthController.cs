using Microsoft.AspNetCore.Mvc;
using ReQuest_backend.Server.Auth;
using ReQuest_backend.Web.DTO;

namespace ReQuest_backend.Web;

[Route("api/auth")]
[ApiController]
public class AuthController : ControllerBase
{
    private readonly IAuthTokenService _authTokenService;

    public AuthController(IAuthTokenService authTokenService)
    {
        _authTokenService = authTokenService;
    }

    [HttpPost("login")]
    public ActionResult<AuthResponse> Login([FromBody] AuthRequest request)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);

        var token = _authTokenService.IssueToken(request.Name, request.Email);
        return Ok(new AuthResponse
        {
            Token = token,
            Name = request.Name.Trim(),
            Email = request.Email.Trim()
        });
    }
}