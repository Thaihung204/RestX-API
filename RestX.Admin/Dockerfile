# ==============================================
# Stage 1: Build Stage
# ==============================================
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

ARG BUILDCONFIG=Release
ENV BUILDCONFIG=${BUILDCONFIG}

WORKDIR /src

# Copy project files (paths relative to solution root)
COPY ["RestX.Admin/RestX.Admin.csproj", "RestX.Admin/"]
COPY ["RestX.AdminDAL/RestX.AdminDAL.csproj", "RestX.AdminDAL/"]
COPY ["RestX.DAL/RestX.DAL.csproj", "RestX.DAL/"]
COPY ["RestX.Models/RestX.Models.csproj", "RestX.Models/"]
COPY ["RestX.Services/RestX.BLL.csproj", "RestX.Services/"]

# Restore dependencies
RUN dotnet restore "RestX.Admin/RestX.Admin.csproj" -v detailed

# Copy all source code (includes Migrations folder)
COPY . .

# Build project
WORKDIR "/src/RestX.Admin"
RUN dotnet build "RestX.Admin.csproj" \
    -c ${BUILDCONFIG} \
    --no-restore \
    -v detailed

# ==============================================
# Stage 2: Publish Stage
# ==============================================
FROM build AS publish

ARG BUILDCONFIG=Release

RUN dotnet publish "RestX.Admin.csproj" \
    -c ${BUILDCONFIG} \
    -o /app/publish \
    /p:UseAppHost=false \
    --no-restore \
    --no-build

# ==============================================
# Stage 3: Runtime Stage
# ==============================================
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final

WORKDIR /app

# Copy published files (migrations are included in DLL)
COPY --from=publish /app/publish .

# Set environment variables
ENV ASPNETCORE_URLS=http://+:4999 \
    ASPNETCORE_ENVIRONMENT=Production \
    DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false \
    DOTNET_EnableDiagnostics=0

# Create directories
RUN mkdir -p /app/logs && \
    chmod -R 777 /app/logs

# Expose port
EXPOSE 4999

ENTRYPOINT ["dotnet", "RestX.Admin.dll"]