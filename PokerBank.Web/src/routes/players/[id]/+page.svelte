<script lang="ts">
	import { formatDateTime } from '$lib/format';
	import type { PageData } from './$types';

	let { data }: { data: PageData } = $props();

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

	function paymentAmount(type: string, amount: number | string) {
		return `${type === 'BankPaysPlayer' ? '+' : '-'}${unsignedMoney(amount)}`;
	}
</script>

<svelte:head>
	<title>{data.player.name} | PokerBank</title>
</svelte:head>

<a href="/players" class="mb-4 inline-block font-bold text-emerald-900">Back to players</a>

<section class="mb-6 flex flex-col items-start justify-between gap-4 sm:flex-row sm:items-center">
	<div>
		<p class="text-xs font-extrabold tracking-wider text-emerald-900 uppercase">
			{data.player.isActive ? 'Active player' : 'Archived player'}
		</p>
		<h1 class="mt-1 text-4xl leading-none font-bold tracking-normal sm:text-6xl">{data.player.name}</h1>
	</div>
</section>

<section class="my-4 grid gap-4 md:grid-cols-3">
	<div class="rounded-lg border border-slate-200 bg-white p-4 shadow-xs">
		<span class="mb-1 block text-sm text-slate-500">Balance</span>
		<strong
			class={`text-2xl ${Number(data.balance?.balanceAmount ?? 0) > 0 ? 'text-emerald-700' : Number(data.balance?.balanceAmount ?? 0) < 0 ? 'text-red-700' : ''}`}
		>
			{money(data.balance?.balanceAmount ?? 0)}
		</strong>
		<p class="mt-1 text-sm text-slate-500">{balanceLabel(data.balance?.balanceAmount ?? 0)}</p>
	</div>
	<div class="rounded-lg border border-slate-200 bg-white p-4 shadow-xs">
		<span class="mb-1 block text-sm text-slate-500">Game net</span>
		<strong
			class={`text-2xl ${Number(data.balance?.gameNetAmount ?? 0) > 0 ? 'text-emerald-700' : Number(data.balance?.gameNetAmount ?? 0) < 0 ? 'text-red-700' : ''}`}
		>
			{money(data.balance?.gameNetAmount ?? 0)}
		</strong>
	</div>
	<div class="rounded-lg border border-slate-200 bg-white p-4 shadow-xs">
		<span class="mb-1 block text-sm text-slate-500">Payment net</span>
		<strong
			class={`text-2xl ${Number(data.balance?.paymentNetAmount ?? 0) > 0 ? 'text-emerald-700' : Number(data.balance?.paymentNetAmount ?? 0) < 0 ? 'text-red-700' : ''}`}
		>
			{money(data.balance?.paymentNetAmount ?? 0)}
		</strong>
	</div>
</section>

<section class="mt-4 grid gap-4 lg:grid-cols-2">
	<div class="rounded-lg border border-slate-200 bg-white p-4 shadow-xs">
		<h2 class="mb-4 text-base font-bold">Game results</h2>

		{#if data.gameResults.length === 0}
			<p class="text-sm text-slate-500">No closed-game results yet.</p>
		{:else}
			<div class="grid gap-3">
				{#each data.gameResults as result}
					<a
						href={`/games/${result.gameId}`}
						class="flex items-center justify-between gap-4 rounded-lg border border-slate-100 p-3 hover:bg-slate-50"
					>
						<div class="min-w-0">
							<strong>{formatDateTime(result.playedAtUtc)}</strong>
							<span class="mt-1 block text-sm text-slate-500">
								Buy-ins {unsignedMoney(result.buyInAmount)} · Cash-outs {unsignedMoney(result.cashOutAmount)}
							</span>
						</div>
						<strong
							class={`shrink-0 text-right ${Number(result.netAmount) > 0 ? 'text-emerald-700' : Number(result.netAmount) < 0 ? 'text-red-700' : ''}`}
						>
							{money(result.netAmount)}
						</strong>
					</a>
				{/each}
			</div>
		{/if}
	</div>

	<div class="rounded-lg border border-slate-200 bg-white p-4 shadow-xs">
		<h2 class="mb-4 text-base font-bold">Payments</h2>

		{#if data.payments.length === 0}
			<p class="text-sm text-slate-500">No payments recorded for this player.</p>
		{:else}
			<div class="grid gap-3">
				{#each data.payments as payment}
					<div class="flex items-center justify-between gap-4 rounded-lg border border-slate-100 p-3">
						<div class="min-w-0">
							<strong>{formatDateTime(payment.recordedAtUtc)}</strong>
							<span class="mt-1 block text-sm text-slate-500">
								{paymentLabel(payment.type)} · {methodLabel(payment.method)}
							</span>
						</div>
						<strong
							class={`shrink-0 text-right ${payment.type === 'BankPaysPlayer' ? 'text-emerald-700' : 'text-red-700'}`}
						>
							{paymentAmount(payment.type, payment.amount)}
						</strong>
					</div>
				{/each}
			</div>
		{/if}
	</div>
</section>
