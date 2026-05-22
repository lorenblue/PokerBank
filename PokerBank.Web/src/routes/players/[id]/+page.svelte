<script lang="ts">
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

	<button
		type="button"
		disabled={!data.player.isActive}
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
{/if}

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
		<div class="mb-4 flex items-center justify-between gap-3">
			<h2 class="text-base font-bold">Game results</h2>
			<a href="/games" class="text-sm font-bold text-emerald-900 hover:text-emerald-950">View games</a>
		</div>

		{#if data.gameResults.length === 0}
			<p class="rounded-lg border border-slate-100 bg-slate-50 px-3 py-2 text-sm text-slate-500">
				No closed-game results yet.
			</p>
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
		<div class="mb-4 flex items-center justify-between gap-3">
			<h2 class="text-base font-bold">Payments</h2>
			<a href="/payments" class="text-sm font-bold text-emerald-900 hover:text-emerald-950">View ledger</a>
		</div>

		{#if data.payments.length === 0}
			<p class="rounded-lg border border-slate-100 bg-slate-50 px-3 py-2 text-sm text-slate-500">
				No payments recorded for this player.
			</p>
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

{#if isRecordPaymentOpen}
	<div class="fixed inset-0 z-50 grid place-items-center bg-slate-950/35 p-4">
		<div class="w-full max-w-md rounded-lg bg-white p-5 shadow-xl">
			<div class="mb-4 flex items-start justify-between gap-4">
				<div>
					<h2 class="text-lg font-bold text-slate-950">Record payment</h2>
					<p class="mt-1 text-sm text-slate-500">{data.player.name}</p>
				</div>
				<button
					type="button"
					class="rounded-md px-2 py-1 text-sm font-bold text-slate-500 hover:bg-slate-100 hover:text-slate-900"
					aria-label="Close"
					onclick={() => (isRecordPaymentOpen = false)}
				>
					Close
				</button>
			</div>

			<form method="POST" action="?/createPayment" class="grid gap-4">
				<label class="grid gap-1 text-sm font-bold text-slate-700">
					Amount
					<input
						name="amount"
						type="number"
						min="0.01"
						step="0.01"
						required
						class="rounded-md border border-slate-300 px-3 py-2"
					/>
				</label>

				<label class="grid gap-1 text-sm font-bold text-slate-700">
					Direction
					<select name="type" required class="rounded-md border border-slate-300 px-3 py-2">
						<option value="">Choose direction</option>
						<option value="PlayerPaysBank">Player paid me</option>
						<option value="BankPaysPlayer">I paid player</option>
					</select>
				</label>

				<label class="grid gap-1 text-sm font-bold text-slate-700">
					Method
					<select name="method" required class="rounded-md border border-slate-300 px-3 py-2">
						<option value="">Choose method</option>
						<option value="ETransfer">e-transfer</option>
						<option value="Cash">cash</option>
					</select>
				</label>

				<div class="flex justify-end gap-2">
					<button
						type="button"
						class="rounded-md px-4 py-2 text-sm font-bold text-slate-700 hover:bg-slate-50"
						onclick={() => (isRecordPaymentOpen = false)}
					>
						Cancel
					</button>
					<button
						type="submit"
						class="rounded-md bg-emerald-900 px-4 py-2 text-sm font-bold text-white hover:bg-emerald-950"
					>
						Record payment
					</button>
				</div>
			</form>
		</div>
	</div>
{/if}
