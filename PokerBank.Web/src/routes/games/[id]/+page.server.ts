import { fail, redirect } from '@sveltejs/kit';
import { ApiError } from '$lib/api/client';
import { pokerBankApi } from '$lib/server/pokerbank';
import type { Actions, PageServerLoad } from './$types';

export const load: PageServerLoad = async ({ fetch, params, request }) => {
	const api = pokerBankApi(fetch, request.headers.get('cookie'));

	const [game, players] = await Promise.all([
		api.getGame(params.id),
		api.listPlayers()
	]);

	return {
		game,
		players: players.filter((player) => player.isActive)
	};
};

export const actions: Actions = {
	addBuyIn: async ({ fetch, params, request }) => {
		const data = await request.formData();

		return addEntry(fetch, request.headers.get('cookie'), params.id, 'buyIn', data);
	},

	addCashOut: async ({ fetch, params, request }) => {
		const data = await request.formData();

		return addEntry(fetch, request.headers.get('cookie'), params.id, 'cashOut', data);
	},

	closeGame: async ({ fetch, params, request }) => {
		const api = pokerBankApi(fetch, request.headers.get('cookie'));

		try {
			await api.closeGame(params.id);
			return { success: true };
		} catch (error) {
			if (error instanceof ApiError) {
				return fail(error.status, { error: error.message });
			}

			throw error;
		}
	},

	deleteEntry: async ({ fetch, params, request }) => {
		const data = await request.formData();
		const entryId = data.get('entryId')?.toString();

		if (!entryId) {
			return fail(400, { error: 'Entry is required.' });
		}

		const api = pokerBankApi(fetch, request.headers.get('cookie'));

		try {
			await api.deleteGameEntry(params.id, entryId);
			return { success: true };
		} catch (error) {
			if (error instanceof ApiError) {
				return fail(error.status, { error: error.message });
			}

			throw error;
		}
	},

	deleteGame: async ({ fetch, params, request }) => {
		const api = pokerBankApi(fetch, request.headers.get('cookie'));

		try {
			await api.deleteGame(params.id);
			redirect(303, '/');
		} catch (error) {
			if (error instanceof ApiError) {
				return fail(error.status, { error: error.message });
			}

			throw error;
		}
	}
};

async function addEntry(
	apiFetch: typeof fetch,
	cookieHeader: string | null,
	gameId: string,
	type: 'buyIn' | 'cashOut',
	data: FormData
) {
	const playerId = data.get('playerId')?.toString();
	const amount = Number(data.get('amount'));

	if (!playerId || !Number.isFinite(amount)) {
		return fail(400, { error: 'Player and amount are required.' });
	}

	const api = pokerBankApi(apiFetch, cookieHeader);

	try {
		if (type === 'buyIn') {
			await api.addBuyIn(gameId, { playerId, amount });
		} else {
			await api.addCashOut(gameId, { playerId, amount });
		}

		return { success: true };
	} catch (error) {
		if (error instanceof ApiError) {
			return fail(error.status, { error: error.message });
		}

		throw error;
	}
}
