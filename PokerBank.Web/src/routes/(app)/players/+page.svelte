<script lang="ts">
	import Modal from '$lib/components/Modal.svelte';
	import type { PageData } from './$types';

	let { data, form }: { data: PageData; form: { error?: string } | null } = $props();

	let isAddPlayerOpen = $state(false);
	let playerToEdit = $state<PageData['players'][number] | null>(null);

	const players = $derived(data.players ?? []);
	const activePlayers = $derived(players.filter((player) => player.isActive));
	const archivedPlayers = $derived(players.filter((player) => !player.isActive));

	function closeEditPlayer() {
		playerToEdit = null;
	}
</script>

<svelte:head>
	<title>Players | PokerBank</title>
</svelte:head>

<section class="page-header">
	<div>
		<h1 class="page-title">Players</h1>
	</div>

	<button type="button" class="btn btn-primary" onclick={() => (isAddPlayerOpen = true)}>Add player</button>
</section>

{#if form?.error}
	<p class="alert alert-error">{form.error}</p>
{/if}

<section class="card card-pad">
	<div class="section-head">
		<div>
			<h2 class="section-title">Active players</h2>
			<p class="row-meta">{activePlayers.length} available players</p>
		</div>
	</div>

	{#if activePlayers.length === 0}
		<p class="empty-state">No active players yet.</p>
	{:else}
		<div class="table-wrap">
			<table class="data-table">
				<thead>
					<tr>
						<th>Name</th>
						<th>Email</th>
						<th>Status</th>
						<th class="text-right">Action</th>
					</tr>
				</thead>
				<tbody>
					{#each activePlayers as player}
						<tr>
							<td>
								<a href={`/players/${player.id}`} class="row-title">{player.name}</a>
							</td>
							<td class="muted">{player.emailAddress ?? 'No email address'}</td>
							<td><span class="badge badge-open">Active</span></td>
							<td class="text-right">
								<button type="button" class="btn btn-ghost" onclick={() => (playerToEdit = player)}>
									Edit
								</button>
							</td>
						</tr>
					{/each}
				</tbody>
			</table>
		</div>
	{/if}
</section>

{#if archivedPlayers.length > 0}
	<section class="card card-pad mt-4">
		<div class="section-head">
			<h2 class="section-title">Archived</h2>
			<span class="badge">{archivedPlayers.length}</span>
		</div>

		<div class="data-list">
			{#each archivedPlayers as player}
				<a href={`/players/${player.id}`} class="data-row opacity-65">
					<div>
						<strong class="row-title">{player.name}</strong>
						<p class="row-meta">{player.emailAddress ?? 'No email address'}</p>
					</div>
					<span class="badge">Archived</span>
				</a>
			{/each}
		</div>
	</section>
{/if}

{#if isAddPlayerOpen}
	<Modal title="Add player" onClose={() => (isAddPlayerOpen = false)}>
		<form method="POST" action="?/createPlayer" class="form-grid">
			<label class="field">
				Name
				<input name="name" type="text" autocomplete="off" required />
			</label>

			<label class="field">
				Email
				<input name="emailAddress" type="email" autocomplete="email" />
			</label>

			<div class="form-actions">
				<button type="button" class="btn btn-secondary" onclick={() => (isAddPlayerOpen = false)}>
					Cancel
				</button>
				<button type="submit" class="btn btn-primary">Add player</button>
			</div>
		</form>
	</Modal>
{/if}

{#if playerToEdit}
	<Modal title="Edit player" onClose={closeEditPlayer}>
		<form method="POST" action="?/updatePlayer" class="form-grid">
			<input type="hidden" name="playerId" value={playerToEdit.id} />
			<label class="field">
				Name
				<input name="name" type="text" autocomplete="off" required value={playerToEdit.name} />
			</label>

			<label class="field">
				Email
				<input
					name="emailAddress"
					type="email"
					autocomplete="email"
					value={playerToEdit.emailAddress ?? ''}
				/>
			</label>

			<div class="form-actions">
				<button type="button" class="btn btn-secondary" onclick={closeEditPlayer}>Cancel</button>
				<button type="submit" class="btn btn-primary">Save player</button>
			</div>
		</form>
	</Modal>
{/if}
