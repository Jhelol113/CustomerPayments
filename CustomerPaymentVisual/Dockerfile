# Etapa de construcción
FROM node:20-alpine AS build
WORKDIR /app

# Instalar dependencias
COPY package.json package-lock.json* ./
RUN npm install

# Copiar código fuente y compilar
COPY . .
RUN npm run build

# Etapa de producción (usando Nginx)
FROM nginx:alpine
# Copiar los archivos estáticos compilados desde la etapa de build
COPY --from=build /app/dist /usr/share/nginx/html

# Copiar configuración personalizada de Nginx para SPA (Single Page Application)
# Esto redirige todas las rutas al index.html para que React Router funcione
RUN echo 'server { \
    listen 80; \
    location / { \
        root /usr/share/nginx/html; \
        index index.html index.htm; \
        try_files $uri $uri/ /index.html; \
    } \
}' > /etc/nginx/conf.d/default.conf

EXPOSE 80
CMD ["nginx", "-g", "daemon off;"]
