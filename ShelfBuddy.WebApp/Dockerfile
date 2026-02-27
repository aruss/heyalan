FROM node:22-alpine AS base

# 1. Install dependencies only when needed
FROM base AS deps
RUN apk add --no-cache libc6-compat
ARG APP_VERSION=0.0.0
ENV APP_VERSION=$APP_VERSION
WORKDIR /app

# Mandated copy lines
COPY ["ShelfBuddy.WebApp/package.json", "ShelfBuddy.WebApp/"]
COPY ["ShelfBuddy.WebApp/yarn.lock*", "ShelfBuddy.WebApp/"]

# FIX: Change directory to the app location before installing
WORKDIR /app/ShelfBuddy.WebApp

RUN \
  if [ -f yarn.lock ]; then yarn --frozen-lockfile; \
  else echo "Lockfile not found." && exit 1; \
  fi

# 2. Rebuild the source code only when needed
FROM base AS builder
WORKDIR /app

COPY --from=deps /app/ShelfBuddy.WebApp/node_modules ./ShelfBuddy.WebApp/node_modules
COPY . .

# FIX: Switch context to app directory for build
WORKDIR /app/ShelfBuddy.WebApp

# Disable telemetry during build
ENV NEXT_TELEMETRY_DISABLED=1

RUN \
  if [ -f yarn.lock ]; then yarn run build; \
  else echo "Lockfile not found." && exit 1; \
  fi

# 3. Production image, copy all the files and run next
FROM base AS runner
WORKDIR /app

ENV NODE_ENV=production
ENV NEXT_TELEMETRY_DISABLED=1
ENV PORT=5010
ENV HOSTNAME="0.0.0.0"

RUN addgroup --system --gid 1001 nodejs
RUN adduser --system --uid 1001 nextjs

COPY --from=builder /app/ShelfBuddy.WebApp/public ./public
RUN mkdir .next
RUN chown nextjs:nodejs .next

COPY --from=builder --chown=nextjs:nodejs /app/ShelfBuddy.WebApp/next.config.js ./next.config.js
COPY --from=builder --chown=nextjs:nodejs /app/ShelfBuddy.WebApp/.next/standalone ./
COPY --from=builder --chown=nextjs:nodejs /app/ShelfBuddy.WebApp/.next/static ./.next/static

USER nextjs

EXPOSE 5010

# NOTE: If standalone output preserves directory structure, this may need to be:
CMD ["node", "server.js"]