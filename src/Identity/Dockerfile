FROM microsoft/dotnet:2.2-sdk AS build-env
WORKDIR /app

COPY Identity/*.csproj ./
COPY Infrastructure.WebHost.Customization ../Infrastructure.WebHost.Customization
RUN dotnet restore

COPY Identity ./
RUN dotnet publish -c Release -o out

FROM microsoft/dotnet:2.2-aspnetcore-runtime
COPY --from=build-env /app/out .

EXPOSE 5300/tcp
# Configure environment variables
ENV WEBSITE_SITE_NAME Identity
ENTRYPOINT ["dotnet", "Identity.dll"]