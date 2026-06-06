#!/bin/sh
set -e
envsubst '${API_KEY}' < /usr/share/nginx/html/env.js > /tmp/env.js
mv /tmp/env.js /usr/share/nginx/html/env.js
exec nginx -g 'daemon off;'
