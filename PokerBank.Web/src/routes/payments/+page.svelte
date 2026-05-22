<script lang="ts">
	import Modal from '$lib/components/Modal.svelte';
	import RecordPaymentModal from '$lib/components/RecordPaymentModal.svelte';
	import { formatDateTime } from '$lib/format';
	import type { PageData } from './$types';

	let {
		data,
		form
	}: { data: PageData; form: { deleted?: boolean; error?: string; success?: boolean } | null } = $props();

	let isRecordPaymentOpen = $state(false);
	let paymentToDelete = $state<PageData['payments'][number] | null>(null);

	const activePlayers = $derived(data.players.filter((player) => player.isActive));
	const playerNames = $derived(new Map(data.players.map((player) => [player.id, player.name] as const)));

	function unsignedMoney(value: number | string) {
		return `$${Math.abs(Number(value)).toFixed(2)}`;
	}

	function paymentLabel(type: string) {
		if (type === 'PlayerPaysBank') return 'Player paid me';
		if (type === 'BankPaysPlayer') return 'I paid player';

		return type;
	}

	function methodLabel(method: string) {
		if (method === 'ETransfer') return 'e-transfer';
		if (method === 'Cash') return 'cash';

		return method;
	}

	function closeDeletePayment() {
		paymentToDelete = null;
	}
</script>

<svelte:head>
	<title>Payments | PokerBank</title>
</svelte:head>

<section class="mb-6 flex flex-col items-start justify-between gap-4 sm:flex-row sm:items-center">
	<h1 class="mt-1 text-4xl leading-none font-bold tracking-normal sm:text-6xl">Payments</h1>

	<button
		type="button"
		disabled={activePlayers.length === 0}
		class="rounded-md bg-emerald-900 px-4 py-3 font-bold text-white hover:bg-emerald-950 disabled:cursor-not-allowed disabled:bg-slate-300 disabled:text-slate-500"
		onclick={() => (isRecordPaymentOpen = true)}
	>
		Record payment
	</button>
</section>

{#if form?.error}
	<p class="mb-4 rounded-lg border border-red-200 bg-red-50 px-4 py-3 text-red-700">{form.error}</p>
{:else if form?.success}
	<p class="mb-4 rounded-lg border border-emerald-200 bg-emerald-50 px-4 py-3 text-emerald-800">
		Payment recorded.
	</p>
{:else if form?.deleted}
	<p class="mb-4 rounded-lg border border-emerald-200 bg-emerald-50 px-4 py-3 text-emerald-800">
		Payment deleted.
	</p>
{/if}

<section>
	<div class="rounded-lg border border-slate-200 bg-white p-4 shadow-xs">
		<h2 class="mb-4 text-base font-bold">Payment history</h2>

		{#if activePlayers.length === 0}
			<p class="mb-4 rounded-lg border border-slate-100 bg-slate-50 px-3 py-2 text-sm text-slate-500">
				Add an active player before recording payments.
			</p>
		{/if}

		{#if data.payments.length === 0}
			<p class="text-sm text-slate-500">No payments recorded yet.</p>
		{:else}
			<div class="grid gap-3">
				{#each data.payments as payment}
					<article class="flex items-center justify-between gap-4 rounded-lg border border-slate-100 p-3">
						<div>
							<a href={`/players/${payment.playerId}`} class="text-sm font-bold hover:text-emerald-900">
								{playerNames.get(payment.playerId) ?? payment.playerId}
							</a>
							<p class="mt-1 text-xs text-slate-500">
								{paymentLabel(payment.type)} · {methodLabel(payment.method)} · {formatDateTime(
									payment.recordedAtUtc
								)}
							</p>
						</div>
						<div class="flex items-center gap-3">
							<strong
								class={`text-right font-bold ${payment.type === 'BankPaysPlayer' ? 'text-emerald-700' : 'text-red-700'}`}
							>
								{payment.type === 'BankPaysPlayer' ? '+' : '-'}{unsignedMoney(payment.amount)}
							</strong>
							<button
								type="button"
								class="rounded-md px-2 py-1 text-xs font-bold text-red-700 hover:bg-red-50"
								onclick={() => (paymentToDelete = payment)}
							>
								Delete
							</button>
						</div>
					</article>
				{/each}
			</div>
		{/if}
	</div>
</section>

{#if isRecordPaymentOpen}
	<RecordPaymentModal players={activePlayers} onClose={() => (isRecordPaymentOpen = false)} />
{/if}

{#if paymentToDelete}
	<Modal title="Delete payment?" onClose={closeDeletePayment}>
		<p class="mt-2 text-sm leading-6 text-slate-600">
			This removes the payment from the ledger. This is only intended for payments recorded by
			mistake.
		</p>

		<div class="mt-4 rounded-lg border border-slate-100 bg-slate-50 p-3 text-sm">
			<p class="font-bold text-slate-950">
				{playerNames.get(paymentToDelete.playerId) ?? paymentToDelete.playerId}
			</p>
			<p class="mt-1 text-slate-600">
				{paymentLabel(paymentToDelete.type)} · {methodLabel(paymentToDelete.method)} ·
				{unsignedMoney(paymentToDelete.amount)}
			</p>
		</div>

		<form method="POST" action="?/deletePayment" class="mt-5 flex justify-end gap-2">
			<input type="hidden" name="paymentId" value={paymentToDelete.id} />
			<button
				type="button"
				class="rounded-md px-4 py-2 text-sm font-bold text-slate-700 hover:bg-slate-50"
				onclick={closeDeletePayment}
			>
				Cancel
			</button>
			<button
				type="submit"
				class="rounded-md bg-red-700 px-4 py-2 text-sm font-bold text-white hover:bg-red-800"
			>
				Delete payment
			</button>
		</form>
	</Modal>
{/if}
