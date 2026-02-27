# Stage 1: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS base
ARG APP_VERSION=0.0.0
ENV APP_VERSION=$APP_VERSION
WORKDIR /app

# Install required libraries for Npgsql/System.Data.SqlClient (Kerberos/GSSAPI)
# and ICU libs for proper string/date handling
RUN apk add --no-cache krb5-libs icu-libs

# Stage 2: SDK + Build Tools
FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build
# Install Node.js and npm for Blazor build process
#RUN apk add --no-cache nodejs npm
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Cache .NET dependencies
COPY ["ShelfBuddy/ShelfBuddy.csproj", "ShelfBuddy/"]
COPY ["ShelfBuddy.Data/ShelfBuddy.Data.csproj", "ShelfBuddy.Data/"]
COPY ["ShelfBuddy.WebApi/ShelfBuddy.WebApi.csproj", "ShelfBuddy.WebApi/"]

# Restore for the specific target RID to ensure native dependencies are correct
RUN dotnet restore "ShelfBuddy.WebApi/ShelfBuddy.WebApi.csproj" -r linux-musl-x64

# Cache NPM dependencies
#COPY ["ShelfBuddy.WebApi/package.json", "ShelfBuddy.WebApi/package-lock.json*", "ShelfBuddy.WebApi/"]
#RUN cd ShelfBuddy.WebApi && npm install

# Copy remaining files
COPY . .

# Build step is optional if using dotnet publish directly, but kept for cache logic
# RUN dotnet build "ShelfBuddy.WebApi.csproj" -c $BUILD_CONFIGURATION -o /app/build

# Stage 3: Publish 
FROM build AS publish
WORKDIR "/src/ShelfBuddy.WebApi"

# Remove --no-restore if assets file issues persist, or ensure build stage output is clean
RUN dotnet publish "ShelfBuddy.WebApi.csproj" \
    -c $BUILD_CONFIGURATION \
    -o /app/publish

# Stage 4: Final
FROM base AS final
ENV ASPNETCORE_HTTP_PORTS=5000
ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://+:5000
EXPOSE 5000
WORKDIR /app
COPY --from=publish /app/publish .
# start the application
ENTRYPOINT ["dotnet", "ShelfBuddy.WebApi.dll"]