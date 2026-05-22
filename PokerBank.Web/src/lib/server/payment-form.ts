import type { CreatePaymentRequest } from '$lib/api/client';

type PaymentFields = Omit<CreatePaymentRequest, 'playerId'>;

export function readPaymentFields(data: FormData): PaymentFields | null {
	const amount = Number(data.get('amount'));
	const type = data.get('type')?.toString();
	const method = data.get('method')?.toString();

	if (!Number.isFinite(amount) || !isPaymentType(type) || !isPaymentMethod(method)) {
		return null;
	}

	return { amount, type, method };
}

function isPaymentType(value: string | undefined): value is 'PlayerPaysBank' | 'BankPaysPlayer' {
	return value === 'PlayerPaysBank' || value === 'BankPaysPlayer';
}

function isPaymentMethod(value: string | undefined): value is 'ETransfer' | 'Cash' {
	return value === 'ETransfer' || value === 'Cash';
}
