# Use the official .NET 8.0 SDK image to build the app
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

# Set the working directory
WORKDIR /app

# Copy only the project file to restore dependencies
COPY ["src/API/API.csproj", "./"]

RUN dotnet restore

# Copy the entire source code after restoring dependencies
COPY . .

# Change working directory to the API folder
WORKDIR /app/src/API

# Build and publish the application
RUN dotnet publish -c Release -o /out

# Use the official .NET 8.0 ASP.NET runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime

WORKDIR /app
COPY --from=build /out .

# Set the entry point
CMD ["dotnet", "API.dll"]
