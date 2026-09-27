FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY src/DocVault.WebApi/DocVault.WebApi.csproj src/DocVault.WebApi/
COPY src/DocVault.Core/DocVault.Core.csproj     src/DocVault.Core/
RUN dotnet restore src/DocVault.WebApi/DocVault.WebApi.csproj

COPY src/ src/
RUN dotnet publish src/DocVault.WebApi/DocVault.WebApi.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "DocVault.WebApi.dll"]