FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY CivicFlow.sln ./
COPY global.json ./
COPY src/CivicFlow.Domain/CivicFlow.Domain.csproj src/CivicFlow.Domain/
COPY src/CivicFlow.Application/CivicFlow.Application.csproj src/CivicFlow.Application/
COPY src/CivicFlow.Infrastructure/CivicFlow.Infrastructure.csproj src/CivicFlow.Infrastructure/
COPY src/CivicFlow.Api/CivicFlow.Api.csproj src/CivicFlow.Api/

RUN dotnet restore src/CivicFlow.Api/CivicFlow.Api.csproj

COPY src/ src/
RUN dotnet publish src/CivicFlow.Api/CivicFlow.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "CivicFlow.Api.dll"]
