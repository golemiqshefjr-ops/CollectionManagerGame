FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY CollectionManager.Api/CollectionManager.Api.csproj CollectionManager.Api/
RUN dotnet restore CollectionManager.Api/CollectionManager.Api.csproj

COPY . .
RUN dotnet publish CollectionManager.Api/CollectionManager.Api.csproj \
    --configuration Release \
    --output /app/publish \
    --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_ENVIRONMENT=Production
EXPOSE 10000
ENTRYPOINT ["sh", "-c", "dotnet CollectionManager.Api.dll --urls http://0.0.0.0:${PORT:-10000}"]

