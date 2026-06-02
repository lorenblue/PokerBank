import { fail, redirect } from '@sveltejs/kit';
import { ApiError } from '$lib/api/client';
import { setAuthCookie } from '$lib/server/auth';
import { pokerBankApi } from '$lib/server/pokerbank';
import type { Actions, PageServerLoad } from './$types';

export const load: PageServerLoad = async ({ locals, url }) => {
	if (locals.user) {
		redirect(303, '/');
	}

	return {
		token: url.searchParams.get('token') ?? ''
	};
};

export const actions: Actions = {
	default: async ({ fetch, cookies, request }) => {
		const data = await request.formData();
		const token = data.get('token')?.toString().trim();
		const password = data.get('password')?.toString();
		const confirmPassword = data.get('confirmPassword')?.toString();

		if (!token) {
			return fail(400, { error: 'Invitation token is required.', token: '' });
		}

		if (!password || !confirmPassword) {
			return fail(400, { error: 'Password and confirmation are required.', token });
		}

		if (password !== confirmPassword) {
			return fail(400, { error: 'Passwords must match.', token });
		}

		const api = pokerBankApi(fetch);

		try {
			const result = await api.acceptPlayerInvite({ token, password });
			setAuthCookie(cookies, result.response.headers.get('set-cookie'));
		} catch (error) {
			if (error instanceof ApiError) {
				return fail(error.status, { error: error.message, token });
			}

			throw error;
		}

		redirect(303, '/');
	}
};
