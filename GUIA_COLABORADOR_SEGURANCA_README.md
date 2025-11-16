# Guia para Contribuidores — Configuração de Secrets e Variáveis de Ambiente

Este documento explica como configurar o ambiente local para desenvolver e executar os testes do projeto sem submeter credenciais ao repositório.

IMPORTANTE: NÃO comite secrets (por exemplo, `appsettings.Development.json`) no Git. Este guia utiliza `dotnet user-secrets` e variáveis de ambiente para armazenar secrets localmente.

---

## 1) Onde rodar os comandos

Abra um **PowerShell** no diretório do projeto, por exemplo:
```powershell
cd C:\Users\Eduar\Downloads\PROA\sinout-back
```

Os exemplos abaixo usam `--project APISinout.csproj` para garantir que os comandos atinjam o projeto certo.

---

## 2) Inicializar `dotnet user-secrets` (uma vez)

Você só precisa executar isto uma vez por desenvolvedor/projeto:
```powershell
dotnet user-secrets init --project APISinout.csproj
```

Isso adiciona um `UserSecretsId` ao `.csproj` e cria um armazenamento local (por usuário) para secrets.

---

## 3) Comandos `dotnet user-secrets`

Defina apenas em seu ambiente local (não submeter ao repo):

- MongoDB:
```powershell
dotnet user-secrets set "MongoDb:ConnectionString" "STRING_AQUI" --project APISinout.csproj
```

- JWT (chave secreta — mínimo 32 caracteres recomendado):
```powershell
dotnet user-secrets set "Jwt:Key" "CHAVE_JWT_MINIMAMENTE_SEGURA" --project APISinout.csproj
```

- Email (Mailtrap para desenvolvimento):
```powershell
dotnet user-secrets set "Email:SmtpServer" "live.smtp.mailtrap.io" --project APISinout.csproj
dotnet user-secrets set "Email:SmtpPort" "587" --project APISinout.csproj
dotnet user-secrets set "Email:Username" "smtp@mailtrap.io" --project APISinout.csproj
dotnet user-secrets set "Email:Password" "<SEU_TOKEN_MAILTRAP>" --project APISinout.csproj
dotnet user-secrets set "Email:FromEmail" "hello@demomailtrap.co" --project APISinout.csproj
```


- Para listar secrets locais e verificar:
```powershell
dotnet user-secrets list --project APISinout.csproj
```

- Para remover um secret específico:
```powershell
dotnet user-secrets remove "Email:Password" --project APISinout.csproj
```

- Para limpar todos os secrets (uso raro / destrutivo):
```powershell
dotnet user-secrets clear --project APISinout.csproj
```

---

## 4) Variáveis de ambiente (PowerShell) — uso temporário para uma sessão

As variáveis definidas com `$env:` existem somente na sessão de terminal atual e são úteis para testar rapidamente sem persistir.
Lembre-se: para mapear chaves aninhadas use `__` (duplo underscore) em vez de `:`.

- Definir JWT para a sessão atual:
```powershell
$env:Jwt__Key = "CHAVE_JWT_MINIMAMENTE_SEGURA"
```

- Definir MongoDB apenas para a sessão:
```powershell
$env:MongoDb__ConnectionString = "STRING_AQUI"
```

- Definir Email (Mailtrap) apenas para a sessão:
```powershell
$env:Email__SmtpServer = "live.smtp.mailtrap.io"
$env:Email__SmtpPort = "587"
$env:Email__Username = "smtp@mailtrap.io"
$env:Email__Password = "<SEU_TOKEN_MAILTRAP>"
$env:Email__FromEmail = "hello@demomailtrap.co"
```

- Remover variável session:
```powershell
Remove-Item Env:Jwt__Key
# ou
$env:Jwt__Key = $null
```
