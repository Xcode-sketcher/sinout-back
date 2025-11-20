using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using FluentValidation;
using APISinout.Models;
using APISinout.Services;
using APISinout.Helpers;
using Microsoft.AspNetCore.RateLimiting;

namespace APISinout.Controllers;

// Controlador para autenticação.
[ApiController]
[Route("api/auth")]
[EnableRateLimiting("limite-auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly IPasswordResetService _passwordResetService;
    private readonly IValidator<RegisterRequest> _registerValidator;
    private readonly IValidator<LoginRequest> _loginValidator;

    // Construtor que injeta os serviços necessários.
    public AuthController(
        IAuthService authService,
        IPasswordResetService passwordResetService,
        IValidator<RegisterRequest> registerValidator,
        IValidator<LoginRequest> loginValidator)
    {
        _authService = authService;
        _passwordResetService = passwordResetService;
        _registerValidator = registerValidator;
        _loginValidator = loginValidator;
    }

    // Método para registro de usuário.
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        var validationResult = await _registerValidator.ValidateAsync(request);
        if (!validationResult.IsValid)
            return BadRequest(validationResult.Errors);

        try
        {
            var response = await _authService.RegisterAsync(request);
            return Created(string.Empty, response);
        }
        catch (AppException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // Método para login.
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var validationResult = await _loginValidator.ValidateAsync(request);
        if (!validationResult.IsValid)
            return BadRequest(validationResult.Errors);

        try
        {
            var response = await _authService.LoginAsync(request);
            return Ok(response);
        }
        catch (AppException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
    }

    // Método para solicitar reset de senha.
    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
    {
        try
        {
            var response = await _passwordResetService.RequestPasswordResetAsync(request);
            return Ok(response);
        }
        catch (AppException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // Método para reenviar código de reset.
    [HttpPost("resend-reset-code")]
    public async Task<IActionResult> ResendResetCode([FromBody] ResendResetCodeRequest request)
    {
        try
        {
            var response = await _passwordResetService.ResendResetCodeAsync(request);
            return Ok(response);
        }
        catch (AppException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // Método para resetar senha com token.
    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
    {
        try
        {
            var response = await _passwordResetService.ResetPasswordAsync(request);
            return Ok(response);
        }
        catch (AppException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // Método para alterar senha (usuário autenticado).
    [HttpPost("change-password")]
    [Authorize]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        try
        {
            var userId = AuthorizationHelper.GetCurrentUserId(User);
            var response = await _passwordResetService.ChangePasswordAsync(request, userId);
            return Ok(response);
        }
        catch (AppException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // Método para obter informações do usuário logado.
    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> GetCurrentUser()
    {
        try
        {
            var userId = AuthorizationHelper.GetCurrentUserId(User);
            var user = await _authService.GetUserByIdAsync(userId);

            if (user == null)
                return NotFound(new { message = "Usuário não encontrado" });

            return Ok(new
            {
                userId = user.UserId,
                name = user.Name,
                email = user.Email,
                role = user.Role,
                patientName = user.PatientName,
                phone = user.Phone
            });
        }
        catch (AppException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
    }
}