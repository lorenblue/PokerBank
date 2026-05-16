import { fail, redirect } from '@sveltejs/kit';
import { ApiError } from '$lib/api/client';
import { pokerBankApi } from '$lib/server/pokerbank';
import type { Actions, PageServerLoad } from './$types';

export const load: PageServerLoad = async ({ fetch }) => {
	const api = pokerBankApi(fetch);

	const [balances, games, players, payments] = await Promise.all([
		api.listBalances(),
		api.listGames(),
		api.listPlayers(),
		api.listPayments()
	]);

	return {
		balances,
		games,
		players,
		payments
	};
};

export const actions: Actions = {
	createGame: async ({ fetch }) => {
		const api = pokerBankApi(fetch);

		try {
			const game = await api.createGame();
			redirect(303, `/games/${game.id}`);
		} catch (error) {
			if (error instanceof ApiError) {
				return fail(error.status, { error: error.message });
			}

			throw error;
		}
	},

	createPlayer: async ({ fetch, request }) => {
		const data = await request.formData();
		const name = data.get('name')?.toString().trim();

		if (!name) {
			return fail(400, { error: 'Player name is required.' });
		}

		const api = pokerBankApi(fetch);

		try {
			await api.createPlayer({ name });
			return { success: true };
		} catch (error) {
			if (error instanceof ApiError) {
				return fail(error.status, { error: error.message });
			}

			throw error;
		}
	},

	createPayment: async ({ fetch, request }) => {
		const data = await request.formData();
		const playerId = data.get('playerId')?.toString();
		const amount = Number(data.get('amount'));
		const type = data.get('type')?.toString();

		if (!playerId || !Number.isFinite(amount) || !type) {
			return fail(400, { error: 'Player, amount, and payment direction are required.' });
		}

		const api = pokerBankApi(fetch);

		try {
			await api.createPayment({ playerId, amount, type });
			return { success: true };
		} catch (error) {
			if (error instanceof ApiError) {
				return fail(error.status, { error: error.message });
			}

			throw error;
		}
	}
};
