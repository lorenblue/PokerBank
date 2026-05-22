import type { RecordPaymentRequest } from '$lib/api/client';

export type PaymentDirection = 'MadeByPlayer' | 'ReceivedByPlayer';

type PaymentFields = RecordPaymentRequest & {
	direction: PaymentDirection;
};

export function readPaymentFields(data: FormData): PaymentFields | null {
	const amount = Number(data.get('amount'));
	const direction = data.get('direction')?.toString();
	const method = data.get('method')?.toString();

	if (!Number.isFinite(amount) || !isPaymentDirection(direction) || !isPaymentMethod(method)) {
		return null;
	}

	return { amount, direction, method };
}

function isPaymentDirection(value: string | undefined): value is 'MadeByPlayer' | 'ReceivedByPlayer' {
	return value === 'MadeByPlayer' || value === 'ReceivedByPlayer';
}

function isPaymentMethod(value: string | undefined): value is 'ETransfer' | 'Cash' {
	return value === 'ETransfer' || value === 'Cash';
}
