// ============================================================
// 🏰 CONTROLADOR DE AUTENTICAÇÃO - O PORTEIRO DO CASTELO
// ============================================================
// Analogia RPG: Este é o "Porteiro do Castelo"!
// Ele verifica quem pode entrar, cria crachás (tokens) para visitantes,
// e gerencia senhas perdidas. É a primeira linha de defesa do sistema.
//
// Funções principais:
// 1. Registro: Criar nova conta (como comprar um passe para o castelo)
// 2. Login: Verificar identidade e dar crachá de acesso (JWT token)
// 3. Recuperação de senha: Para quando você esquece a senha secreta
// 4. Alteração de senha: Trocar a senha atual
// 5. Informações do usuário: "Quem sou eu?" - retorna dados do visitante
// ============================================================

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using FluentValidation;
using APISinout.Models;
using APISinout.Services;
using APISinout.Helpers;
using Microsoft.AspNetCore.RateLimiting;

namespace APISinout.Controllers;

// 🎮 Decoradores (como "buffs" no personagem):
[ApiController]              // Marca: "Sou um controlador de API!"
[Route("api/auth")] 
[EnableRateLimiting("limite-auth")]         // Rota base: todas as URLs começam com "/api/auth"
public class AuthController : ControllerBase
{
    // 🎒 INVENTÁRIO DO PORTEIRO (Dependências injetadas)
    // Como itens mágicos que o porteiro carrega para fazer seu trabalho
    
    private readonly IAuthService _authService;                      // 🔐 Serviço de autenticação (gerente de identidades)
    private readonly IPasswordResetService _passwordResetService;    // 🔑 Serviço de redefinição de senha
    private readonly IValidator<RegisterRequest> _registerValidator;  // ✅ Validador de registro (inspetor de qualidade)
    private readonly IValidator<LoginRequest> _loginValidator;        // ✅ Validador de login

    // 🏗️ CONSTRUTOR: Montando o porteiro com seus equipamentos
    // Como equipar um personagem antes da missão
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

    // ============================================================
    // 📝 MISSÃO 1: REGISTRAR NOVO USUÁRIO
    // ============================================================
    // Analogia RPG: Como criar um novo personagem no jogo!
    // O jogador preenche os dados, e o sistema verifica se está tudo OK
    // antes de criar a conta e dar o primeiro crachá (token).
    //
    // Fluxo:
    // 1. Recebe dados do formulário de registro (nome, email, senha, etc)
    // 2. Valida se os dados estão corretos (email válido, senha forte, etc)
    // 3. Cria o usuário no banco de dados
    // 4. Retorna o usuário criado + token JWT para acesso imediato
    // ============================================================
    [HttpPost("register")]  // Rota: POST /api/auth/register
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        // 🔍 FASE 1: Inspeção de qualidade (como um chef provando os ingredientes)
        var validationResult = await _registerValidator.ValidateAsync(request);
        if (!validationResult.IsValid)
            return BadRequest(validationResult.Errors);  // ❌ Ingredientes ruins! Rejeita o pedido

        try
        {
            // ✨ FASE 2: Magia de criação (criar o usuário e gerar token)
            var response = await _authService.RegisterAsync(request);
            // Return 201 Created for resource creation
            return Created(string.Empty, response);
        }
        catch (AppException ex)
        {
            // ⚠️ Algo deu errado (ex: email já existe)
            return BadRequest(new { message = ex.Message });
        }
    }

    // ============================================================
    // 🔓 MISSÃO 2: LOGIN (ENTRAR NO CASTELO)
    // ============================================================
    // Analogia RPG: Como fazer login no jogo!
    // O jogador digita email e senha, o porteiro verifica se estão corretos
    // e entrega um "crachá mágico" (token JWT) que permite acessar áreas protegidas.
    //
    // Fluxo:
    // 1. Recebe email + senha
    // 2. Verifica se os dados estão válidos
    // 3. Confere se o usuário existe e a senha está correta
    // 4. Gera um token JWT (crachá temporário válido por algumas horas)
    // 5. Retorna o usuário + token
    // ============================================================
    [HttpPost("login")]  // Rota: POST /api/auth/login
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        // 🔍 FASE 1: Validação dos dados de entrada
        var validationResult = await _loginValidator.ValidateAsync(request);
        if (!validationResult.IsValid)
            return BadRequest(validationResult.Errors);

        try
        {
            // 🔐 FASE 2: Verificação de identidade e geração do crachá
            var response = await _authService.LoginAsync(request);
            return Ok(response);  // ✅ Bem-vindo! Aqui está seu crachá (token)
        }
        catch (AppException ex)
        {
            // ❌ Email/senha incorretos ou usuário desativado
            return Unauthorized(new { message = ex.Message });
        }
    }

    // ============================================================
    // 🔑 MISSÃO 3: ESQUECI MINHA SENHA (CHAVE PERDIDA)
    // ============================================================
    // Analogia RPG: Como pedir uma nova chave quando você perde a sua!
    // O sistema gera uma "chave temporária" (token de reset) e envia
    // por email. É como um ferreiro fazendo uma chave reserva.
    //
    // Fluxo:
    // 1. Usuário informa o email
    // 2. Sistema verifica se o email existe
    // 3. Gera um código/token único e temporário (válido por 1 hora)
    // 4. Envia email com link ou código para redefinir senha
    // ============================================================
    [HttpPost("forgot-password")]  // Rota: POST /api/auth/forgot-password
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
    {
        try
        {
            // 📧 Gera token e envia email
            var response = await _passwordResetService.RequestPasswordResetAsync(request);
            return Ok(response);  // ✅ Email enviado! Verifique sua caixa de entrada
        }
        catch (AppException ex)
        {
            // ⚠️ Email não encontrado ou erro ao enviar
            return BadRequest(new { message = ex.Message });
        }
    }

    // ============================================================
    // 🔁 MISSÃO 3.1: REENVIAR CÓDIGO DE REDEFINIÇÃO
    // ============================================================
    // Analogia RPG: Caso o código tenha se perdido no caminho!
    // O usuário pode solicitar um novo código se o anterior não chegou
    // ou expirou. Rate limiting impede spam.
    // ============================================================
    [HttpPost("resend-reset-code")]  // Rota: POST /api/auth/resend-reset-code
    public async Task<IActionResult> ResendResetCode([FromBody] ResendResetCodeRequest request)
    {
        try
        {
            // 📧 Gera novo código e reenvia email
            var response = await _passwordResetService.ResendResetCodeAsync(request);
            return Ok(response);  // ✅ Novo código enviado!
        }
        catch (AppException ex)
        {
            // ⚠️ Rate limit excedido ou erro ao enviar
            return BadRequest(new { message = ex.Message });
        }
    }

    // ============================================================
    // 🔐 MISSÃO 4: REDEFINIR SENHA COM TOKEN
    // ============================================================
    // Analogia RPG: Usando a chave temporária para criar uma nova senha!
    // O usuário usa o código recebido por email para provar que é ele mesmo
    // e define uma nova senha.
    //
    // Fluxo:
    // 1. Usuário informa o token (recebido por email) + nova senha
    // 2. Sistema verifica se o token é válido e não expirou
    // 3. Atualiza a senha no banco de dados
    // 4. Invalida o token (para não ser reutilizado)
    // ============================================================
    [HttpPost("reset-password")]  // Rota: POST /api/auth/reset-password
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
    {
        try
        {
            // 🔄 Verifica token e troca senha
            var response = await _passwordResetService.ResetPasswordAsync(request);
            return Ok(response);  // ✅ Senha redefinida! Você já pode fazer login
        }
        catch (AppException ex)
        {
            // ❌ Token inválido ou expirado
            return BadRequest(new { message = ex.Message });
        }
    }

    // ============================================================
    // 🔒 MISSÃO 5: ALTERAR SENHA (USUÁRIO AUTENTICADO)
    // ============================================================
    // Analogia RPG: Trocar a senha atual por uma nova!
    // Diferente do reset, aqui o usuário JÁ está logado e quer
    // trocar a senha atual por segurança ou preferência.
    //
    // Fluxo:
    // 1. Usuário já está logado (possui token JWT válido)
    // 2. Informa senha atual + nova senha
    // 3. Sistema valida a senha atual
    // 4. Atualiza para a nova senha
    // ============================================================
    [HttpPost("change-password")]  // Rota: POST /api/auth/change-password
    [Authorize]  // 🔐 REQUER AUTENTICAÇÃO: só quem está logado pode acessar
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        try
        {
            // 🎫 Extrai o ID do usuário do token JWT (do crachá mágico)
            var userId = AuthorizationHelper.GetCurrentUserId(User);
            
            // 🔄 Valida senha antiga e atualiza para nova
            var response = await _passwordResetService.ChangePasswordAsync(request, userId);
            return Ok(response);  // ✅ Senha alterada com sucesso!
        }
        catch (AppException ex)
        {
            // ❌ Senha antiga incorreta ou nova senha inválida
            return BadRequest(new { message = ex.Message });
        }
    }

    // ============================================================
    // 👤 MISSÃO 6: QUEM SOU EU? (INFORMAÇÕES DO USUÁRIO LOGADO)
    // ============================================================
    // Analogia RPG: Abrir o menu de "Status do Personagem"!
    // Retorna as informações completas do usuário atualmente logado,
    // lendo os dados do crachá (token JWT).
    //
    // Fluxo:
    // 1. Extrai o ID do usuário do token JWT
    // 2. Busca os dados completos no banco
    // 3. Retorna: nome, email, role, telefone, nome do paciente, etc
    //
    // Útil para: carregar perfil, mostrar nome na tela, verificar permissões
    // ============================================================
    [HttpGet("me")]  // Rota: GET /api/auth/me
    [Authorize]      // 🔐 REQUER AUTENTICAÇÃO: precisa estar logado
    public async Task<IActionResult> GetCurrentUser()
    {
        try
        {
            // 🎫 Lê o "crachá" (token JWT) e extrai o ID do usuário
            var userId = AuthorizationHelper.GetCurrentUserId(User);
            
            // 📖 Busca os dados completos no "livro de registros" (banco de dados)
            var user = await _authService.GetUserByIdAsync(userId);
            
            if (user == null)
                return NotFound(new { message = "Usuário não encontrado" });  // ❌ Estranho... o ID existe no token mas não no banco!

            // ✅ Retorna os dados do personagem (usuário)
            return Ok(new 
            { 
                userId = user.UserId,          // ID numérico do jogador
                name = user.Name,              // Nome do personagem
                email = user.Email,            // Email de contato
                role = user.Role,              // Classe/Cargo (Admin ou Cuidador)
                patientName = user.PatientName, // Nome do paciente vinculado
                phone = user.Phone             // Telefone de contato
            });
        }
        catch (AppException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
    }
}