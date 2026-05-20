import { fail } from '@sveltejs/kit';
import { ApiError } from '$lib/api/client';
import { pokerBankApi } from '$lib/server/pokerbank';
import type { Actions, PageServerLoad } from './$types';

export const load: PageServerLoad = async ({ fetch }) => {
	const api = pokerBankApi(fetch);

	const [players, payments] = await Promise.all([api.listPlayers(), api.listPayments()]);

	return {
		players,
		payments
	};
};

export const actions: Actions = {
	createPayment: async ({ fetch, request }) => {
		const data = await request.formData();
		const playerId = data.get('playerId')?.toString();
		const amount = Number(data.get('amount'));
		const type = data.get('type')?.toString();
		const method = data.get('method')?.toString();

		if (!playerId || !Number.isFinite(amount) || !type || !method) {
			return fail(400, { error: 'Player, amount, payment direction, and payment method are required.' });
		}

		const api = pokerBankApi(fetch);

		try {
			await api.createPayment({ playerId, amount, type, method });
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

		const api = pokerBankApi(fetch);

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
