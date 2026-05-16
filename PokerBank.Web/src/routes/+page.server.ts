import { fail, redirect } from '@sveltejs/kit';
import { ApiError } from '$lib/api/client';
import { pokerBankApi } from '$lib/server/pokerbank';
import type { Actions, PageServerLoad } from './$types';

export const load: PageServerLoad = async ({ fetch }) => {
	const api = pokerBankApi(fetch);

	const [balances, games, players] = await Promise.all([
		api.listBalances(),
		api.listGames(),
		api.listPlayers()
	]);

	return {
		balances,
		games,
		players
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
	}
};
