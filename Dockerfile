FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /app

# Copy csproj and restore dependencies
COPY TeeTime/*.csproj ./TeeTime/
COPY TTS-Beta.sln .
RUN dotnet restore

# Copy the rest of the source code
COPY . .

# Build the project
WORKDIR /app/TeeTime
RUN dotnet build -c Release --no-restore

# Publish the application
RUN dotnet publish -c Release -o /app/publish --no-build

# Build runtime image
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

# Set environment variables
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

# Expose port 8080
EXPOSE 8080

# Start the application
ENTRYPOINT ["dotnet", "TeeTime.dll"]