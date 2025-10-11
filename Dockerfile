# Stage 1: Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj and restore dependencies
COPY ["MY PORTFOLIO.csproj", "./"]
RUN dotnet restore "./MY PORTFOLIO.csproj"

# Copy the rest of the source code and build
COPY . .
RUN dotnet publish "MY PORTFOLIO.csproj" -c Release -o /app/publish

# Stage 2: Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "MY PORTFOLIO.dll"]
