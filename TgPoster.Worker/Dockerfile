FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8090
EXPOSE 8091
USER root
RUN apt-get update && apt-get install -y --no-install-recommends ffmpeg && rm -rf /var/lib/apt/lists/*
USER $APP_UID

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["TgPoster.Worker/TgPoster.Worker.csproj", "TgPoster.Worker/"]
COPY ["Security/Security.csproj", "Security/"]
COPY ["TgPoster.Storage/TgPoster.Storage.csproj", "TgPoster.Storage/"]
COPY ["TgPoster.API.Domain/TgPoster.API.Domain.csproj", "TgPoster.API.Domain/"]
COPY ["Shared/Shared.csproj", "Shared/"]
COPY ["TgPoster.Worker.Domain/TgPoster.Worker.Domain.csproj", "TgPoster.Worker.Domain/"]
RUN dotnet restore "TgPoster.Worker/TgPoster.Worker.csproj"
COPY . .
WORKDIR "/src/TgPoster.Worker"
RUN dotnet publish "TgPoster.Worker.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "TgPoster.Worker.dll"]
