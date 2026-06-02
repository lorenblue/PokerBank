import { env } from '$env/dynamic/private';
import type { Cookies } from '@sveltejs/kit';

const apiBaseUrl = env.POKERBANK_API_BASE_URL ?? 'http://localhost:5186';
const authCookieName = 'PokerBank.Auth';

export type CurrentUser = {
	id: string;
	email: string;
	pokerGroupId: string;
	groupRole: string | null;
};

export async function getCurrentUser(apiFetch: typeof fetch, cookieHeader?: string | null) {
	if (!cookieHeader) {
		return null;
	}

	const response = await apiFetch(`${apiBaseUrl}/auth/me`, {
		headers: {
			cookie: cookieHeader
		}
	});

	if (response.status === 401 || response.status === 403) {
		return null;
	}

	if (!response.ok) {
		throw new Error(`Failed to load current user. Status: ${response.status}`);
	}

	return (await response.json()) as CurrentUser;
}

export async function signIn(apiFetch: typeof fetch, cookies: Cookies, email: string, password: string) {
	const response = await apiFetch(`${apiBaseUrl}/auth/login`, {
		method: 'POST',
		headers: {
			'content-type': 'application/json'
		},
		body: JSON.stringify({ email, password })
	});

	if (!response.ok) {
		return false;
	}

	setAuthCookie(cookies, response.headers.get('set-cookie'));

	return true;
}

export function setAuthCookie(cookies: Cookies, setCookie: string | null) {
	const authCookie = readCookieValue(setCookie, authCookieName);

	if (!authCookie) {
		throw new Error('API did not return an auth cookie.');
	}

	cookies.set(authCookieName, authCookie, {
		path: '/',
		httpOnly: true,
		sameSite: 'lax',
		secure: false
	});
}

export async function signOut(apiFetch: typeof fetch, cookies: Cookies, cookieHeader?: string | null) {
	await apiFetch(`${apiBaseUrl}/auth/logout`, {
		method: 'POST',
		headers: cookieHeader
			? {
					cookie: cookieHeader
				}
			: undefined
	});

	cookies.delete(authCookieName, { path: '/' });
}

function readCookieValue(setCookieHeader: string | null, name: string) {
	if (!setCookieHeader) {
		return null;
	}

	const cookie = setCookieHeader
		.split(/,(?=\s*[^;,]+=[^;,]+)/)
		.find((value) => value.trimStart().startsWith(`${name}=`));

	if (!cookie) {
		return null;
	}

	const [nameAndValue] = cookie.split(';', 1);
	const separatorIndex = nameAndValue.indexOf('=');

	return separatorIndex === -1 ? null : nameAndValue.slice(separatorIndex + 1);
}
