<script lang="ts">
	import { formatDateTime } from '$lib/format';
	import type { PageData } from './$types';

	let { data }: { data: PageData } = $props();

	const payments = $derived(data.payments ?? []);

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
</script>

<svelte:head>
	<title>My Payments | PokerBank</title>
</svelte:head>

<section class="page-header">
	<div>
		<h1 class="page-title">My Payments</h1>
	</div>
</section>

<section class="card card-pad">
	<div class="section-head">
		<h2 class="section-title">My payment history</h2>
		<span class="badge">{payments.length}</span>
	</div>

	{#if payments.length === 0}
		<p class="empty-state">No payments recorded yet.</p>
	{:else}
		<div class="data-list">
			{#each payments as payment}
				<article class="data-row">
					<div>
						<strong class="row-title">{paymentLabel(payment.direction)}</strong>
						<p class="row-meta">{methodLabel(payment.method)} · {formatDateTime(payment.recordedAtUtc)}</p>
					</div>
					<strong
						class={`amount ${payment.direction === 'ReceivedByPlayer' ? 'amount-positive' : 'amount-negative'}`}
					>
						{payment.direction === 'ReceivedByPlayer' ? '+' : '-'}{unsignedMoney(payment.amount)}
					</strong>
				</article>
			{/each}
		</div>
	{/if}
</section>
