# Build Stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /source

# Copy everything
COPY . .
RUN dotnet restore "ErmayMuhasebe.Functions/ErmayMuhasebe.Functions.csproj"
RUN dotnet publish "ErmayMuhasebe.Functions/ErmayMuhasebe.Functions.csproj" -c Release -o /app

# Runtime Stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
COPY --from=build /app .

# Install font dependencies for SkiaSharp
RUN apt-get update && apt-get install -y libfontconfig1 libfreetype6

# Expose port
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "ErmayMuhasebe.Functions.dll"]
