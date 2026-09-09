# syntax=docker/dockerfile:1
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY AI-Trading-Platform.sln ./
COPY src ./src
RUN dotnet restore src/AiTrading.Api/AiTrading.Api.csproj
RUN dotnet publish src/AiTrading.Api/AiTrading.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
ENV ASPNETCORE_HTTP_PORTS=8080
COPY --from=build /app/publish .
EXPOSE 8080
USER $APP_UID
ENTRYPOINT ["dotnet", "AiTrading.Api.dll"]
