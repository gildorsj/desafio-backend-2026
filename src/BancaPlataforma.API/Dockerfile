FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /source

COPY ["src/BancaPlataforma.API/BancaPlataforma.API.csproj",                       "src/BancaPlataforma.API/"]
COPY ["src/BancaPlataforma.Application/BancaPlataforma.Application.csproj",       "src/BancaPlataforma.Application/"]
COPY ["src/BancaPlataforma.Domain/BancaPlataforma.Domain.csproj",                 "src/BancaPlataforma.Domain/"]
COPY ["src/BancaPlataforma.Infrastructure/BancaPlataforma.Infrastructure.csproj", "src/BancaPlataforma.Infrastructure/"]

RUN dotnet restore "src/BancaPlataforma.API/BancaPlataforma.API.csproj"

COPY . .

    RUN dotnet publish "src/BancaPlataforma.API/BancaPlataforma.API.csproj" \
-c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

RUN addgroup --system appgroup && adduser --system --ingroup appgroup appuser
    USER appuser

    COPY --from=build /app/publish .

    EXPOSE 8080
ENTRYPOINT ["dotnet", "BancaPlataforma.API.dll"]
