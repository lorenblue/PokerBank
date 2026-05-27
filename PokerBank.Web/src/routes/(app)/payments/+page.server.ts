import { error, fail } from '@sveltejs/kit';
import { ApiError } from '$lib/api/client';
import { readPaymentFields } from '$lib/server/payment-form';
import { pokerBankApi } from '$lib/server/pokerbank';
import type { Actions, PageServerLoad } from './$types';

export const load: PageServerLoad = async ({ fetch, parent, request, url }) => {
	const { isManager } = await parent();

	const api = pokerBankApi(fetch, request.headers.get('cookie'));

	if (!isManager) {
		const payments = await api.getMyPayments();

		return {
			isManager,
			players: [],
			payments
		};
	}

	const selectedPlayerId = url.searchParams.get('playerId') || undefined;
	const page = readInteger(url.searchParams.get('page'), 1);
	const pageSize = readInteger(url.searchParams.get('pageSize'), 10);

	try {
		const [players, paymentPage] = await Promise.all([
			api.listPlayers(),
			api.listPayments(selectedPlayerId, page, pageSize)
		]);

		return {
			isManager,
			players,
			payments: paymentPage.items,
			paymentPage
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

			return { success: true };
		} catch (error) {
			if (error instanceof ApiError) {
				return fail(error.status, { error: error.message });
			}

			throw error;
		}
	},

	deletePayment: async ({ fetch, request }) => {
		const data = await request.formData();
		const paymentId = data.get('paymentId')?.toString();

		if (!paymentId) {
			return fail(400, { error: 'Payment is required.' });
		}

		const api = pokerBankApi(fetch, request.headers.get('cookie'));

		try {
			await api.deletePayment(paymentId);
			return { deleted: true };
		} catch (error) {
			if (error instanceof ApiError) {
				return fail(error.status, { error: error.message });
			}

			throw error;
		}
	}
};
