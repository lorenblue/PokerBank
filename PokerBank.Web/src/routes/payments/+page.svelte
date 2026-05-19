<script lang="ts">
	import type { PageData } from './$types';

	let { data, form }: { data: PageData; form: { error?: string; success?: boolean } | null } = $props();

	let isRecordPaymentOpen = $state(false);

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
							<h3 class="text-sm font-bold">{playerNames.get(payment.playerId) ?? payment.playerId}</h3>
							<p class="mt-1 text-xs text-slate-500">
								{paymentLabel(payment.type)} · {methodLabel(payment.method)} · {new Date(
									payment.recordedAtUtc
								).toLocaleString()}
							</p>
						</div>
						<strong
							class={`text-right font-bold ${payment.type === 'BankPaysPlayer' ? 'text-emerald-700' : 'text-red-700'}`}
						>
							{payment.type === 'BankPaysPlayer' ? '+' : '-'}{unsignedMoney(payment.amount)}
						</strong>
					</article>
				{/each}
			</div>
		{/if}
	</div>
</section>

{#if isRecordPaymentOpen}
	<div class="fixed inset-0 z-50 grid place-items-center bg-slate-950/35 p-4">
		<div class="w-full max-w-md rounded-lg bg-white p-5 shadow-xl">
			<div class="mb-4 flex items-start justify-between gap-4">
				<h2 class="text-lg font-bold text-slate-950">Record payment</h2>
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
					Player
					<select name="playerId" required class="rounded-md border border-slate-300 px-3 py-2">
						<option value="">Choose a player</option>
						{#each activePlayers as player}
							<option value={player.id}>{player.name}</option>
						{/each}
					</select>
				</label>

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
