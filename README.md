#  API Sinout - Back-end ASP.NET Core

<p align="center">
	<img src="./sinout_logo.jpeg" alt="Sinout logo" width="900" />
</p>

Bem vindo ao repositório do back-end da Sinout.
Este repositório contém a API RESTful construída em .NET 8, com repositórios, serviços, validadores e uma suíte de testes (unitários e algumas integrações).


## Sobre
Esta API do Sinout permite a ligação entre os sistemas de reconhecimento facial, e a aplicação front-end, permitindo operações como autenticação, mapeamento de regras, consulta de histórico, envio de e-mails e mais... devolvidos em formato JSON.

## 💻Tecnologias Utilizadas
- [![MongoDB](https://img.shields.io/badge/MongoDB-4EA94B?logo=mongodb&logoColor=white&style=for-the-badge)](#)
- [![.NET](https://img.shields.io/badge/.NET-5C2D91?logo=.net&logoColor=white&style=for-the-badge)](#)
- [![C#](https://img.shields.io/badge/C%23-239120?logo=c-sharp&logoColor=white&style=for-the-badge)](#)

## 🧪Testes e QA
O projeto possui uma suíte de testes (unitários + integrações) e cobertura de código automatizada.
Última análise local (19/11/2025 02:33:03):

- Cobertura de linhas: **79.42%** (1729 / 2177)
- Cobertura de branches: **69.00%** (305 / 442)

Meta de qualidade: atingir **>80%** de cobertura por linhas e branches.



## 👥Equipe

| Nome      | Função                | GitHub                                               |
|-----------|-----------------------|------------------------------------------------------|
| Luana     | Product Owner         | [@luanarochamiron](https://github.com/luanarochamiron) |
| Fabio     | Scrum Master          | [@FabioRoberto-ppt](https://github.com/FabioRoberto-ppt) |
| Guilherme | Desenvolvedor         | [@GuilhermefDomingues](https://github.com/GuilhermefDomingues) |
| Erick Isaac     | Desenvolvedor         | [@IsaacZ33](https://github.com/IsaacZ33)             |
| Felipe    | Desenvolvedor         | [@Felipe-Koshimizu](https://github.com/Felipe-Koshimizu) |
| Eduardo   | Desenvolvedor e QA    | [@Xcode-sketcher](https://github.com/Xcode-sketcher) |


## 📐Padrão de Arquitetura do Projeto
O projeto adota o padrão **Monolito modular / Clean-style (layered)**, o que garante simplicidade nos processos de build/deploy. O projeto atual oferece inúmeras vantagens:

- Simplicidade de deploy e debug
- Fácil compartilhamento de modelos, DTOs e utilitários entre camadas
- Menor overhead operacional comparado com micro serviços
- Fácil migração para **Clean Architecture** se necessesário

A estrutura segue:

- **Controllers**: definem os endpoints HTTP e a orquestração de requisições — recebem a request, aplicam validação, chamam os services e retornam DTOs/respostas. Ex: `AuthController`, `PatientController`.
- **Models**: representam as entidades do domínio e os modelos de transporte (DTOs / RequestModels) — por exemplo `User`, `Patient`, `HistoryRecord`, `RequestModels`.
- **Services**: implementam a lógica de negócio e orquestram regras do domínio — ex: `AuthService`, `PatientService`, `PasswordResetService`. Serviços lidam com validação de regras, fluxo de transações e chamadas a repositórios/infra.
- **Data / Repositories**: acesso a persistência (MongoDB) e implementação dos repositórios (`UserRepository`, `PatientRepository`, `HistoryRepository`) que encapsulam queries e mapeamento para entidades.
- **Validators**: validação de input via FluentValidation (`RegisterRequestValidator`, `LoginRequestValidator`) para proteger endpoints de dados inválidos antes do serviço processar.
- **Helpers / Utilitários**: utilitários transversais, p.ex. `JwtHelper`, `AuthorizationHelper`, funções de mapeamento e utilidades de email/templating.
- **Infra / Hosted Services**: background services e infra (ex.: `TokenCleanupService`, `RateLimitService`) que rodam em segundo plano e provêm infra cross-cutting.
- **Program.cs**: o composition root — configura DI, authentication, swagger, CORS, rate limiting e registra services/repositories; ponto de inicialização da aplicação.
- **APISinout.Tests/**: testes unitários e integração com fixtures (`Fixtures/`), testes por domínio em `Unit/Services`, `Validators`, e integrações em `Integration/Controllers`.



## Endpoints Principais (Resumo)
**Autenticação**
| Método | Endpoint | Função |
|--------|-------------------------|-------------------------------|
| POST   | `/api/auth/register`    | Criar usuário (registro) |
| POST   | `/api/auth/login`       | Login e token JWT |
| POST   | `/api/auth/request-reset`| Solicitar reset de senha |
| POST   | `/api/auth/reset-password`| Confirmar reset de senha |
| POST   | `/api/auth/resend-reset-code`| Reenviar código de reset |

**Usuários e Pacientes**
- `GET /api/patient` - Listar pacientes
- `POST /api/patient` - Criar paciente
- `PUT /api/patient/{id}` - Atualizar paciente
- `DELETE /api/patient/{id}` - Deletar paciente

**Emotion Mapping & History**
- `POST /api/emotionmapping` - Mapear emoção
- `GET /api/history/{patientId}` - Recuperar histórico de emoções do paciente

> 🔐 Rotas protegidas exigem header Authorization: `Bearer <token>`

---



