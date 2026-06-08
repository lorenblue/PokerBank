import { error, fail } from '@sveltejs/kit';
import { ApiError } from '$lib/api/client';
import { pokerBankApi } from '$lib/server/pokerbank';
import type { Actions, PageServerLoad } from './$types';

export const load: PageServerLoad = async ({ fetch, parent, request }) => {
	const { isManager } = await parent();
	const api = pokerBankApi(fetch, request.headers.get('cookie'));

	try {
		const events = await api.listEvents();

		return {
			events,
			isManager
		};
	} catch (caught) {
		if (caught instanceof ApiError) {
			error(caught.status, caught.message);
		}

		throw caught;
	}
};

export const actions: Actions = {
	createEvent: async ({ fetch, request }) => {
		const data = await request.formData();
		const event = readEventFields(data);

		if (event === null) {
			return fail(400, { error: 'Title and scheduled time are required.' });
		}

		const api = pokerBankApi(fetch, request.headers.get('cookie'));

		try {
			await api.createEvent(event);

			return { created: true };
		} catch (caught) {
			if (caught instanceof ApiError) {
				return fail(caught.status, { error: caught.message });
			}

			throw caught;
		}
	},

	cancelEvent: async ({ fetch, request }) => {
		const data = await request.formData();
		const eventId = data.get('eventId')?.toString();

		if (!eventId) {
			return fail(400, { error: 'Event is required.' });
		}

		const api = pokerBankApi(fetch, request.headers.get('cookie'));

		try {
			await api.cancelEvent(eventId);

			return { cancelled: true };
		} catch (caught) {
			if (caught instanceof ApiError) {
				return fail(caught.status, { error: caught.message });
			}

			throw caught;
		}
	},

	setRsvp: async ({ fetch, request }) => {
		const data = await request.formData();
		const eventId = data.get('eventId')?.toString();
		const status = readRsvpStatus(data);

		if (!eventId || status === null) {
			return fail(400, { error: 'Event and RSVP status are required.' });
		}

		const api = pokerBankApi(fetch, request.headers.get('cookie'));

		try {
			await api.setMyEventRsvp(eventId, { status });

			return { rsvpStatus: status };
		} catch (caught) {
			if (caught instanceof ApiError) {
				return fail(caught.status, { error: caught.message });
			}

			throw caught;
		}
	}
};

function readEventFields(data: FormData) {
	const title = data.get('title')?.toString();
	const scheduledAtUtc = data.get('scheduledAtUtc')?.toString();

	if (!title || !scheduledAtUtc) {
		return null;
	}

	const parsedDate = new Date(scheduledAtUtc);

	if (Number.isNaN(parsedDate.getTime())) {
		return null;
	}

	return {
		title,
		scheduledAtUtc: parsedDate.toISOString()
	};
}

function readRsvpStatus(data: FormData) {
	const status = data.get('status')?.toString();

	if (status === 'Going' || status === 'Maybe' || status === 'NotGoing') {
		return status;
	}

	return null;
}
