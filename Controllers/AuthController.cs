// --- CONTROLADOR DE AUTENTICAÇÃO: O PORTÃO DO CASTELO ---
// Imagine que nossa API é um jogo RPG!
// O AuthController é como o "portão principal" do castelo.
// Aqui os jogadores (usuários) se registram e fazem login para entrar no jogo.
// Sem passar por aqui, ninguém entra na aventura!

using Microsoft.AspNetCore.Mvc;
using FluentValidation;
using APISinout.Models;
using APISinout.Services;
using APISinout.Helpers;

namespace APISinout.Controllers;

[ApiController] // Marca esta classe como um controlador de API
[Route("api/auth")] // O "endereço" das missões de autenticação
public class AuthController : ControllerBase
{
    // Os "aliados" que ajudam nas missões
    private readonly IAuthService _authService;
    private readonly IValidator<RegisterRequest> _registerValidator;
    private readonly IValidator<LoginRequest> _loginValidator;

    // Construtor: Como equipar o personagem com suas armas e armaduras
    public AuthController(
        IAuthService authService,
        IValidator<RegisterRequest> registerValidator,
        IValidator<LoginRequest> loginValidator)
    {
        _authService = authService;
        _registerValidator = registerValidator;
        _loginValidator = loginValidator;
    }

    // Missão: Registrar um novo jogador no jogo
    [HttpPost("register")] // POST é como "entregar uma missão"
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        // Primeiro, validar se a missão foi bem descrita
        var validationResult = await _registerValidator.ValidateAsync(request);
        if (!validationResult.IsValid)
            return BadRequest(validationResult.Errors); // "Missão rejeitada" - dados inválidos

        try
        {
            // Executar a missão: criar o novo jogador
            var response = await _authService.RegisterAsync(request);
            return Ok(response); // "Missão cumprida!"
        }
        catch (AppException ex)
        {
            return BadRequest(new { message = ex.Message }); // "Missão falhou" - erro específico
        }
    }

    // Missão: Fazer login (entrar no jogo)
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        // Validar os dados da missão
        var validationResult = await _loginValidator.ValidateAsync(request);
        if (!validationResult.IsValid)
            return BadRequest(validationResult.Errors);

        try
        {
            // Executar login
            var response = await _authService.LoginAsync(request);
            return Ok(response); // "Bem-vindo ao jogo!"
        }
        catch (AppException ex)
        {
            return Unauthorized(new { message = ex.Message }); // "Acesso negado" - credenciais erradas
        }
    }
}