export function formatDateTime(value: string) {
	const date = new Date(value);

	return new Intl.DateTimeFormat(undefined, {
		...(isSameLocalYear(date, new Date()) ? {} : { year: 'numeric' as const }),
		month: 'short',
		day: 'numeric',
		hour: 'numeric',
		minute: '2-digit'
	}).format(date);
}

export function formatGameEntryDateTime(value: string, gameValue: string) {
	const date = new Date(value);
	const gameDate = new Date(gameValue);

	if (isSameLocalDate(date, gameDate)) {
		return formatTime(date);
	}

	return new Intl.DateTimeFormat(undefined, {
		...(isSameLocalYear(date, gameDate) ? {} : { year: 'numeric' as const }),
		month: 'short',
		day: 'numeric',
		hour: 'numeric',
		minute: '2-digit'
	}).format(date);
}

function formatTime(date: Date) {
	return new Intl.DateTimeFormat(undefined, {
		hour: 'numeric',
		minute: '2-digit'
	}).format(date);
}

function isSameLocalDate(left: Date, right: Date) {
	return (
		left.getFullYear() === right.getFullYear() &&
		left.getMonth() === right.getMonth() &&
		left.getDate() === right.getDate()
	);
}

function isSameLocalYear(left: Date, right: Date) {
	return left.getFullYear() === right.getFullYear();
}
