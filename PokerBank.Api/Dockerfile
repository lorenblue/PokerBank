# This stage is used when running from an IDE in fast mode.
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080

# This stage is used to build the service project.
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["PokerBank.Api/PokerBank.Api.csproj", "PokerBank.Api/"]
COPY ["PokerBank.Domain/PokerBank.Domain.csproj", "PokerBank.Domain/"]
RUN dotnet restore "PokerBank.Api/PokerBank.Api.csproj"
COPY . .
WORKDIR "/src/PokerBank.Api"
RUN dotnet build "PokerBank.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build

# This stage is used to publish the service project.
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "PokerBank.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# This stage is used in production or when running from an IDE in regular mode.
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "PokerBank.Api.dll"]
