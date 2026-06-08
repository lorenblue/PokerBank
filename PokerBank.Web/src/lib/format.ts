const appLocale = 'en-CA';
const appTimeZone = 'America/Vancouver';

export function formatDateTime(value: string) {
	const date = new Date(value);

	return new Intl.DateTimeFormat(appLocale, {
		timeZone: appTimeZone,
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

	return new Intl.DateTimeFormat(appLocale, {
		timeZone: appTimeZone,
		...(isSameLocalYear(date, gameDate) ? {} : { year: 'numeric' as const }),
		month: 'short',
		day: 'numeric',
		hour: 'numeric',
		minute: '2-digit'
	}).format(date);
}

function formatTime(date: Date) {
	return new Intl.DateTimeFormat(appLocale, {
		timeZone: appTimeZone,
		hour: 'numeric',
		minute: '2-digit'
	}).format(date);
}

function isSameLocalDate(left: Date, right: Date) {
	return (
		getDatePart(left, 'year') === getDatePart(right, 'year') &&
		getDatePart(left, 'month') === getDatePart(right, 'month') &&
		getDatePart(left, 'day') === getDatePart(right, 'day')
	);
}

function isSameLocalYear(left: Date, right: Date) {
	return getDatePart(left, 'year') === getDatePart(right, 'year');
}

function getDatePart(date: Date, part: 'year' | 'month' | 'day') {
	const parts = new Intl.DateTimeFormat(appLocale, {
		timeZone: appTimeZone,
		year: 'numeric',
		month: 'numeric',
		day: 'numeric'
	}).formatToParts(date);

	return parts.find((datePart) => datePart.type === part)?.value;
}
