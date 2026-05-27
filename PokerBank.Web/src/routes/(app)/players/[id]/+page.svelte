<script lang="ts">
	import RecordPaymentModal from '$lib/components/RecordPaymentModal.svelte';
	import StatCard from '$lib/components/StatCard.svelte';
	import { formatDateTime } from '$lib/format';
	import type { PageData } from './$types';

	let {
		data,
		form
	}: { data: PageData; form: { error?: string; success?: boolean } | null } = $props();

	let isRecordPaymentOpen = $state(false);

	function money(value: number | string) {
		const amount = Number(value);
		const sign = amount > 0 ? '+' : amount < 0 ? '-' : '';

		return `${sign}$${Math.abs(amount).toFixed(2)}`;
	}

	function unsignedMoney(value: number | string) {
		return `$${Math.abs(Number(value)).toFixed(2)}`;
	}

	function balanceLabel(value: number | string) {
		const amount = Number(value);

		if (amount > 0) return 'should receive';
		if (amount < 0) return 'owes';

		return 'settled';
	}

	function moneyTone(value: number | string) {
		const amount = Number(value);

		if (amount > 0) return 'positive';
		if (amount < 0) return 'negative';

		return 'neutral';
	}

	function amountClass(value: number | string) {
		const amount = Number(value);

		if (amount > 0) return 'amount-positive';
		if (amount < 0) return 'amount-negative';

		return 'amount-neutral';
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

	function paymentAmount(direction: string, amount: number | string) {
		return `${direction === 'ReceivedByPlayer' ? '+' : '-'}${unsignedMoney(amount)}`;
	}
</script>

<svelte:head>
	<title>{data.player.name} | PokerBank</title>
</svelte:head>

<a href="/players" class="back-link">Back to players</a>

<section class="page-header">
	<div>
		<span class="eyebrow">{data.player.isActive ? 'Active player' : 'Archived player'}</span>
		<h1 class="page-title">{data.player.name}</h1>
		<p class="page-subtitle">{data.player.emailAddress ?? 'No email address'}</p>
	</div>

	<button
		type="button"
		disabled={!data.player.isActive}
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
{/if}

<section class="stat-grid">
	<StatCard
		label="Balance"
		value={money(data.balance?.balanceAmount ?? 0)}
		detail={balanceLabel(data.balance?.balanceAmount ?? 0)}
		tone={moneyTone(data.balance?.balanceAmount ?? 0)}
	/>
	<StatCard
		label="Game net"
		value={money(data.balance?.gameNetAmount ?? 0)}
		tone={moneyTone(data.balance?.gameNetAmount ?? 0)}
	/>
	<StatCard
		label="Payment net"
		value={money(data.balance?.paymentNetAmount ?? 0)}
		tone={moneyTone(data.balance?.paymentNetAmount ?? 0)}
	/>
</section>

<section class="grid-two">
	<div class="card card-pad">
		<div class="section-head">
			<div>
				<h2 class="section-title">Game results</h2>
				<p class="row-meta">Closed-game results for this player.</p>
			</div>
			<a href="/games" class="section-link">View games</a>
		</div>

		{#if data.gameResults.length === 0}
			<p class="empty-state">No closed-game results yet.</p>
		{:else}
			<div class="data-list">
				{#each data.gameResults as result}
					<a href={`/games/${result.gameId}`} class="data-row">
						<div>
							<strong class="row-title">{formatDateTime(result.playedAtUtc)}</strong>
							<p class="row-meta">
								Buy-ins {unsignedMoney(result.buyInAmount)} · Cash-outs {unsignedMoney(result.cashOutAmount)}
							</p>
						</div>
						<strong class={`amount ${amountClass(result.netAmount)}`}>{money(result.netAmount)}</strong>
					</a>
				{/each}
			</div>
		{/if}
	</div>

	<div class="card card-pad">
		<div class="section-head">
			<div>
				<h2 class="section-title">Payments</h2>
				<p class="row-meta">Settlement history for this player.</p>
			</div>
			<a href="/payments" class="section-link">View ledger</a>
		</div>

		{#if data.payments.length === 0}
			<p class="empty-state">No payments recorded for this player.</p>
		{:else}
			<div class="data-list">
				{#each data.payments as payment}
					<div class="data-row">
						<div>
							<strong class="row-title">{formatDateTime(payment.recordedAtUtc)}</strong>
							<p class="row-meta">{paymentLabel(payment.direction)} · {methodLabel(payment.method)}</p>
						</div>
						<strong
							class={`amount ${payment.direction === 'ReceivedByPlayer' ? 'amount-positive' : 'amount-negative'}`}
						>
							{paymentAmount(payment.direction, payment.amount)}
						</strong>
					</div>
				{/each}
			</div>
		{/if}
	</div>
</section>

{#if isRecordPaymentOpen}
	<RecordPaymentModal playerName={data.player.name} onClose={() => (isRecordPaymentOpen = false)} />
{/if}
