FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /source

COPY ["src/BancaPlataforma.Worker/BancaPlataforma.Worker.csproj",                 "src/BancaPlataforma.Worker/"]
COPY ["src/BancaPlataforma.Application/BancaPlataforma.Application.csproj",       "src/BancaPlataforma.Application/"]
COPY ["src/BancaPlataforma.Domain/BancaPlataforma.Domain.csproj",                 "src/BancaPlataforma.Domain/"]
COPY ["src/BancaPlataforma.Infrastructure/BancaPlataforma.Infrastructure.csproj", "src/BancaPlataforma.Infrastructure/"]

RUN dotnet restore "src/BancaPlataforma.Worker/BancaPlataforma.Worker.csproj"

COPY . .

RUN dotnet publish "src/BancaPlataforma.Worker/BancaPlataforma.Worker.csproj" \
    -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

RUN addgroup --system appgroup && adduser --system --ingroup appgroup appuser
USER appuser

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "BancaPlataforma.Worker.dll"]