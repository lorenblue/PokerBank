import { redirect, type Handle } from '@sveltejs/kit';
import { getCurrentUser } from '$lib/server/auth';

const publicPaths = new Set(['/login', '/logout']);

export const handle: Handle = async ({ event, resolve }) => {
	if (event.route.id === null) {
		return resolve(event);
	}

	const user = await getCurrentUser(event.fetch, event.request.headers.get('cookie'));
	event.locals.user = user;

	if (!user && !publicPaths.has(event.url.pathname)) {
		redirect(303, '/login');
	}

	if (user && event.url.pathname === '/login') {
		redirect(303, '/');
	}

	return resolve(event);
};
