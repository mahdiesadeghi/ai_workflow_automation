# =============================================================================
# Stage 1: Build the Angular application
# =============================================================================
FROM node:22-alpine AS build
WORKDIR /app

# Copy package manifests first to leverage Docker layer caching
COPY package.json package-lock.json* ./

# Install dependencies
RUN npm ci --prefer-offline

# Copy the rest of the source code
COPY . .

# Build the Angular app in production mode
RUN npx ng build --configuration production

# =============================================================================
# Stage 2: Serve with Nginx
# =============================================================================
FROM nginx:alpine AS runtime

# Remove the default nginx site
RUN rm -rf /usr/share/nginx/html/*

# Copy the built Angular app.
# Angular 17 with the application builder outputs to dist/<project>/browser/
COPY --from=build /app/dist/workflow-automation/browser/ /usr/share/nginx/html/
# Fallback: older builder outputs to dist/<project>/ directly
COPY --from=build /app/dist/workflow-automation/*.html /usr/share/nginx/html/ 2>/dev/null || true

# Copy custom Nginx configuration for SPA routing and API proxy
COPY nginx.conf /etc/nginx/conf.d/default.conf

EXPOSE 80

CMD ["nginx", "-g", "daemon off;"]
