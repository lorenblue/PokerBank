import { env } from '$env/dynamic/private';
import { createPokerBankApi } from '$lib/api/client';

const apiBaseUrl = env.POKERBANK_API_BASE_URL ?? 'http://localhost:5186';

export function pokerBankApi(apiFetch: typeof fetch, cookieHeader?: string | null) {
	if (!cookieHeader) {
		return createPokerBankApi((request) => apiFetch(request), apiBaseUrl);
	}

	return createPokerBankApi(
		(request) =>
			apiFetch(
				new Request(request, {
					headers: {
						...headersToObject(request.headers),
						cookie: cookieHeader
					}
				})
			),
		apiBaseUrl
	);
}

function headersToObject(headers: HeadersInit | undefined): Record<string, string> {
	if (!headers) {
		return {};
	}

	if (headers instanceof Headers) {
		return Object.fromEntries(headers.entries());
	}

	if (Array.isArray(headers)) {
		return Object.fromEntries(headers);
	}

	return headers;
}
