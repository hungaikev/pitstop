FROM microsoft/dotnet:2.2-sdk AS build-env
WORKDIR /app

# Copy necessary files and restore as distinct layer
COPY VehicleManagementAPI/*.csproj ./
# Copy referenced Service Discovery project
COPY Infrastructure.ServiceDiscovery ../Infrastructure.ServiceDiscovery
RUN dotnet restore -s https://api.nuget.org/v3/index.json -s https://www.myget.org/F/pitstop/api/v3/index.json

# Copy everything else and build
COPY VehicleManagementAPI ./
RUN dotnet publish -c Release -o out

# Build runtime image
FROM microsoft/dotnet:2.2-aspnetcore-runtime
COPY --from=build-env /app/out .

# Expose ports
EXPOSE 5000/tcp
ENV ASPNETCORE_URLS http://*:5000
HEALTHCHECK --interval=30s --timeout=3s --retries=1 CMD curl --silent --fail http://localhost:5000/hc || exit 1

# Configure environment variables
ENV WEBSITE_SITE_NAME VehicleManagementAPI

# Start
ENTRYPOINT ["dotnet", "Pitstop.VehicleManagement.dll"]
