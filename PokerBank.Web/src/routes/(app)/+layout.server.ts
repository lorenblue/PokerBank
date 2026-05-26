import { redirect } from '@sveltejs/kit';
import { isManagerRole } from '$lib/authz';
import type { LayoutServerLoad } from './$types';

export const load: LayoutServerLoad = async ({ locals }) => {
	const { user } = locals;

	if (!user) {
		redirect(303, '/login');
	}

	return {
		user,
		isManager: isManagerRole(user.groupRole)
	};
};
