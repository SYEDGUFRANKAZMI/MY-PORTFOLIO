FROM mcr.microsoft.com/dotnet/sdk:8.0-windowsservercore-ltsc2022 AS build
WORKDIR /src

# Use admin to avoid ContainerUser issue
USER ContainerAdministrator

COPY ["MYPORTFOLIO.csproj", "."]
RUN dotnet restore "./MYPORTFOLIO.csproj"

COPY . .
RUN dotnet publish "./MYPORTFOLIO.csproj" -c Release -o /app/publish

# Final runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0-windowsservercore-ltsc2022 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "MYPORTFOLIO.dll"]
