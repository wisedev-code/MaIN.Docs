#!/bin/sh
set -e
envsubst '${TURNSTILE_SITE_KEY} ${API_BASE_URL}' < /usr/share/nginx/html/env.js > /tmp/env.js
mv /tmp/env.js /usr/share/nginx/html/env.js
exec nginx -g 'daemon off;'
