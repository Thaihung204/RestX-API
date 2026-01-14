FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY *.sln .
COPY RestX.WebApp/*.csproj RestX.WebApp/
COPY RestX.AdminDAL/*.csproj RestX.AdminDAL/
COPY RestX.DAL/*.csproj RestX.DAL/
COPY RestX.Models/*.csproj RestX.Models/
COPY RestX.Services/*.csproj RestX.Services/
RUN dotnet restore RestX.WebApp/RestX.WebApp.csproj

COPY . .
WORKDIR /src/RestX.WebApp
RUN dotnet publish -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 5000
ENTRYPOINT ["dotnet", "RestX.WebApp.dll"]
