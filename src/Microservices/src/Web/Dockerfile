FROM node:22-alpine AS build
WORKDIR /app

RUN corepack enable

COPY src/Web/package.json src/Web/pnpm-lock.yaml src/Web/pnpm-workspace.yaml ./
RUN pnpm install --frozen-lockfile

COPY src/Web/ .
RUN pnpm build

FROM nginx:alpine AS runtime
COPY --from=build /app/dist /usr/share/nginx/html
COPY infra/nginx/nginx.conf /etc/nginx/conf.d/default.conf

EXPOSE 80
