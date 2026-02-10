# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["AccessManager.sln", "./"]
COPY ["AccessManager.Domain/AccessManager.Domain.csproj", "AccessManager.Domain/"]
COPY ["AccessManager.Application/AccessManager.Application.csproj", "AccessManager.Application/"]
COPY ["AccessManager.Infrastructure/AccessManager.Infrastructure.csproj", "AccessManager.Infrastructure/"]
COPY ["AccessManager.Web/AccessManager.UI.csproj", "AccessManager.Web/"]

RUN dotnet restore "AccessManager.sln"

COPY . .
RUN dotnet publish "AccessManager.Web/AccessManager.UI.csproj" -c Release -o /app/publish --no-restore

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

COPY --from=build /app/publish .

# Cloud Run beklediği port
ENV ASPNETCORE_URLS=http://*:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "AccessManager.UI.dll"]
