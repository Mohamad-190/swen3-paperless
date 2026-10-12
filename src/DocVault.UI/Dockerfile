FROM nginx:1.27-alpine

RUN rm /etc/nginx/conf.d/default.conf
COPY src/DocVault.UI/nginx.conf /etc/nginx/conf.d/default.conf
COPY src/DocVault.UI/html/ /usr/share/nginx/html/

EXPOSE 80

HEALTHCHECK --interval=10s --timeout=3s --retries=3 \
    CMD ["wget", "-q", "--spider", "http://127.0.0.1/"]
