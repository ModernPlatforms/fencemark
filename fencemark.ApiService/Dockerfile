# Dockerfile for pre-published .NET application
# This expects the application to be published to the build context
FROM mcr.microsoft.com/dotnet/aspnet:10.0-preview AS final
WORKDIR /app
EXPOSE 8080

# Copy published output
COPY . .

# Set environment variables
ENV ASPNETCORE_URLS=http://+:8080
ENV DOTNET_RUNNING_IN_CONTAINER=true

ENTRYPOINT ["dotnet", "fencemark.ApiService.dll"]
