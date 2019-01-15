FROM microsoft/dotnet:2.2-aspnetcore-runtime AS base
WORKDIR /app
EXPOSE 5300

FROM microsoft/dotnet:2.2-sdk AS build
COPY ["Identity/Identity.csproj", "./"]
COPY Infrastructure.WebHost.Customization ../Infrastructure.WebHost.Customization
RUN dotnet restore "Identity/Identity.csproj"

COPY Identity ./
RUN dotnet build "Identity.csproj" -c Release -o out

FROM build AS publish
RUN dotnet publish "Identity.csproj" -c Release -o out

FROM base AS final
WORKDIR /app
COPY --from=publish /app/out .

# Configure environment variables
ENV WEBSITE_SITE_NAME Identity
ENTRYPOINT ["dotnet", "Pitstop.Identity.dll"]