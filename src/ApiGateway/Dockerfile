FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY . .

RUN dotnet restore src/ApiGateway/ApiGateway.csproj
RUN dotnet publish src/ApiGateway/ApiGateway.csproj -c Release -o /out

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app

COPY --from=build /out .

ENTRYPOINT ["dotnet", "ApiGateway.dll"]