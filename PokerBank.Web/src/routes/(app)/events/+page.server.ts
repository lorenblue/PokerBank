import { error, fail, redirect } from '@sveltejs/kit';
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
			const createdEvent = await api.createEvent(event);

			redirect(303, `/events/${createdEvent.id}`);
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
