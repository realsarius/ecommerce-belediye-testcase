# Dockerfile for .NET 8 API
FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

COPY ["EcommerceAPI.API/EcommerceAPI.API.csproj", "EcommerceAPI.API/"]
COPY ["EcommerceAPI.Business/EcommerceAPI.Business.csproj", "EcommerceAPI.Business/"]
COPY ["EcommerceAPI.DataAccess/EcommerceAPI.DataAccess.csproj", "EcommerceAPI.DataAccess/"]
COPY ["EcommerceAPI.Entities/EcommerceAPI.Entities.csproj", "EcommerceAPI.Entities/"]
COPY ["EcommerceAPI.Core/EcommerceAPI.Core.csproj", "EcommerceAPI.Core/"]
COPY ["EcommerceAPI.Infrastructure/EcommerceAPI.Infrastructure.csproj", "EcommerceAPI.Infrastructure/"]
COPY ["EcommerceAPI.Seeder/EcommerceAPI.Seeder.csproj", "EcommerceAPI.Seeder/"]

RUN dotnet restore "EcommerceAPI.API/EcommerceAPI.API.csproj"

COPY . .
WORKDIR "/src/EcommerceAPI.API"
RUN dotnet build "EcommerceAPI.API.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "EcommerceAPI.API.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

ENTRYPOINT ["dotnet", "EcommerceAPI.API.dll"]
