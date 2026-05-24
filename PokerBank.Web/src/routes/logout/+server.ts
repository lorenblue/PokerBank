import { redirect, type RequestHandler } from '@sveltejs/kit';
import { signOut } from '$lib/server/auth';

export const POST: RequestHandler = async ({ fetch, cookies, request }) => {
	await signOut(fetch, cookies, request.headers.get('cookie'));

	redirect(303, '/login');
};
