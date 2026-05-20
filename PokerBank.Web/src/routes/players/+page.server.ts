import { fail } from '@sveltejs/kit';
import { ApiError } from '$lib/api/client';
import { pokerBankApi } from '$lib/server/pokerbank';
import type { Actions, PageServerLoad } from './$types';

export const load: PageServerLoad = async ({ fetch }) => {
	const api = pokerBankApi(fetch);
	const players = await api.listPlayers();

	return { players };
};

export const actions: Actions = {
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

	renamePlayer: async ({ fetch, request }) => {
		const data = await request.formData();
		const playerId = data.get('playerId')?.toString();
		const name = data.get('name')?.toString().trim();

		if (!playerId || !name) {
			return fail(400, { error: 'Player and name are required.' });
		}

		const api = pokerBankApi(fetch);

		try {
			await api.renamePlayer(playerId, { name });
			return { success: true };
		} catch (error) {
			if (error instanceof ApiError) {
				return fail(error.status, { error: error.message });
			}

			throw error;
		}
	}
};
