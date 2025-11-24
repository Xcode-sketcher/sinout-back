# Usa a imagem oficial do .NET SDK para construir a aplicação
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copia o arquivo do projeto e restaura as dependências
COPY ["APISinout.csproj", "."]
RUN dotnet restore "APISinout.csproj"

# Copia o restante do código fonte
COPY . .

# Constrói a aplicação
RUN dotnet build "APISinout.csproj" -c Release -o /app/build

# Publica a aplicação
FROM build AS publish
RUN dotnet publish "APISinout.csproj" -c Release -o /app/publish

# Usa a imagem de runtime para o estágio final
FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine AS base
WORKDIR /app

# Copia a aplicação publicada do estágio de publicação
COPY --from=publish /app/publish .

# Expõe a porta em que a aplicação roda
EXPOSE 80

# Define o ponto de entrada
ENTRYPOINT ["dotnet", "APISinout.dll"]

