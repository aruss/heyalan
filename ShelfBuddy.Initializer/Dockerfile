# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build
ARG APP_VERSION=0.0.0
ENV APP_VERSION=$APP_VERSION
WORKDIR /src

# 1. Install EF Tool
ENV PATH="$PATH:/root/.dotnet/tools"

# 2. Restore (Relative paths are preserved in temp folder)
COPY ["ShelfBuddy/ShelfBuddy.csproj", "ShelfBuddy/"]
COPY ["ShelfBuddy.Data/ShelfBuddy.Data.csproj", "ShelfBuddy.Data/"]
COPY ["ShelfBuddy.Initializer/ShelfBuddy.Initializer.csproj", "ShelfBuddy.Initializer/"]

RUN dotnet restore "ShelfBuddy.Initializer/ShelfBuddy.Initializer.csproj" -r linux-musl-x64

# 3. Copy Source (Now guaranteed clean)
COPY . .

# 4. Generate Bundle
WORKDIR "/src/ShelfBuddy.Initializer"
RUN dotnet publish "ShelfBuddy.Initializer.csproj" \
    -c Release \
    -r linux-musl-x64 \
    --no-restore \
    --self-contained true \
    /p:PublishSingleFile=true \
    -o /app/publish

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/runtime-deps:10.0-alpine AS final
WORKDIR /app

# Add ICU for PostgreSQL globalization
RUN apk add --no-cache icu-libs
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false

# Security
RUN adduser -D -u 1000 appuser
USER appuser

COPY --from=build --chown=appuser:appuser /app/publish .

ENTRYPOINT ["./ShelfBuddy.Initializer"]