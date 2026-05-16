import { fail, redirect } from '@sveltejs/kit';
import { ApiError } from '$lib/api/client';
import { pokerBankApi } from '$lib/server/pokerbank';
import type { Actions, PageServerLoad } from './$types';

export const load: PageServerLoad = async ({ fetch }) => {
	const api = pokerBankApi(fetch);

	const [balances, games] = await Promise.all([api.listBalances(), api.listGames()]);

	return {
		balances,
		games
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
	}
};
