// Empty (docker-compose, nginx proxies /api/) or the unsubstituted `ng serve`
// placeholder both resolve to relative URLs. Only set to an absolute origin
// when the SPA is hosted separately from the backend (e.g. Azure Static Web Apps).
export const API_BASE = (() => {
  const raw = (window as any).__env?.apiBaseUrl;
  if (!raw || raw.startsWith('${')) return '';
  return raw.replace(/\/+$/, '');
})();
