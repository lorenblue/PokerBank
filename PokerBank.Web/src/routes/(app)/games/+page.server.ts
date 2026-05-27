import { error, fail, redirect } from '@sveltejs/kit';
import { ApiError } from '$lib/api/client';
import { pokerBankApi } from '$lib/server/pokerbank';
import type { Actions, PageServerLoad } from './$types';

export const load: PageServerLoad = async ({ fetch, parent, request, url }) => {
	const { isManager } = await parent();

	const api = pokerBankApi(fetch, request.headers.get('cookie'));

	if (!isManager) {
		const games = await api.getMyGames();

		return { games, isManager };
	}

	const page = readInteger(url.searchParams.get('page'), 1);
	const pageSize = readInteger(url.searchParams.get('pageSize'), 10);

	try {
		const [gamePage, latestGamePage] = await Promise.all([
			api.listGames(page, pageSize),
			api.listGames(1, 1)
		]);
		const latestGame = latestGamePage.items[0];

		return {
			games: gamePage.items,
			gamePage,
			openGame: latestGame?.status === 'Open' ? latestGame : null,
			isManager
		};
	} catch (caught) {
		if (caught instanceof ApiError) {
			error(caught.status, caught.message);
		}

		throw caught;
	}
};

function readInteger(value: string | null, fallback: number) {
	if (value === null) {
		return fallback;
	}

	const parsed = Number(value);

	return Number.isInteger(parsed) ? parsed : fallback;
}

export const actions: Actions = {
	createGame: async ({ fetch, request }) => {
		const api = pokerBankApi(fetch, request.headers.get('cookie'));

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
