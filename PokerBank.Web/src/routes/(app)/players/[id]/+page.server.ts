import { error, fail } from '@sveltejs/kit';
import { ApiError } from '$lib/api/client';
import { readPaymentFields } from '$lib/server/payment-form';
import { pokerBankApi } from '$lib/server/pokerbank';
import type { Actions, PageServerLoad } from './$types';

export const load: PageServerLoad = async ({ fetch, params, parent, request }) => {
	const { isManager } = await parent();

	if (!isManager) {
		error(404, 'Player was not found.');
	}

	const api = pokerBankApi(fetch, request.headers.get('cookie'));

	try {
		const [player, balances, gameResults, paymentPage] = await Promise.all([
			api.getPlayer(params.id),
			api.listBalances(params.id),
			api.listGameResults(undefined, params.id),
			api.listPayments(params.id)
		]);

		return {
			player,
			balance: balances[0],
			gameResults,
			payments: paymentPage.items
		};
	} catch (caught) {
		if (caught instanceof ApiError && caught.status === 404) {
			error(404, caught.message);
		}

		throw caught;
	}
};

export const actions: Actions = {
	createPayment: async ({ fetch, params, request }) => {
		const data = await request.formData();
		const payment = readPaymentFields(data);

		if (payment === null) {
			return fail(400, { error: 'Amount, payment direction, and payment method are required.' });
		}

		const api = pokerBankApi(fetch, request.headers.get('cookie'));
		const { direction, ...paymentRequest } = payment;

		try {
			if (direction === 'MadeByPlayer') {
				await api.recordPaymentMadeByPlayer(params.id, paymentRequest);
			} else {
				await api.recordPaymentReceivedByPlayer(params.id, paymentRequest);
			}

			return { success: true };
		} catch (caught) {
			if (caught instanceof ApiError) {
				return fail(caught.status, { error: caught.message });
			}

			throw caught;
		}
	}
};
