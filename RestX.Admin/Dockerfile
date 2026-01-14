FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY *.sln .
COPY RestX.Admin/*.csproj RestX.Admin/
COPY RestX.AdminDAL/*.csproj RestX.AdminDAL/
COPY RestX.DAL/*.csproj RestX.DAL/
COPY RestX.Models/*.csproj RestX.Models/
COPY RestX.Services/*.csproj RestX.Services/
RUN dotnet restore RestX.Admin/RestX.Admin.csproj

COPY . .
WORKDIR /src/RestX.Admin
RUN dotnet publish -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 4999
ENTRYPOINT ["dotnet", "RestX.Admin.dll"]

