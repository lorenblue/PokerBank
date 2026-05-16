import { fail, redirect } from '@sveltejs/kit';
import { ApiError } from '$lib/api/client';
import { pokerBankApi } from '$lib/server/pokerbank';
import type { Actions, PageServerLoad } from './$types';

export const load: PageServerLoad = async ({ fetch, params }) => {
	const api = pokerBankApi(fetch);

	const [game, players, results] = await Promise.all([
		api.getGame(params.id),
		api.listPlayers(),
		api.listGameResults(params.id)
	]);

	return {
		game,
		players: players.filter((player) => player.isActive),
		results
	};
};

export const actions: Actions = {
	addBuyIn: async ({ fetch, params, request }) => {
		const data = await request.formData();

		return addEntry(fetch, params.id, 'buyIn', data);
	},

	addCashOut: async ({ fetch, params, request }) => {
		const data = await request.formData();

		return addEntry(fetch, params.id, 'cashOut', data);
	},

	closeGame: async ({ fetch, params }) => {
		const api = pokerBankApi(fetch);

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

	deleteGame: async ({ fetch, params }) => {
		const api = pokerBankApi(fetch);

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
	gameId: string,
	type: 'buyIn' | 'cashOut',
	data: FormData
) {
	const playerId = data.get('playerId')?.toString();
	const amount = Number(data.get('amount'));

	if (!playerId || !Number.isFinite(amount)) {
		return fail(400, { error: 'Player and amount are required.' });
	}

	const api = pokerBankApi(apiFetch);

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
