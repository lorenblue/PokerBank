export function isManagerRole(role: string | null | undefined) {
	return role === 'Owner' || role === 'Admin';
}
