FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["BandHub.Gateway/BandHub.Gateway.csproj", "BandHub.Gateway/"]
RUN dotnet restore "BandHub.Gateway/BandHub.Gateway.csproj"
COPY . .
WORKDIR "/src/BandHub.Gateway"
RUN dotnet build "BandHub.Gateway.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "BandHub.Gateway.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "BandHub.Gateway.dll"]
