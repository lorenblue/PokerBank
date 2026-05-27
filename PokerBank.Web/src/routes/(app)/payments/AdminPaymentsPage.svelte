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

	const players = $derived(data.players ?? []);
	const payments = $derived(data.payments ?? []);
	const activePlayers = $derived(players.filter((player) => player.isActive));
	const playerNames = $derived(new Map(players.map((player) => [player.id, player.name] as const)));

	function unsignedMoney(value: number | string) {
		return `$${Math.abs(Number(value)).toFixed(2)}`;
	}

	function paymentLabel(direction: string) {
		if (direction === 'MadeByPlayer') return 'Player made payment';
		if (direction === 'ReceivedByPlayer') return 'Player received payment';

		return direction;
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

<section class="page-header">
	<div>
		<h1 class="page-title">Payments</h1>
	</div>

	<button
		type="button"
		disabled={activePlayers.length === 0}
		class="btn btn-primary"
		onclick={() => (isRecordPaymentOpen = true)}
	>
		Record payment
	</button>
</section>

{#if form?.error}
	<p class="alert alert-error">{form.error}</p>
{:else if form?.success}
	<p class="alert alert-success">Payment recorded.</p>
{:else if form?.deleted}
	<p class="alert alert-success">Payment deleted.</p>
{/if}

<section class="card card-pad">
	<div class="section-head">
		<div>
			<h2 class="section-title">Payment history</h2>
			<p class="row-meta">{payments.length} recorded payments</p>
		</div>
	</div>

	{#if activePlayers.length === 0}
		<p class="empty-state">Add an active player before recording payments.</p>
	{:else if payments.length === 0}
		<p class="empty-state">No payments recorded yet.</p>
	{:else}
		<div class="table-wrap">
			<table class="data-table">
				<thead>
					<tr>
						<th>Player</th>
						<th>Direction</th>
						<th>Method</th>
						<th>Recorded</th>
						<th class="text-right">Amount</th>
						<th class="text-right">Action</th>
					</tr>
				</thead>
				<tbody>
					{#each payments as payment}
						<tr>
							<td>
								<a href={`/players/${payment.playerId}`} class="row-title">
									{playerNames.get(payment.playerId) ?? payment.playerId}
								</a>
							</td>
							<td>{paymentLabel(payment.direction)}</td>
							<td>{methodLabel(payment.method)}</td>
							<td class="muted">{formatDateTime(payment.recordedAtUtc)}</td>
							<td
								class={`text-right amount ${payment.direction === 'ReceivedByPlayer' ? 'amount-positive' : 'amount-negative'}`}
							>
								{payment.direction === 'ReceivedByPlayer' ? '+' : '-'}{unsignedMoney(payment.amount)}
							</td>
							<td class="text-right">
								<button type="button" class="btn btn-subtle-danger" onclick={() => (paymentToDelete = payment)}>
									Delete
								</button>
							</td>
						</tr>
					{/each}
				</tbody>
			</table>
		</div>
	{/if}
</section>

{#if isRecordPaymentOpen}
	<RecordPaymentModal players={activePlayers} onClose={() => (isRecordPaymentOpen = false)} />
{/if}

{#if paymentToDelete}
	<Modal title="Delete payment?" onClose={closeDeletePayment}>
		<p class="page-subtitle">
			This removes the payment from the ledger. This is only intended for payments recorded by mistake.
		</p>

		<div class="empty-state mt-4">
			<strong>{playerNames.get(paymentToDelete.playerId) ?? paymentToDelete.playerId}</strong>
			<p>
				{paymentLabel(paymentToDelete.direction)} · {methodLabel(paymentToDelete.method)} ·
				{unsignedMoney(paymentToDelete.amount)}
			</p>
		</div>

		<form method="POST" action="?/deletePayment" class="form-grid mt-5">
			<input type="hidden" name="paymentId" value={paymentToDelete.id} />
			<div class="form-actions">
				<button type="button" class="btn btn-secondary" onclick={closeDeletePayment}>Cancel</button>
				<button type="submit" class="btn btn-danger">Delete payment</button>
			</div>
		</form>
	</Modal>
{/if}
