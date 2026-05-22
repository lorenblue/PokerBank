<script lang="ts">
	import type { PageData } from './$types';

	let { data, form }: { data: PageData; form: { error?: string } | null } = $props();

	let isAddPlayerOpen = $state(false);
	let playerToRename = $state<PageData['players'][number] | null>(null);

	const activePlayers = $derived(data.players.filter((player) => player.isActive));
	const archivedPlayers = $derived(data.players.filter((player) => !player.isActive));

	function closeRenamePlayer() {
		playerToRename = null;
	}
</script>

<svelte:head>
	<title>Players | PokerBank</title>
</svelte:head>

<section class="mb-6 flex flex-col items-start justify-between gap-4 sm:flex-row sm:items-center">
	<h1 class="mt-1 text-4xl leading-none font-bold tracking-normal sm:text-6xl">Players</h1>

	<button
		type="button"
		class="rounded-md bg-emerald-900 px-4 py-3 font-bold text-white hover:bg-emerald-950"
		onclick={() => (isAddPlayerOpen = true)}
	>
		Add player
	</button>
</section>

{#if form?.error}
	<p class="mb-4 rounded-lg border border-red-200 bg-red-50 px-4 py-3 text-red-700">{form.error}</p>
{/if}

<section>
	<div class="rounded-lg border border-slate-200 bg-white p-4 shadow-xs">
		<h2 class="mb-4 text-base font-bold">Active players</h2>

		{#if activePlayers.length === 0}
			<p class="text-sm text-slate-500">No active players yet.</p>
		{:else}
			<div class="grid gap-3 sm:grid-cols-2">
				{#each activePlayers as player}
					<div class="flex items-center justify-between gap-3 rounded-lg border border-slate-100 p-3">
						<a href={`/players/${player.id}`} class="min-w-0 hover:text-emerald-900">
							<h3 class="text-sm font-bold">{player.name}</h3>
							<p class="mt-1 text-sm text-slate-500">Active</p>
						</a>
						<button
							type="button"
							class="rounded-md px-2 py-1 text-xs font-bold text-emerald-900 hover:bg-emerald-50"
							onclick={() => (playerToRename = player)}
						>
							Rename
						</button>
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
							<a href={`/players/${player.id}`} class="block hover:text-emerald-900">
								<h3 class="text-sm font-bold">{player.name}</h3>
								<p class="mt-1 text-sm text-slate-500">Archived</p>
							</a>
						</div>
					{/each}
				</div>
			</div>
		{/if}
	</div>
</section>

{#if isAddPlayerOpen}
	<div class="fixed inset-0 z-50 grid place-items-center bg-slate-950/35 p-4">
		<div class="w-full max-w-md rounded-lg bg-white p-5 shadow-xl">
			<div class="mb-4 flex items-start justify-between gap-4">
				<h2 class="text-lg font-bold text-slate-950">Add player</h2>
				<button
					type="button"
					class="rounded-md px-2 py-1 text-sm font-bold text-slate-500 hover:bg-slate-100 hover:text-slate-900"
					aria-label="Close"
					onclick={() => (isAddPlayerOpen = false)}
				>
					Close
				</button>
			</div>

			<form method="POST" action="?/createPlayer" class="grid gap-4">
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

				<div class="flex justify-end gap-2">
					<button
						type="button"
						class="rounded-md px-4 py-2 text-sm font-bold text-slate-700 hover:bg-slate-50"
						onclick={() => (isAddPlayerOpen = false)}
					>
						Cancel
					</button>
					<button
						type="submit"
						class="rounded-md bg-emerald-900 px-4 py-2 text-sm font-bold text-white hover:bg-emerald-950"
					>
						Add player
					</button>
				</div>
			</form>
		</div>
	</div>
{/if}

{#if playerToRename}
	<div class="fixed inset-0 z-50 grid place-items-center bg-slate-950/35 p-4">
		<div class="w-full max-w-md rounded-lg bg-white p-5 shadow-xl">
			<div class="mb-4 flex items-start justify-between gap-4">
				<h2 class="text-lg font-bold text-slate-950">Rename player</h2>
				<button
					type="button"
					class="rounded-md px-2 py-1 text-sm font-bold text-slate-500 hover:bg-slate-100 hover:text-slate-900"
					aria-label="Close"
					onclick={closeRenamePlayer}
				>
					Close
				</button>
			</div>

			<form method="POST" action="?/renamePlayer" class="grid gap-4">
				<input type="hidden" name="playerId" value={playerToRename.id} />
				<label class="grid gap-1 text-sm font-bold text-slate-700">
					Name
					<input
						name="name"
						type="text"
						autocomplete="off"
						required
						value={playerToRename.name}
						class="rounded-md border border-slate-300 px-3 py-2"
					/>
				</label>

				<div class="flex justify-end gap-2">
					<button
						type="button"
						class="rounded-md px-4 py-2 text-sm font-bold text-slate-700 hover:bg-slate-50"
						onclick={closeRenamePlayer}
					>
						Cancel
					</button>
					<button
						type="submit"
						class="rounded-md bg-emerald-900 px-4 py-2 text-sm font-bold text-white hover:bg-emerald-950"
					>
						Rename player
					</button>
				</div>
			</form>
		</div>
	</div>
{/if}
