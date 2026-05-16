<script lang="ts">
	import type { PageData } from './$types';

	let { data, form }: { data: PageData; form: { error?: string } | null } = $props();

	const openGames = $derived(data.games.filter((game) => game.status === 'Open'));
	const closedGames = $derived(data.games.filter((game) => game.status !== 'Open'));
	const openGame = $derived(openGames[0]);
	const players = $derived(data.players ?? []);
	const activePlayers = $derived(players.filter((player) => player.isActive));
	const archivedPlayers = $derived(players.filter((player) => !player.isActive));
	const payments = $derived(data.payments ?? []);
	const playerNames = $derived(new Map(players.map((player) => [player.id, player.name] as const)));

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
</script>

<svelte:head>
	<title>PokerBank</title>
</svelte:head>

<section class="mb-6 flex flex-col items-start justify-between gap-4 sm:flex-row sm:items-center">
	<div>
		<p class="text-xs font-extrabold tracking-wider text-emerald-900 uppercase">PokerBank</p>
		<h1 class="mt-1 text-4xl leading-none font-bold tracking-normal sm:text-6xl">Dashboard</h1>
	</div>

	{#if openGame}
		<a
			href={`/games/${openGame.id}`}
			class="inline-flex rounded-md bg-emerald-900 px-4 py-3 font-bold text-white hover:bg-emerald-950"
		>
			Continue open game
		</a>
	{:else}
		<form method="POST" action="?/createGame">
			<button
				type="submit"
				class="rounded-md bg-emerald-900 px-4 py-3 font-bold text-white hover:bg-emerald-950"
			>
				New game
			</button>
		</form>
	{/if}
</section>

<section class="mt-4 rounded-lg border border-slate-200 bg-white p-4 shadow-xs">
	<div class="mb-4 flex items-center justify-between">
		<h2 class="text-base font-bold">Players</h2>
	</div>

	<form method="POST" action="?/createPlayer" class="grid gap-3 sm:grid-cols-[1fr_auto]">
		<label class="grid gap-1 text-sm font-bold text-slate-700">
			Name
			<input
				name="name"
				type="text"
				autocomplete="off"
				required
				class="rounded-md border border-slate-300 px-3 py-2"
			/>
		</label>

		<button
			type="submit"
			class="self-end rounded-md bg-emerald-900 px-4 py-3 font-bold text-white hover:bg-emerald-950"
		>
			Add player
		</button>
	</form>

	<div class="mt-4 grid gap-3 sm:grid-cols-2 lg:grid-cols-3">
		{#if activePlayers.length === 0}
			<p class="text-sm text-slate-500">No active players yet.</p>
		{:else}
			{#each activePlayers as player}
				<div class="rounded-lg border border-slate-100 p-3">
					<h3 class="text-sm font-bold">{player.name}</h3>
					<p class="mt-1 text-sm text-slate-500">Active</p>
				</div>
			{/each}
		{/if}
	</div>

	{#if archivedPlayers.length > 0}
		<div class="mt-4">
			<h3 class="text-sm font-bold">Archived</h3>
			<div class="mt-3 grid gap-3 sm:grid-cols-2 lg:grid-cols-3">
				{#each archivedPlayers as player}
					<div class="rounded-lg border border-slate-100 p-3 opacity-55">
						<h4 class="text-sm font-bold">{player.name}</h4>
						<p class="mt-1 text-sm text-slate-500">Archived</p>
					</div>
				{/each}
			</div>
		</div>
	{/if}
</section>

<section class="mt-4 rounded-lg border border-slate-200 bg-white p-4 shadow-xs">
	<div class="mb-4 flex items-center justify-between">
		<h2 class="text-base font-bold">Payments</h2>
	</div>

	<form method="POST" action="?/createPayment" class="grid gap-3 lg:grid-cols-[1fr_10rem_12rem_auto]">
		<label class="grid gap-1 text-sm font-bold text-slate-700">
			Player
			<select name="playerId" required class="rounded-md border border-slate-300 px-3 py-2">
				<option value="">Choose player</option>
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
				<option value="PlayerPaysBank">Player paid me</option>
				<option value="BankPaysPlayer">I paid player</option>
			</select>
		</label>

		<button
			type="submit"
			class="self-end rounded-md bg-emerald-900 px-4 py-3 font-bold text-white hover:bg-emerald-950"
		>
			Record payment
		</button>
	</form>

	<div class="mt-4 grid gap-3">
		{#if payments.length === 0}
			<p class="text-sm text-slate-500">No payments recorded yet.</p>
		{:else}
			{#each payments.slice(0, 5) as payment}
				<div class="flex items-center justify-between gap-4 rounded-lg border border-slate-100 p-3">
					<div>
						<h3 class="text-sm font-bold">{playerNames.get(payment.playerId) ?? payment.playerId}</h3>
						<p class="mt-1 text-sm text-slate-500">
							{paymentLabel(payment.type)} on {new Date(payment.recordedAtUtc).toLocaleString()}
						</p>
					</div>
					<strong
						class={`text-right text-lg font-bold ${payment.type === 'BankPaysPlayer' ? 'text-emerald-700' : 'text-red-700'}`}
					>
						{payment.type === 'BankPaysPlayer' ? '+' : '-'}{unsignedMoney(payment.amount)}
					</strong>
				</div>
			{/each}
		{/if}
	</div>
</section>

{#if form?.error}
	<p class="mb-4 rounded-lg border border-red-200 bg-red-50 px-4 py-3 text-red-700">{form.error}</p>
{/if}

<section class="grid gap-4 lg:grid-cols-2">
	<div class="rounded-lg border border-slate-200 bg-white p-4 shadow-xs">
		<div class="flex items-center justify-between">
			<h2 class="mb-4 text-base font-bold">Current balances</h2>
		</div>

		{#if data.balances.length === 0}
			<p class="text-sm text-slate-500">No players yet.</p>
		{:else}
			<div class="grid gap-3">
				{#each data.balances as balance}
					<article
						class={`flex items-center justify-between gap-4 rounded-lg border border-slate-100 p-3 ${balance.isActive ? '' : 'opacity-55'}`}
					>
						<div>
							<h3 class="text-sm font-bold">{balance.playerName}</h3>
							<p class="mt-1 text-sm text-slate-500">{balanceLabel(balance.balanceAmount)}</p>
						</div>
						<strong
							class={`text-right text-lg font-bold ${Number(balance.balanceAmount) > 0 ? 'text-emerald-700' : Number(balance.balanceAmount) < 0 ? 'text-red-700' : ''}`}
						>
							{money(balance.balanceAmount)}
						</strong>
					</article>
				{/each}
			</div>
		{/if}
	</div>

	<div class="rounded-lg border border-slate-200 bg-white p-4 shadow-xs">
		<div class="flex items-center justify-between">
			<h2 class="mb-4 text-base font-bold">Games</h2>
		</div>

		<div>
			<h3 class="text-sm font-bold">Open</h3>
			{#if openGames.length === 0}
				<p class="mt-1 text-sm text-slate-500">No open games.</p>
			{:else}
				<div class="mt-3 grid gap-3">
					{#each openGames as game}
						<a
							href={`/games/${game.id}`}
							class="flex items-center justify-between gap-4 rounded-lg border border-slate-100 p-3 hover:bg-slate-50"
						>
							<span class="text-sm text-slate-500">{new Date(game.createdAtUtc).toLocaleString()}</span>
							<strong>{game.status}</strong>
						</a>
					{/each}
				</div>
			{/if}
		</div>

		<div class="mt-6">
			<h3 class="text-sm font-bold">Closed</h3>
			{#if closedGames.length === 0}
				<p class="mt-1 text-sm text-slate-500">No closed games.</p>
			{:else}
				<div class="mt-3 grid gap-3">
					{#each closedGames as game}
						<a
							href={`/games/${game.id}`}
							class="flex items-center justify-between gap-4 rounded-lg border border-slate-100 p-3 hover:bg-slate-50"
						>
							<span class="text-sm text-slate-500">{new Date(game.createdAtUtc).toLocaleString()}</span>
							<strong>{game.status}</strong>
						</a>
					{/each}
				</div>
			{/if}
		</div>
	</div>
</section>
