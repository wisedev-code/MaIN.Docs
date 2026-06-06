# Build context: MaIN.Docs/backend/
# docker build -t main-docs-api .

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /build

COPY MaIN.Docs.Api/MaIN.Docs.Api.csproj MaIN.Docs.Api/
RUN dotnet restore MaIN.Docs.Api/MaIN.Docs.Api.csproj

COPY MaIN.Docs.Api/ MaIN.Docs.Api/
RUN dotnet publish MaIN.Docs.Api/MaIN.Docs.Api.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

COPY --from=build /app .

# docs-content is mounted as a volume in production; copy defaults for local builds
COPY ../docs-content /app/docs-content

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "MaIN.Docs.Api.dll"]
