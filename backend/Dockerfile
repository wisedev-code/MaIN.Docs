# Build context: MaIN.Docs/backend/
# docker build -t main-docs-api .

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /build

COPY MaIN.Docs.Api/MaIN.Docs.Api.csproj MaIN.Docs.Api/
RUN dotnet restore MaIN.Docs.Api/MaIN.Docs.Api.csproj

COPY MaIN.Docs.Api/ MaIN.Docs.Api/
RUN dotnet publish MaIN.Docs.Api/MaIN.Docs.Api.csproj \
        -c Release --self-contained false \
        -o /app && \
    rm -rf /app/runtimes

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS runtime
WORKDIR /app

COPY --from=build /app .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "MaIN.Docs.Api.dll"]
