FROM node:22-alpine AS base

# 1. Install dependencies only when needed
FROM base AS deps
RUN apk add --no-cache libc6-compat
ARG APP_VERSION=0.0.0
ENV APP_VERSION=$APP_VERSION
WORKDIR /app

# Mandated copy lines
COPY ["HeyAlan.WebApp/package.json", "HeyAlan.WebApp/"]
COPY ["HeyAlan.WebApp/yarn.lock*", "HeyAlan.WebApp/"]

# FIX: Change directory to the app location before installing
WORKDIR /app/HeyAlan.WebApp

RUN \
  if [ -f yarn.lock ]; then yarn --frozen-lockfile; \
  else echo "Lockfile not found." && exit 1; \
  fi

# 2. Rebuild the source code only when needed
FROM base AS builder
WORKDIR /app

COPY --from=deps /app/HeyAlan.WebApp/node_modules ./HeyAlan.WebApp/node_modules
COPY . .

# FIX: Switch context to app directory for build
WORKDIR /app/HeyAlan.WebApp

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

COPY --from=builder /app/HeyAlan.WebApp/public ./public
RUN mkdir .next
RUN chown nextjs:nodejs .next

COPY --from=builder --chown=nextjs:nodejs /app/HeyAlan.WebApp/next.config.js ./next.config.js
COPY --from=builder --chown=nextjs:nodejs /app/HeyAlan.WebApp/.next/standalone ./
COPY --from=builder --chown=nextjs:nodejs /app/HeyAlan.WebApp/.next/static ./.next/static

USER nextjs

EXPOSE 5010

# NOTE: If standalone output preserves directory structure, this may need to be:
CMD ["node", "server.js"]