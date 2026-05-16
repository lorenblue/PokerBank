import type { components } from './schema';

type Schemas = components['schemas'];

export type Balance = Schemas['ListBalancesResponse'];
export type GameSummary = Schemas['ListGamesResponse'];
export type GameDetails = Schemas['GetGameResponse'];
export type GameResult = Schemas['ListGameResultsResponse'];
export type Player = Schemas['ListPlayersResponse'];
export type CreateGameResponse = Schemas['CreateGameResponse'];
export type CreatePlayerRequest = Schemas['CreatePlayerRequest'];
export type AddBuyInRequest = Schemas['AddBuyInRequest'];
export type AddCashOutRequest = Schemas['AddCashOutRequest'];

type ApiFetch = typeof fetch;

export class ApiError extends Error {
	constructor(
		message: string,
		readonly status: number
	) {
		super(message);
		this.name = 'ApiError';
	}
}

export function createPokerBankApi(apiFetch: ApiFetch, baseUrl: string) {
	return {
		listBalances: (playerId?: string) =>
			request<Balance[]>(apiFetch, baseUrl, '/balances', { query: { playerId } }),

		listGames: () => request<GameSummary[]>(apiFetch, baseUrl, '/games'),

		createGame: () =>
			request<CreateGameResponse>(apiFetch, baseUrl, '/games', {
				method: 'POST'
			}),

		getGame: (id: string) => request<GameDetails>(apiFetch, baseUrl, `/games/${id}`),

		deleteGame: (id: string) =>
			request<void>(apiFetch, baseUrl, `/games/${id}`, {
				method: 'DELETE'
			}),

		listGameResults: (gameId?: string, playerId?: string) =>
			request<GameResult[]>(apiFetch, baseUrl, '/game-results', {
				query: { gameId, playerId }
			}),

		addBuyIn: (gameId: string, body: AddBuyInRequest) =>
			request(apiFetch, baseUrl, `/games/${gameId}/buy-ins`, {
				method: 'POST',
				body
			}),

		addCashOut: (gameId: string, body: AddCashOutRequest) =>
			request(apiFetch, baseUrl, `/games/${gameId}/cash-outs`, {
				method: 'POST',
				body
			}),

		closeGame: (gameId: string) =>
			request(apiFetch, baseUrl, `/games/${gameId}/close`, {
				method: 'POST'
			}),

		listPlayers: () => request<Player[]>(apiFetch, baseUrl, '/players'),

		createPlayer: (body: CreatePlayerRequest) =>
			request<Player>(apiFetch, baseUrl, '/players', {
				method: 'POST',
				body
			})
	};
}

async function request<T>(
	apiFetch: ApiFetch,
	baseUrl: string,
	path: string,
	options: {
		method?: string;
		query?: Record<string, string | undefined>;
		body?: unknown;
	} = {}
): Promise<T> {
	const url = new URL(path, baseUrl);

	for (const [key, value] of Object.entries(options.query ?? {})) {
		if (value) {
			url.searchParams.set(key, value);
		}
	}

	const response = await apiFetch(url, {
		method: options.method ?? 'GET',
		headers: options.body === undefined ? undefined : { 'content-type': 'application/json' },
		body: options.body === undefined ? undefined : JSON.stringify(options.body)
	});

	if (!response.ok) {
		throw new ApiError(await readError(response), response.status);
	}

	if (response.status === 204) {
		return undefined as T;
	}

	return (await response.json()) as T;
}

async function readError(response: Response) {
	const fallback = `Request failed with status ${response.status}.`;

	try {
		const body = (await response.json()) as { error?: string };
		return body.error ?? fallback;
	} catch {
		return fallback;
	}
}
