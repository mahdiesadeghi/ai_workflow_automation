FROM postgres:16-alpine

# Copy init scripts and migrations, stripping any Windows CRLF line endings
# so the shell scripts work correctly inside the Linux container.
COPY init/   /docker-entrypoint-initdb.d-raw/
COPY migrations/ /docker-entrypoint-initdb.d-raw/

RUN for f in /docker-entrypoint-initdb.d-raw/*; do \
      sed 's/\r$//' "$f" > "/docker-entrypoint-initdb.d/$(basename "$f")"; \
      chmod +x "/docker-entrypoint-initdb.d/$(basename "$f")"; \
    done && rm -rf /docker-entrypoint-initdb.d-raw
