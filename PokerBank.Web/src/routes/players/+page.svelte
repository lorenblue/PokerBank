<script lang="ts">
	import type { PageData } from './$types';

	let { data, form }: { data: PageData; form: { error?: string } | null } = $props();

	const activePlayers = $derived(data.players.filter((player) => player.isActive));
	const archivedPlayers = $derived(data.players.filter((player) => !player.isActive));
</script>

<svelte:head>
	<title>Players | PokerBank</title>
</svelte:head>

<section class="mb-6">
	<p class="text-xs font-extrabold tracking-wider text-emerald-900 uppercase">Players</p>
	<h1 class="mt-1 text-4xl leading-none font-bold tracking-normal sm:text-6xl">Friends</h1>
</section>

{#if form?.error}
	<p class="mb-4 rounded-lg border border-red-200 bg-red-50 px-4 py-3 text-red-700">{form.error}</p>
{/if}

<section class="grid gap-4 lg:grid-cols-[minmax(18rem,24rem)_1fr]">
	<div class="rounded-lg border border-slate-200 bg-white p-4 shadow-xs">
		<h2 class="mb-4 text-base font-bold">Add player</h2>

		<form method="POST" action="?/createPlayer" class="grid gap-3">
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
				class="rounded-md bg-emerald-900 px-4 py-3 font-bold text-white hover:bg-emerald-950"
			>
				Add player
			</button>
		</form>
	</div>

	<div class="rounded-lg border border-slate-200 bg-white p-4 shadow-xs">
		<h2 class="mb-4 text-base font-bold">Active players</h2>

		{#if activePlayers.length === 0}
			<p class="text-sm text-slate-500">No active players yet.</p>
		{:else}
			<div class="grid gap-3 sm:grid-cols-2">
				{#each activePlayers as player}
					<div class="rounded-lg border border-slate-100 p-3">
						<h3 class="text-sm font-bold">{player.name}</h3>
						<p class="mt-1 text-sm text-slate-500">Active</p>
					</div>
				{/each}
			</div>
		{/if}

		{#if archivedPlayers.length > 0}
			<div class="mt-6">
				<h2 class="mb-4 text-base font-bold">Archived</h2>
				<div class="grid gap-3 sm:grid-cols-2">
					{#each archivedPlayers as player}
						<div class="rounded-lg border border-slate-100 p-3 opacity-55">
							<h3 class="text-sm font-bold">{player.name}</h3>
							<p class="mt-1 text-sm text-slate-500">Archived</p>
						</div>
					{/each}
				</div>
			</div>
		{/if}
	</div>
</section>
