import { error, fail, redirect } from '@sveltejs/kit';
import { ApiError } from '$lib/api/client';
import { pokerBankApi } from '$lib/server/pokerbank';
import type { Actions, PageServerLoad } from './$types';

export const load: PageServerLoad = async ({ fetch, params, parent, request }) => {
	const { isManager } = await parent();
	const api = pokerBankApi(fetch, request.headers.get('cookie'));

	try {
		const event = await api.getEvent(params.id);

		return {
			event,
			isManager
		};
	} catch (caught) {
		if (caught instanceof ApiError && (caught.status === 403 || caught.status === 404)) {
			error(caught.status, caught.message);
		}

		throw caught;
	}
};

export const actions: Actions = {
	cancelEvent: async ({ fetch, params, request }) => {
		const api = pokerBankApi(fetch, request.headers.get('cookie'));

		try {
			await api.cancelEvent(params.id);

			return { cancelled: true };
		} catch (caught) {
			if (caught instanceof ApiError) {
				return fail(caught.status, { error: caught.message });
			}

			throw caught;
		}
	},

	startGame: async ({ fetch, params, request }) => {
		const api = pokerBankApi(fetch, request.headers.get('cookie'));

		try {
			const game = await api.startEventGame(params.id);

			redirect(303, `/games/${game.id}`);
		} catch (caught) {
			if (caught instanceof ApiError) {
				return fail(caught.status, { error: caught.message });
			}

			throw caught;
		}
	},

	setRsvp: async ({ fetch, params, request }) => {
		const data = await request.formData();
		const status = readRsvpStatus(data);

		if (status === null) {
			return fail(400, { error: 'RSVP status is required.' });
		}

		const api = pokerBankApi(fetch, request.headers.get('cookie'));

		try {
			await api.setMyEventRsvp(params.id, { status });

			return { rsvpStatus: status };
		} catch (caught) {
			if (caught instanceof ApiError) {
				return fail(caught.status, { error: caught.message });
			}

			throw caught;
		}
	}
};

function readRsvpStatus(data: FormData) {
	const status = data.get('status')?.toString();

	if (status === 'Going' || status === 'Maybe' || status === 'NotGoing') {
		return status;
	}

	return null;
}
