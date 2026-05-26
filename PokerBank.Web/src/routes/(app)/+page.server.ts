import { fail, redirect } from '@sveltejs/kit';
import { ApiError } from '$lib/api/client';
import { readPaymentFields } from '$lib/server/payment-form';
import { pokerBankApi } from '$lib/server/pokerbank';
import type { Actions, PageServerLoad } from './$types';

export const load: PageServerLoad = async ({ fetch, parent, request }) => {
	const { isManager } = await parent();

	const api = pokerBankApi(fetch, request.headers.get('cookie'));

	if (!isManager) {
		const [balance, games, payments] = await Promise.all([
			api.getMyBalance(),
			api.getMyGames(),
			api.getMyPayments()
		]);

		return {
			view: 'member' as const,
			balance,
			games,
			payments
		};
	}

	const [balances, games, players, payments] = await Promise.all([
		api.listBalances(),
		api.listGames(),
		api.listPlayers(),
		api.listPayments()
	]);

	return {
		view: 'manager' as const,
		balances,
		games,
		players,
		payments
	};
};

export const actions: Actions = {
	createPayment: async ({ fetch, request }) => {
		const data = await request.formData();
		const playerId = data.get('playerId')?.toString();
		const payment = readPaymentFields(data);

		if (!playerId || payment === null) {
			return fail(400, { error: 'Player, amount, payment direction, and payment method are required.' });
		}

		const api = pokerBankApi(fetch, request.headers.get('cookie'));
		const { direction, ...paymentRequest } = payment;

		try {
			if (direction === 'MadeByPlayer') {
				await api.recordPaymentMadeByPlayer(playerId, paymentRequest);
			} else {
				await api.recordPaymentReceivedByPlayer(playerId, paymentRequest);
			}

			return { paymentRecorded: true };
		} catch (error) {
			if (error instanceof ApiError) {
				return fail(error.status, { error: error.message });
			}

			throw error;
		}
	},

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
	},

	sendBalanceUpdates: async ({ fetch, request }) => {
		const api = pokerBankApi(fetch, request.headers.get('cookie'));

		try {
			const result = await api.sendBalanceUpdates();

			return { balanceUpdates: result };
		} catch (error) {
			if (error instanceof ApiError) {
				return fail(error.status, { error: error.message });
			}

			throw error;
		}
	}
};
