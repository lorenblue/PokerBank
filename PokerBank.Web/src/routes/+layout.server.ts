import { getCurrentUser } from '$lib/server/auth';
import type { LayoutServerLoad } from './$types';

export const load: LayoutServerLoad = async ({ fetch, request }) => {
	const user = await getCurrentUser(fetch, request.headers.get('cookie'));

	return { user };
};
