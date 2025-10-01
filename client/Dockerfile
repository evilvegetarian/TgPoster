FROM node:18-alpine AS builder

WORKDIR /app

COPY package*.json ./
RUN npm install

COPY . .
RUN npm run build

FROM nginx:stable-alpine

COPY --from=builder /app/dist /usr/share/nginx/html

RUN rm /etc/nginx/conf.d/default.conf

COPY nginx.conf /etc/nginx/conf.d/default.conf

RUN addgroup --system --gid 1654 appgroup && \
    adduser --system --uid 1654 --ingroup appgroup appuser

RUN chown -R appuser:appgroup /var/cache/nginx /tmp && \
    chmod -R 755 /var/cache/nginx /tmp

RUN chown -R appuser:appgroup /usr/share/nginx/html

USER appuser

EXPOSE 80

CMD ["nginx", "-g", "daemon off;"]