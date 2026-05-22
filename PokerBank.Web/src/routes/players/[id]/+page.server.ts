import { error } from '@sveltejs/kit';
import { ApiError } from '$lib/api/client';
import { pokerBankApi } from '$lib/server/pokerbank';
import type { PageServerLoad } from './$types';

export const load: PageServerLoad = async ({ fetch, params }) => {
	const api = pokerBankApi(fetch);

	try {
		const [player, balances, gameResults, payments] = await Promise.all([
			api.getPlayer(params.id),
			api.listBalances(params.id),
			api.listGameResults(undefined, params.id),
			api.listPayments(params.id)
		]);

		return {
			player,
			balance: balances[0],
			gameResults,
			payments
		};
	} catch (caught) {
		if (caught instanceof ApiError && caught.status === 404) {
			error(404, caught.message);
		}

		throw caught;
	}
};
