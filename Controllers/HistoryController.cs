// ============================================================
// 📚 CONTROLADOR DE HISTÓRICO - O DIÁRIO DE BORDO
// ============================================================
// Analogia RPG: Este é o "Livro de Registros" do aventureiro!
// Imagina um diário onde você anota TODAS as batalhas, tesouros encontrados
// e conquistas. Aqui guardamos o histórico de emoções detectadas.
//
// Analogia da Cozinha: É como o "Caderno de Pedidos"!
// Toda vez que um prato (emoção) é servido, anotamos:
// - Que prato foi? (qual emoção)
// - Quão saboroso estava? (percentual de intensidade)
// - Cliente gostou? (mensagem disparada ou não)
// - Que horas foi servido? (timestamp)
//
// Funcionalidades:
// 1. Ver histórico de análises (últimas 24 horas por padrão)
// 2. Ver estatísticas e tendências (dashboard)
// 3. Filtrar por período, emoção, etc
// 4. Salvar novas emoções detectadas
// 5. Limpar registros antigos (Admin)
// ============================================================

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Authorization;
using APISinout.Models;
using APISinout.Services;
using APISinout.Helpers;

namespace APISinout.Controllers;

[ApiController]
[Route("api/history")]
[Authorize]  // 🔐 Todas as rotas precisam de autenticação
[EnableRateLimiting("limite-api")] // Limite geral para o controller
public class HistoryController : ControllerBase
{
    // 📖 INVENTÁRIO: O livro de registros
    private readonly IHistoryService _historyService;

    // 🏗️ CONSTRUTOR: Pegando o livro na estante
    public HistoryController(IHistoryService historyService)
    {
        _historyService = historyService;
    }

    // ============================================================
    // 📜 MISSÃO 1: VER HISTÓRICO DE UM USUÁRIO ESPECÍFICO
    // ============================================================
    // Analogia RPG: Ler o diário de um personagem específico!
    // Admin pode ler qualquer diário, mas Cuidador só pode ler o próprio.
    //
    // Parâmetros:
    // - userId: ID do usuário cujo histórico queremos ver
    // - hours: quantas horas olhar para trás (padrão: 24h)
    //
    // Retorna: Lista de registros de emoções detectadas
    // ============================================================
    [HttpGet("user/{userId}")]  // Rota: GET /api/history/user/123?hours=48
    public async Task<IActionResult> GetHistoryByUser(int userId, [FromQuery] int hours = 24)
    {
        try
        {
            // 🎫 Quem está fazendo a requisição?
            var currentUserId = AuthorizationHelper.GetCurrentUserId(User);
            var userRole = AuthorizationHelper.GetCurrentUserRole(User);

            // 📖 Busca o histórico (com validação de permissões dentro do service)
            var history = await _historyService.GetHistoryByUserAsync(userId, currentUserId, userRole, hours);
            return Ok(history);  // ✅ Aqui está o diário!
        }
        catch (AppException ex)
        {
            // ❌ Sem permissão ou usuário não encontrado
            return BadRequest(new { message = ex.Message });
        }
    }

    // ============================================================
    // 📔 MISSÃO 2: VER MEU PRÓPRIO HISTÓRICO
    // ============================================================
    // Analogia RPG: Abrir o "Diário de Bordo" do seu personagem!
    // É um atalho para ver suas próprias aventuras (emoções detectadas).
    //
    // Útil para: Dashboard pessoal, ver padrões recentes
    // ============================================================
    [HttpGet("my-history")]  // Rota: GET /api/history/my-history?hours=24
    public async Task<IActionResult> GetMyHistory([FromQuery] int hours = 24)
    {
        try
        {
            // 🎫 Extrair identidade do token
            var userId = AuthorizationHelper.GetCurrentUserId(User);
            var userRole = AuthorizationHelper.GetCurrentUserRole(User);
            
            Console.WriteLine($"[DEBUG] UserId extraído: {userId}, Role: {userRole}");

            // 📖 Buscar histórico próprio
            var history = await _historyService.GetHistoryByUserAsync(userId, userId, userRole, hours);
            Console.WriteLine($"[DEBUG] Histórico recuperado: {history.Count} registros");
            return Ok(history);
        }
        catch (AppException ex)
        {
            Console.WriteLine($"[DEBUG] ❌ AppException: {ex.Message}");
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[DEBUG] ❌ Exception: {ex.Message}");
            return StatusCode(500, new { message = "Erro interno", error = ex.Message });
        }
    }

    // ============================================================
    // 🔍 MISSÃO 3: BUSCA AVANÇADA COM FILTROS
    // ============================================================
    // Analogia RPG: Procurar no diário com critérios específicos!
    // Como buscar "todas as batalhas contra dragões na semana passada"
    //
    // Filtros disponíveis:
    // - Período (data início/fim)
    // - Emoção dominante específica
    // - Se houve mensagem disparada
    // - Paginação (quantos resultados por página)
    // ============================================================
    [HttpPost("filter")]  // Rota: POST /api/history/filter
    public async Task<IActionResult> GetHistoryByFilter([FromBody] HistoryFilter filter)
    {
        try
        {
            var userId = AuthorizationHelper.GetCurrentUserId(User);
            var userRole = AuthorizationHelper.GetCurrentUserRole(User);

            // 🔎 Buscar com filtros personalizados
            var history = await _historyService.GetHistoryByFilterAsync(filter, userId, userRole);
            return Ok(history);
        }
        catch (AppException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // ============================================================
    // 📊 MISSÃO 4: ESTATÍSTICAS DE UM USUÁRIO ESPECÍFICO
    // ============================================================
    // Analogia RPG: Ver o "Painel de Conquistas" de um personagem!
    // Mostra resumos como:
    // - Quantas vezes ficou feliz/triste/com raiva
    // - Qual emoção mais frequente
    // - Quais mensagens foram mais disparadas
    // - Tendências por hora do dia
    //
    // É como um resumo de experiência ganha no jogo!
    // ============================================================
    [HttpGet("statistics/user/{userId}")]  // Rota: GET /api/history/statistics/user/123?hours=24
    public async Task<IActionResult> GetUserStatistics(int userId, [FromQuery] int hours = 24)
    {
        try
        {
            // 🎫 Quem está pedindo?
            var currentUserId = AuthorizationHelper.GetCurrentUserId(User);
            var userRole = AuthorizationHelper.GetCurrentUserRole(User);

            // 📊 Gerar estatísticas
            var stats = await _historyService.GetUserStatisticsAsync(userId, currentUserId, userRole, hours);
            return Ok(stats);
        }
        catch (AppException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // ============================================================
    // 📈 MISSÃO 5: MINHAS PRÓPRIAS ESTATÍSTICAS
    // ============================================================
    // Analogia RPG: Ver o seu próprio "Painel de Conquistas"!
    // Atalho para ver as estatísticas do usuário autenticado.
    //
    // Usado no Dashboard principal para mostrar:
    // - Gráficos de emoções ao longo do tempo
    // - Palavras mais disparadas
    // - Padrões comportamentais
    // ============================================================
    [HttpGet("statistics/my-stats")]  // Rota: GET /api/history/statistics/my-stats?hours=24
    public async Task<IActionResult> GetMyStatistics([FromQuery] int hours = 24)
    {
        try
        {
            Console.WriteLine($"[DEBUG] GetMyStatistics chamado, hours={hours}");
            
            // 🎫 Identificar usuário
            var userId = AuthorizationHelper.GetCurrentUserId(User);
            var userRole = AuthorizationHelper.GetCurrentUserRole(User);
            
            Console.WriteLine($"[DEBUG] UserId: {userId}, Role: {userRole}");

            // 📊 Calcular estatísticas
            var stats = await _historyService.GetUserStatisticsAsync(userId, userId, userRole, hours);
            Console.WriteLine($"[DEBUG] Estatísticas recuperadas");
            return Ok(stats);
        }
        catch (AppException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Erro interno", error = ex.Message });
        }
    }

    // ============================================================
    // 🗑️ MISSÃO 6: LIMPAR HISTÓRICO ANTIGO (APENAS ADMIN)
    // ============================================================
    // Analogia RPG: Queimar páginas antigas do diário!
    // Remove registros anteriores a X horas para liberar espaço.
    //
    // Analogia da Cozinha: Jogar fora recibos de pedidos antigos!
    // Mantém apenas as notas de pedidos recentes para não lotar o arquivo.
    //
    // CUIDADO: Só Admin pode fazer isso!
    // ============================================================
    [HttpDelete("cleanup")]  // Rota: DELETE /api/history/cleanup?hours=24
    [Authorize(Roles = "Admin")]  // 👑 APENAS ADMIN
    public async Task<IActionResult> CleanupOldHistory([FromQuery] int hours = 24)
    {
        try
        {
            // 🗑️ Limpar registros antigos
            await _historyService.CleanOldHistoryAsync(hours);
            return Ok(new { message = $"Histórico anterior a {hours} horas foi limpo com sucesso" });
        }
        catch (AppException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // ============================================================
    // 💾 MISSÃO 7: SALVAR NOVA EMOÇÃO DETECTADA (O MAIS IMPORTANTE!)
    // ============================================================
    // Analogia RPG: Anotar uma nova aventura no diário!
    // Toda vez que a câmera/IA detecta uma emoção no paciente,
    // este endpoint é chamado para salvar no histórico.
    //
    // Analogia da Cozinha: Registrar um novo pedido!
    // Cliente (paciente) fez um pedido (expressou emoção),
    // anotamos: o que pediu, quão forte foi o pedido, que horas foi.
    //
    // Fluxo completo:
    // 1. API Python DeepFace detecta emoção na câmera
    // 2. Frontend chama este endpoint com os dados
    // 3. Sistema busca se há regra de tradução (EmotionMapping)
    // 4. Se houver regra, anexa a mensagem ao histórico
    // 5. Salva tudo no banco
    // 6. Retorna a mensagem (se houver) para exibir na tela
    // ============================================================
    [HttpPost("cuidador-emotion")]  // Rota: POST /api/history/cuidador-emotion
    [EnableRateLimiting("limite-emotion")] // Limite específico para detecção de emoções
    public async Task<IActionResult> SaveCuidadorEmotion([FromBody] CuidadorEmotionRequest? request)
    {
        try
        {
            if (request != null)
            {
                Console.WriteLine($"  CuidadorId: {request.CuidadorId}");
                Console.WriteLine($"  PatientName: {request.PatientName}");
                Console.WriteLine($"  DominantEmotion: {request.DominantEmotion}");
                Console.WriteLine($"  EmotionsDetected: {request.EmotionsDetected?.Count ?? 0} emoções");
                Console.WriteLine($"  Timestamp: {request.Timestamp}");
            }

            // ❌ VALIDAÇÃO 1: Request válido?
            if (request == null || request.CuidadorId == 0)
            {
                return BadRequest(new { sucesso = false, message = "Request vazio ou formato inválido - verifique o JSON" });
            }

            // 🎫 Quem está enviando esta emoção?
            var userId = AuthorizationHelper.GetCurrentUserId(User);
            var userRole = AuthorizationHelper.GetCurrentUserRole(User);

            // 🔒 VALIDAÇÃO 2: O cuidador está tentando salvar emoção para si mesmo?
            // (Impedir que alguém salve emoções em nome de outro)
            if (request.CuidadorId != userId && userRole != "Admin")
            {
                return Forbid();  // ❌ Não autorizado!
            }

            // 🎯 FASE 1: BUSCAR REGRA DE TRADUÇÃO (se houver)
            // Analogia: Consultar o "dicionário" para ver se esta emoção tem tradução
            var emotionMappingService = HttpContext.RequestServices.GetService<IEmotionMappingService>();
            string? triggeredMessage = null;  // A palavra/frase a ser exibida (se houver)
            string? triggeredRuleId = null;   // ID da regra que foi acionada

            if (!string.IsNullOrEmpty(request.DominantEmotion) && request.EmotionsDetected != null)
            {
                // Pegar o percentual da emoção dominante
                var percentage = request.EmotionsDetected.GetValueOrDefault(request.DominantEmotion, 0);
                
                if (emotionMappingService != null)
                {
                    // Procurar regra que combine: emoção + percentual mínimo
                    var ruleResult = await emotionMappingService.FindMatchingRuleAsync(
                        userId, 
                        request.DominantEmotion, 
                        percentage
                    );
                    triggeredMessage = ruleResult.message;
                    triggeredRuleId = ruleResult.ruleId;
                }
            }

            // 📝 FASE 2: CRIAR REGISTRO DE HISTÓRICO
            // Analogia: Escrever nova página no diário
            var historyRecord = new HistoryRecord
            {
                UserId = userId,                           // Cuidador dono deste registro
                PatientName = request.PatientName ?? "Paciente",  // Nome do paciente
                Timestamp = request.Timestamp ?? DateTime.UtcNow, // Quando aconteceu
                EmotionsDetected = request.EmotionsDetected,      // Todas as emoções com %
                DominantEmotion = request.DominantEmotion,        // Emoção principal
                DominantPercentage = request.EmotionsDetected?.GetValueOrDefault(request.DominantEmotion ?? "", 0) ?? 0,  // % da emoção principal
                MessageTriggered = triggeredMessage,              // Palavra disparada (ou null)
                TriggeredRuleId = triggeredRuleId                 // ID da regra usada (ou null)
            };

            // 💾 FASE 3: SALVAR NO BANCO DE DADOS
            await _historyService.CreateHistoryRecordAsync(historyRecord);

            // ✅ FASE 4: RETORNAR RESPOSTA
            // Retorna a mensagem para o frontend exibir na tela (se houver)
            return Ok(new 
            { 
                sucesso = true,
                message = "Emoção registrada com sucesso",
                cuidadorId = request.CuidadorId,
                dominantEmotion = request.DominantEmotion,
                suggestedMessage = triggeredMessage,  // ⭐ PALAVRA A SER EXIBIDA!
                timestamp = historyRecord.Timestamp
            });
        }
        catch (AppException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Erro interno ao salvar emoção", error = ex.Message });
        }
    }
}

// ============================================================
// 📦 MODELO AUXILIAR: REQUISIÇÃO DE EMOÇÃO DO CUIDADOR
// ============================================================
// Este modelo define o formato do JSON que o frontend envia
// quando uma nova emoção é detectada.
//
// Exemplo de JSON:
// {
//   "cuidadorId": 123,
//   "patientName": "João Silva",
//   "emotionsDetected": {
//     "happy": 85.5,
//     "sad": 10.2,
//     "angry": 2.1,
//     ...
//   },
//   "dominantEmotion": "happy",
//   "timestamp": "2024-11-12T14:30:00Z"
// }
// ============================================================
public class CuidadorEmotionRequest
{
    public int CuidadorId { get; set; }                        // ID do cuidador
    public string? PatientName { get; set; }                    // Nome do paciente
    public Dictionary<string, double>? EmotionsDetected { get; set; }  // Todas as emoções com %
    public string? DominantEmotion { get; set; }                // Emoção dominante
    public string? Age { get; set; }                            // Idade (opcional, da IA)
    public string? Gender { get; set; }                         // Gênero (opcional, da IA)
    public DateTime? Timestamp { get; set; }                    // Quando foi detectado
}
