import createClient from 'openapi-fetch';
import type { components, paths } from './schema';

type Schemas = components['schemas'];

export type Balance = Schemas['ListBalancesResponse'];
export type GameSummary = Schemas['ListGamesResponse'];
export type GameDetails = Schemas['GetGameResponse'];
export type GameResult = Schemas['ListGameResultsResponse'];
export type Player = Schemas['ListPlayersResponse'];
export type Payment = Schemas['ListPaymentsResponse'];
export type CreateGameResponse = Schemas['CreateGameResponse'];
export type CreatePlayerRequest = Schemas['CreatePlayerRequest'];
export type RenamePlayerRequest = Schemas['RenamePlayerRequest'];
export type CreatePaymentRequest = Schemas['CreatePaymentRequest'];
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
	const client = createClient<paths>({ baseUrl, fetch: apiFetch });

	return {
		listBalances: (playerId?: string) =>
			unwrap<Balance[]>(
				client.GET('/balances', {
					params: { query: { playerId } }
				})
			),

		listPayments: (playerId?: string) =>
			unwrap<Payment[]>(
				client.GET('/payments', {
					params: { query: { playerId } }
				})
			),

		listGames: () => unwrap<GameSummary[]>(client.GET('/games')),

		createGame: () => unwrap<CreateGameResponse>(client.POST('/games')),

		getGame: (id: string) =>
			unwrap<GameDetails>(
				client.GET('/games/{id}', {
					params: { path: { id } }
				})
			),

		deleteGame: (id: string) =>
			unwrap<void>(
				client.DELETE('/games/{id}', {
					params: { path: { id } }
				})
			),

		listGameResults: (gameId?: string, playerId?: string) =>
			unwrap<GameResult[]>(
				client.GET('/game-results', {
					params: { query: { gameId, playerId } }
				})
			),

		addBuyIn: (gameId: string, body: AddBuyInRequest) =>
			unwrap(
				client.POST('/games/{gameId}/buy-ins', {
					params: { path: { gameId } },
					body
				})
			),

		addCashOut: (gameId: string, body: AddCashOutRequest) =>
			unwrap(
				client.POST('/games/{gameId}/cash-outs', {
					params: { path: { gameId } },
					body
				})
			),

		deleteGameEntry: (gameId: string, entryId: string) =>
			unwrap<void>(
				client.DELETE('/games/{gameId}/entries/{entryId}', {
					params: { path: { gameId, entryId } }
				})
			),

		closeGame: (gameId: string) =>
			unwrap(
				client.POST('/games/{gameId}/close', {
					params: { path: { gameId } }
				})
			),

		listPlayers: () => unwrap<Player[]>(client.GET('/players')),

		createPlayer: (body: CreatePlayerRequest) =>
			unwrap<Player>(
				client.POST('/players', {
					body
				})
			),

		renamePlayer: (id: string, body: RenamePlayerRequest) =>
			unwrap<Player>(
				client.PUT('/players/{id}/name', {
					params: { path: { id } },
					body
				})
			),

		createPayment: (body: CreatePaymentRequest) =>
			unwrap<Payment>(
				client.POST('/payments', {
					body
				})
			),

		deletePayment: (id: string) =>
			unwrap<void>(
				client.DELETE('/payments/{id}', {
					params: { path: { id } }
				})
			)
	};
}

async function unwrap<T>(
	request: Promise<{ data?: T; error?: unknown; response: Response }>
): Promise<T> {
	const { data, error, response } = await request;

	if (error !== undefined) {
		throw new ApiError(readError(error, response), response.status);
	}

	return data as T;
}

function readError(error: unknown, response: Response) {
	const fallback = `Request failed with status ${response.status}.`;

	if (typeof error === 'object' && error !== null && 'error' in error) {
		const message = (error as { error?: unknown }).error;

		if (typeof message === 'string') {
			return message;
		}
	}

	return fallback;
}
