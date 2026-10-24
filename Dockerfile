# Imagem da API (o app MAUI é distribuído como APK, não roda em container)
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY global.json Directory.Build.props Directory.Packages.props ./
COPY src/ChateauDuPet.Contracts/ChateauDuPet.Contracts.csproj src/ChateauDuPet.Contracts/
COPY src/ChateauDuPet.Api/ChateauDuPet.Api.csproj src/ChateauDuPet.Api/
RUN dotnet restore src/ChateauDuPet.Api/ChateauDuPet.Api.csproj
COPY src/ChateauDuPet.Contracts/ src/ChateauDuPet.Contracts/
COPY src/ChateauDuPet.Api/ src/ChateauDuPet.Api/
RUN dotnet publish src/ChateauDuPet.Api/ChateauDuPet.Api.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app .
USER $APP_UID
ENV ASPNETCORE_ENVIRONMENT=Production
EXPOSE 8080
ENTRYPOINT ["dotnet", "ChateauDuPet.Api.dll"]
