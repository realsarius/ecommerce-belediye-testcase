import {
    HubConnection,
    HubConnectionBuilder,
    HubConnectionState,
    HttpTransportType,
    LogLevel,
} from '@microsoft/signalr';

const HUB_PATH = '/hubs/wishlist';
const RECONNECT_DELAYS = [0, 2000, 5000, 10000];

function resolveHubUrl(apiBaseUrl: string): string {
    const normalized = apiBaseUrl.replace(/\/+$/, '');

    if (normalized.startsWith('http://') || normalized.startsWith('https://')) {
        if (normalized.endsWith('/api/v1')) {
            return `${normalized.slice(0, -7)}${HUB_PATH}`;
        }
        if (normalized.endsWith('/api')) {
            return `${normalized.slice(0, -4)}${HUB_PATH}`;
        }

        return `${normalized}${HUB_PATH}`;
    }

    if (normalized.endsWith('/api/v1') || normalized.endsWith('/api')) {
        return HUB_PATH;
    }

    if (!normalized) {
        return HUB_PATH;
    }

    return `${normalized}${HUB_PATH}`;
}

const apiBaseUrl = import.meta.env.VITE_API_URL || '/api/v1';
export const wishlistHubUrl = resolveHubUrl(apiBaseUrl);

export function createWishlistConnection(getToken: () => string | null): HubConnection {
    return new HubConnectionBuilder()
        .withUrl(wishlistHubUrl, {
            accessTokenFactory: () => getToken() ?? '',
            transport: HttpTransportType.WebSockets | HttpTransportType.LongPolling,
        })
        .withAutomaticReconnect(RECONNECT_DELAYS)
        .configureLogging(LogLevel.None)
        .build();
}

export async function ensureWishlistConnectionStarted(connection: HubConnection): Promise<void> {
    if (
        connection.state === HubConnectionState.Connected ||
        connection.state === HubConnectionState.Connecting ||
        connection.state === HubConnectionState.Reconnecting
    ) {
        return;
    }

    await connection.start();
}
