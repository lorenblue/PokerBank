<script lang="ts">
	import { formatDateTime } from '$lib/format';
	import type { PageData } from './$types';

	type AdminGame = Extract<PageData['games'][number], { createdAtUtc: string }>;

	let { data, form }: { data: PageData; form: { error?: string } | null } = $props();

	const games = $derived(data.games.filter((game): game is AdminGame => 'createdAtUtc' in game));
	const gamePage = $derived(data.gamePage);
	const openGame = $derived(data.openGame);
	const openGames = $derived(openGame ? [openGame] : []);
	const closedGames = $derived(games.filter((game) => game.status !== 'Open'));
	const currentPage = $derived(Number(gamePage?.page ?? 1));
	const pageSize = $derived(Number(gamePage?.pageSize ?? 10));
	const totalPages = $derived(Number(gamePage?.totalPages ?? 0));
	const hasPreviousPage = $derived(currentPage > 1);
	const hasNextPage = $derived(totalPages > currentPage);

	function pageHref(page: number) {
		const params = new URLSearchParams({
			page: page.toString(),
			pageSize: pageSize.toString()
		});

		return `/games?${params.toString()}`;
	}
</script>

<svelte:head>
	<title>Games | PokerBank</title>
</svelte:head>

<section class="page-header">
	<div>
		<h1 class="page-title">Games</h1>
	</div>

	<div class="page-actions">
		{#if openGame}
			<a href={`/games/${openGame.id}`} class="btn btn-primary">Continue open game</a>
		{:else}
			<form method="POST" action="?/createGame">
				<button type="submit" class="btn btn-primary">New game</button>
			</form>
		{/if}
	</div>
</section>

{#if form?.error}
	<p class="alert alert-error">{form.error}</p>
{/if}

<section class="grid-two">
	<div class="card card-pad">
		<div class="section-head">
			<h2 class="section-title">Open</h2>
			<span class="badge badge-open">{openGames.length}</span>
		</div>

		{#if openGames.length === 0}
			<p class="empty-state">No open games.</p>
		{:else}
			<div class="data-list">
				{#each openGames as game}
					<a href={`/games/${game.id}`} class="data-row">
						<div>
							<strong class="row-title">{formatDateTime(game.createdAtUtc)}</strong>
							<p class="row-meta">In progress</p>
						</div>
						<span class="badge badge-open">{game.status}</span>
					</a>
				{/each}
			</div>
		{/if}
	</div>

	<div class="card card-pad">
		<div class="section-head">
			<h2 class="section-title">Closed</h2>
			<span class="badge badge-closed">{closedGames.length}</span>
		</div>

		{#if closedGames.length === 0}
			<p class="empty-state">No closed games.</p>
		{:else}
			<div class="data-list">
				{#each closedGames as game}
					<a href={`/games/${game.id}`} class="data-row">
						<div>
							<strong class="row-title">{formatDateTime(game.createdAtUtc)}</strong>
							<p class="row-meta">Finalized</p>
						</div>
						<span class="badge badge-closed">{game.status}</span>
					</a>
				{/each}
			</div>
		{/if}
	</div>
</section>

{#if gamePage}
	<div class="section-head mt-5">
		<p class="row-meta">Page {totalPages === 0 ? 0 : currentPage} of {totalPages}</p>
		<div class="row-actions">
			{#if hasPreviousPage}
				<a href={pageHref(currentPage - 1)} class="btn btn-secondary">Previous</a>
			{:else}
				<button type="button" class="btn btn-secondary" disabled>Previous</button>
			{/if}
			{#if hasNextPage}
				<a href={pageHref(currentPage + 1)} class="btn btn-secondary">Next</a>
			{:else}
				<button type="button" class="btn btn-secondary" disabled>Next</button>
			{/if}
		</div>
	</div>
{/if}
