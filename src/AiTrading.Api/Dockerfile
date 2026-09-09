# syntax=docker/dockerfile:1
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY AI-Trading-Platform.sln ./
COPY src ./src
RUN dotnet restore src/AiTrading.Api/AiTrading.Api.csproj
RUN dotnet publish src/AiTrading.Api/AiTrading.Api.csproj -c Release -o /app/publish --no-restore
RUN dotnet tool install --global dotnet-ef --version 9.0.0
ENV PATH="/root/.dotnet/tools:${PATH}"
RUN dotnet ef migrations bundle --project src/AiTrading.Infrastructure/AiTrading.Infrastructure.csproj --startup-project src/AiTrading.Api/AiTrading.Api.csproj --self-contained --runtime linux-x64 --output /app/efbundle

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
ENV ASPNETCORE_HTTP_PORTS=8080
COPY --from=build /app/publish .
COPY --from=build /app/efbundle /app/efbundle
RUN chmod +x /app/efbundle
EXPOSE 8080
USER $APP_UID
ENTRYPOINT ["dotnet", "AiTrading.Api.dll"]
