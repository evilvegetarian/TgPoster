FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["TgPoster.API/TgPoster.API.csproj", "TgPoster.API/"]
COPY ["TgPoster.API.Domain/TgPoster.API.Domain.csproj", "TgPoster.API.Domain/"]
COPY ["Security/Security.csproj", "Security/"]
COPY ["Shared/Shared.csproj", "Shared/"]
COPY ["TgPoster.Storage/TgPoster.Storage.csproj", "TgPoster.Storage/"]
COPY ["TgPoster.Worker.Domain/TgPoster.Worker.Domain.csproj", "TgPoster.Worker.Domain/"]
RUN dotnet restore "TgPoster.API/TgPoster.API.csproj"
COPY . .
WORKDIR "/src/TgPoster.API"
RUN dotnet build "TgPoster.API.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "TgPoster.API.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "TgPoster.API.dll"]
