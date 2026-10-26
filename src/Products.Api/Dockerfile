# Build context: repository root (see docker-compose.yml).
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restore separately so the layer is cached when only source files change.
COPY global.json Directory.Build.props .editorconfig ./
COPY src/Products.Domain/Products.Domain.csproj src/Products.Domain/
COPY src/Products.Application/Products.Application.csproj src/Products.Application/
COPY src/Products.Infrastructure/Products.Infrastructure.csproj src/Products.Infrastructure/
COPY src/Products.Api/Products.Api.csproj src/Products.Api/
RUN dotnet restore src/Products.Api/Products.Api.csproj

COPY src/ src/
RUN dotnet publish src/Products.Api/Products.Api.csproj -c Release -o /app/publish --no-restore /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
RUN mkdir -p /app/data && chown -R $APP_UID /app/data
COPY --from=build /app/publish .

ENV ASPNETCORE_HTTP_PORTS=8080 \
    ConnectionStrings__ProductsDb="Data Source=/app/data/products.db"

USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "Products.Api.dll"]
