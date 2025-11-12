// ============================================================
// 🎯 CONTROLADOR DE MAPEAMENTO DE EMOÇÕES - O TRADUTOR
// ============================================================
// Analogia RPG: Este é o "Livro de Traduções" do jogo!
// Imagina um sistema onde cada emoção detectada é como um "feitiço mágico",
// e este controlador define que "palavras mágicas" são invocadas quando
// o feitiço atinge determinada força (intensidade).
//
// Analogia da Cozinha: É o "Cardápio Personalizado"!
// Cada cliente (paciente) tem preferências específicas:
// - Se detectamos "felicidade" > 80%, servimos "Quero água"
// - Se detectamos "tristeza" > 70%, servimos "Preciso de ajuda"
// - Máximo de 2 pratos (mensagens) por tipo de tempero (emoção)
//
// Regras importantes:
// 1. Cada paciente pode ter até 2 palavras/mensagens por emoção
// 2. Cada regra tem um percentual mínimo para ser acionada
// 3. Priority 1 ou 2 define a ordem de exibição
// ============================================================

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using APISinout.Models;
using APISinout.Services;
using APISinout.Helpers;

namespace APISinout.Controllers;

[ApiController]
[Route("api/emotion-mappings")]
[Authorize]  // 🔐 Só usuários autenticados podem gerenciar regras
public class EmotionMappingController : ControllerBase
{
    // 📜 INVENTÁRIO: O livro de traduções
    private readonly IEmotionMappingService _mappingService;

    // 🏗️ CONSTRUTOR: Pegando o livro
    public EmotionMappingController(IEmotionMappingService mappingService)
    {
        _mappingService = mappingService;
    }

    // ============================================================
    // ✨ MISSÃO 1: CRIAR NOVA REGRA DE TRADUÇÃO
    // ============================================================
    // Analogia RPG: Criar um novo "encantamento" no grimório!
    // O mago (cuidador) define: "Quando detectar emoção X com força Y%, invocar palavra Z"
    //
    // Exemplo prático:
    // - Emoção: "happy" (feliz)
    // - MinPercentage: 80% (tem que estar BEM feliz)
    // - Message: "Quero passear" (o que o paciente quer dizer)
    // - Priority: 1 (primeira opção)
    // ============================================================
    [HttpPost]  // Rota: POST /api/emotion-mappings
    public async Task<IActionResult> CreateMapping([FromBody] EmotionMappingRequest request)
    {
        try
        {
            // 🎫 Quem está criando esta regra?
            var userId = AuthorizationHelper.GetCurrentUserId(User);
            var userRole = AuthorizationHelper.GetCurrentUserRole(User);

            // Se não especificou userId, assume que é para si mesmo
            if (request.UserId == 0)
                request.UserId = userId;

            // ✨ Criar a regra mágica!
            var response = await _mappingService.CreateMappingAsync(request, userId, userRole);
            return CreatedAtAction(nameof(GetMappingsByUser), new { userId = response.UserId }, response);
        }
        catch (AppException ex)
        {
            // ❌ Erro: limite de regras atingido, dados inválidos, etc
            return BadRequest(new { message = ex.Message });
        }
    }

    // ============================================================
    // 📖 MISSÃO 2: VER REGRAS DE UM USUÁRIO ESPECÍFICO
    // ============================================================
    // Analogia RPG: Ler o grimório de outro mago!
    // Admin pode ler qualquer grimório, Caregiver só o próprio.
    // ============================================================
    [HttpGet("user/{userId}")]  // Rota: GET /api/emotion-mappings/user/123
    public async Task<IActionResult> GetMappingsByUser(int userId)
    {
        try
        {
            var currentUserId = AuthorizationHelper.GetCurrentUserId(User);
            var userRole = AuthorizationHelper.GetCurrentUserRole(User);

            // 📜 Buscar todas as regras deste usuário
            var mappings = await _mappingService.GetMappingsByUserAsync(userId, currentUserId, userRole);
            return Ok(mappings);
        }
        catch (AppException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // ============================================================
    // 📝 MISSÃO 3: VER MINHAS PRÓPRIAS REGRAS
    // ============================================================
    // Analogia RPG: Abrir meu próprio grimório!
    // Atalho para ver as regras do usuário autenticado.
    // ============================================================
    [HttpGet("my-rules")]  // Rota: GET /api/emotion-mappings/my-rules
    public async Task<IActionResult> GetMyMappings()
    {
        try
        {
            var userId = AuthorizationHelper.GetCurrentUserId(User);
            var userRole = AuthorizationHelper.GetCurrentUserRole(User);

            // 📜 Buscar minhas regras
            var mappings = await _mappingService.GetMappingsByUserAsync(userId, userId, userRole);
            return Ok(mappings);
        }
        catch (AppException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // ============================================================
    // ✏️ MISSÃO 4: ATUALIZAR UMA REGRA EXISTENTE
    // ============================================================
    // Analogia RPG: Reescrever um encantamento no grimório!
    // Pode mudar a palavra, o percentual mínimo, a prioridade, etc.
    // ============================================================
    [HttpPut("{id}")]  // Rota: PUT /api/emotion-mappings/abc123
    public async Task<IActionResult> UpdateMapping(string id, [FromBody] EmotionMappingRequest request)
    {
        try
        {
            var userId = AuthorizationHelper.GetCurrentUserId(User);
            var userRole = AuthorizationHelper.GetCurrentUserRole(User);

            // 🔄 Atualizar a regra
            var response = await _mappingService.UpdateMappingAsync(id, request, userId, userRole);
            return Ok(response);
        }
        catch (AppException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // ============================================================
    // 🗑️ MISSÃO 5: DELETAR UMA REGRA
    // ============================================================
    // Analogia RPG: Arrancar uma página do grimório!
    // Remove a regra permanentemente.
    // ============================================================
    [HttpDelete("{id}")]  // Rota: DELETE /api/emotion-mappings/abc123
    public async Task<IActionResult> DeleteMapping(string id)
    {
        try
        {
            var userId = AuthorizationHelper.GetCurrentUserId(User);
            var userRole = AuthorizationHelper.GetCurrentUserRole(User);

            // 🗑️ Apagar a regra
            await _mappingService.DeleteMappingAsync(id, userId, userRole);
            return Ok(new { message = "Mapeamento removido com sucesso" });
        }
        catch (AppException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
