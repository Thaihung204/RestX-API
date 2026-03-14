FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ReverseProxy/ReverseProxy.csproj ReverseProxy/
RUN dotnet restore ReverseProxy/ReverseProxy.csproj

COPY . .

WORKDIR /src/ReverseProxy
RUN dotnet publish -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "ReverseProxy.dll"]
