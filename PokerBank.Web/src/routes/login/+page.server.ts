import { fail, redirect } from '@sveltejs/kit';
import { signIn } from '$lib/server/auth';
import type { Actions, PageServerLoad } from './$types';

export const load: PageServerLoad = async ({ locals }) => {
	if (locals.user) {
		redirect(303, '/');
	}
};

export const actions: Actions = {
	default: async ({ fetch, cookies, request }) => {
		const data = await request.formData();
		const email = data.get('email')?.toString().trim();
		const password = data.get('password')?.toString();

		if (!email || !password) {
			return fail(400, { error: 'Email and password are required.', email });
		}

		const signedIn = await signIn(fetch, cookies, email, password);

		if (!signedIn) {
			return fail(401, { error: 'Email or password is incorrect.', email });
		}

		redirect(303, '/');
	}
};
