import createClient from 'openapi-fetch';
import type { components, paths } from './schema';

type Schemas = components['schemas'];

export type Balance = Schemas['BalanceResponse'];
export type GameSummary = Schemas['ListGamesResponse'];
export type GameDetails = Schemas['GetGameResponse'];
export type GameResult = Schemas['GameResultResponse'];
export type MyGame = Schemas['GetMyGamesResponse'];
export type Player = Schemas['PlayerResponse'];
export type PlayerDetails = Schemas['PlayerResponse'];
export type Payment = Schemas['PaymentResponse'];
export type CreateGameResponse = Schemas['CreateGameResponse'];
export type CreatePlayerRequest = Schemas['CreatePlayerRequest'];
export type UpdatePlayerRequest = Schemas['UpdatePlayerRequest'];
export type RecordPaymentRequest = Schemas['RecordPaymentRequest'];
export type AddBuyInRequest = Schemas['AddBuyInRequest'];
export type AddCashOutRequest = Schemas['AddCashOutRequest'];
export type SendBalanceUpdatesResponse = Schemas['SendBalanceUpdatesResponse'];

type ApiFetch = (request: Request) => Promise<Response>;

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

		getMyBalance: () => unwrap<Balance>(client.GET('/me/balance')),

		sendBalanceUpdates: () =>
			unwrap<SendBalanceUpdatesResponse>(
				client.POST('/balances/updates/send')
			),

		listPayments: (playerId?: string) =>
			unwrap<Payment[]>(
				client.GET('/payments', {
					params: { query: { playerId } }
				})
			),

		getMyPayments: () => unwrap<Payment[]>(client.GET('/me/payments')),

		listGames: () => unwrap<GameSummary[]>(client.GET('/games')),

		getMyGames: () => unwrap<MyGame[]>(client.GET('/me/games')),

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

		getPlayer: (id: string) =>
			unwrap<PlayerDetails>(
				client.GET('/players/{id}', {
					params: { path: { id } }
				})
			),

		createPlayer: (body: CreatePlayerRequest) =>
			unwrap<Player>(
				client.POST('/players', {
					body
				})
			),

		updatePlayer: (id: string, body: UpdatePlayerRequest) =>
			unwrap<Player>(
				client.PUT('/players/{id}', {
					params: { path: { id } },
					body
				})
			),

		recordPaymentMadeByPlayer: (playerId: string, body: RecordPaymentRequest) =>
			unwrap<Payment>(
				client.POST('/players/{playerId}/payments/made', {
					params: { path: { playerId } },
					body
				})
			),

		recordPaymentReceivedByPlayer: (playerId: string, body: RecordPaymentRequest) =>
			unwrap<Payment>(
				client.POST('/players/{playerId}/payments/received', {
					params: { path: { playerId } },
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

	if (!response.ok || error !== undefined) {
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
