import { env } from '$env/dynamic/private';
import { createPokerBankApi } from '$lib/api/client';

const apiBaseUrl = env.POKERBANK_API_BASE_URL ?? 'http://localhost:5186';

export function pokerBankApi(apiFetch: typeof fetch) {
	return createPokerBankApi(apiFetch, apiBaseUrl);
}
