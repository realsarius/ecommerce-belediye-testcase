const DEFAULT_API_BASE_URL = '/api/v1';

function isLocalNetworkHost(hostname: string) {
  return (
    hostname === 'localhost' ||
    hostname === '127.0.0.1' ||
    hostname === '0.0.0.0' ||
    hostname === '::1' ||
    /^10\./.test(hostname) ||
    /^192\.168\./.test(hostname) ||
    /^172\.(1[6-9]|2\d|3[0-1])\./.test(hostname)
  );
}

function isAbsoluteHttpUrl(value: string) {
  return value.startsWith('http://') || value.startsWith('https://');
}

export function getRuntimeApiBaseUrl() {
  const configuredBaseUrl = import.meta.env.VITE_API_URL?.trim();

  if (!configuredBaseUrl) {
    return DEFAULT_API_BASE_URL;
  }

  if (typeof window === 'undefined' || !isAbsoluteHttpUrl(configuredBaseUrl)) {
    return configuredBaseUrl;
  }

  const configuredUrl = new URL(configuredBaseUrl);
  const currentHostname = window.location.hostname;

  if (
    isLocalNetworkHost(configuredUrl.hostname) &&
    !isLocalNetworkHost(currentHostname) &&
    configuredUrl.hostname !== currentHostname
  ) {
    return DEFAULT_API_BASE_URL;
  }

  return configuredBaseUrl;
}

export function buildRuntimeApiUrl(path: string) {
  const baseUrl = getRuntimeApiBaseUrl().replace(/\/+$/, '');
  const normalizedPath = path.startsWith('/') ? path : `/${path}`;

  return `${baseUrl}${normalizedPath}`;
}
