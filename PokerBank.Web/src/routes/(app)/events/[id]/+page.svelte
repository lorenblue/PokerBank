<script lang="ts">
	import StatCard from '$lib/components/StatCard.svelte';
	import { formatDateTime } from '$lib/format';
	import type { PageData } from './$types';

	type FormData = {
		cancelled?: boolean;
		error?: string;
		rsvpStatus?: string;
	};

	let {
		data,
		form
	}: {
		data: PageData;
		form: FormData | null;
	} = $props();

	const pokerEvent = $derived(data.event);
	const canStartGame = $derived(
		pokerEvent.status === 'Scheduled' &&
			pokerEvent.gameId === null &&
			new Date(pokerEvent.scheduledAtUtc).getTime() <= Date.now()
	);
	const canCancel = $derived(pokerEvent.status === 'Scheduled' && pokerEvent.gameId === null);
	const canRsvp = $derived(pokerEvent.status === 'Scheduled');

	function rsvpLabel(status: string | null | undefined) {
		if (status === 'Going') return 'Going';
		if (status === 'Maybe') return 'Maybe';
		if (status === 'NotGoing') return 'Not going';

		return 'No RSVP';
	}

	function statusClass(status: string) {
		if (status === 'Cancelled') return 'badge-closed';

		return 'badge-open';
	}

	function rsvpClass(status: string | null | undefined) {
		if (status === 'Going') return 'badge-open';
		if (status === 'NotGoing') return 'badge-closed';

		return '';
	}
</script>

<svelte:head>
	<title>{pokerEvent.title} | PokerBank</title>
</svelte:head>

<a href="/events" class="back-link">Back to events</a>

<section class="page-header">
	<div>
		<span class={`badge ${statusClass(pokerEvent.status)}`}>{pokerEvent.status}</span>
		<h1 class="page-title">{pokerEvent.title}</h1>
		<p class="page-subtitle">{formatDateTime(pokerEvent.scheduledAtUtc)}</p>
	</div>

	<div class="page-actions">
		{#if data.isManager}
			{#if pokerEvent.gameId}
				<a href={`/games/${pokerEvent.gameId}`} class="btn btn-primary">Open game</a>
			{:else if canStartGame}
				<form method="POST" action="?/startGame">
					<button type="submit" class="btn btn-primary">Start game</button>
				</form>
			{:else if pokerEvent.status === 'Scheduled'}
				<button type="button" class="btn btn-secondary" disabled>Not started</button>
			{/if}

			{#if canCancel}
				<form
					method="POST"
					action="?/cancelEvent"
					onsubmit={(submitEvent) => {
						if (!confirm('Cancel this event?')) submitEvent.preventDefault();
					}}
				>
					<button type="submit" class="btn btn-subtle-danger">Cancel</button>
				</form>
			{/if}
		{:else if pokerEvent.gameId}
			<a href={`/games/${pokerEvent.gameId}`} class="btn btn-primary">Open game</a>
		{/if}
	</div>
</section>

{#if form?.error}
	<p class="alert alert-error">{form.error}</p>
{:else if form?.cancelled}
	<p class="alert alert-success">Event cancelled.</p>
{:else if form?.rsvpStatus}
	<p class="alert alert-success">RSVP set to {rsvpLabel(form.rsvpStatus)}.</p>
{/if}

<section class="stat-grid">
	<StatCard label="Going" value={String(pokerEvent.goingCount)} tone="positive" />
	<StatCard label="Maybe" value={String(pokerEvent.maybeCount)} />
	<StatCard label="Not going" value={String(pokerEvent.notGoingCount)} tone="negative" />
</section>

<section class="grid-main">
	<div class="card card-pad">
		<div class="section-head">
			<div>
				<h2 class="section-title">RSVPs</h2>
				<p class="row-meta">Responses from linked players.</p>
			</div>
			{#if !data.isManager}
				<span class={`badge ${rsvpClass(pokerEvent.myRsvpStatus)}`}>{rsvpLabel(pokerEvent.myRsvpStatus)}</span>
			{/if}
		</div>

		{#if pokerEvent.rsvps.length === 0}
			<p class="empty-state">No responses yet.</p>
		{:else}
			<div class="data-list">
				{#each pokerEvent.rsvps as rsvp}
					<div class="data-row">
						<div>
							<strong class="row-title">{rsvp.playerName}</strong>
							<p class="row-meta">Responded {formatDateTime(rsvp.respondedAtUtc)}</p>
						</div>
						<span class={`badge ${rsvpClass(rsvp.status)}`}>{rsvpLabel(rsvp.status)}</span>
					</div>
				{/each}
			</div>
		{/if}
	</div>

	<div class="card card-pad">
		<div class="section-head">
			<h2 class="section-title">{data.isManager ? 'Game' : 'My RSVP'}</h2>
			{#if !data.isManager}
				<span class={`badge ${rsvpClass(pokerEvent.myRsvpStatus)}`}>{rsvpLabel(pokerEvent.myRsvpStatus)}</span>
			{/if}
		</div>

		{#if data.isManager}
			{#if pokerEvent.gameId}
				<div class="empty-state">
					<strong>Game linked</strong>
					<p class="row-meta">This event already has a game.</p>
				</div>
			{:else if canStartGame}
				<p class="empty-state">Ready to start.</p>
			{:else if pokerEvent.status === 'Scheduled'}
				<p class="empty-state">Game can start at the scheduled time.</p>
			{:else}
				<p class="empty-state">Cancelled events cannot start games.</p>
			{/if}
		{:else if !canRsvp}
			<p class="empty-state">RSVPs are closed for this event.</p>
		{:else}
			<div class="rsvp-actions" aria-label={`RSVP for ${pokerEvent.title}`}>
				<form method="POST" action="?/setRsvp">
					<input type="hidden" name="status" value="Going" />
					<button
						type="submit"
						class={`rsvp-button ${pokerEvent.myRsvpStatus === 'Going' ? 'is-selected' : ''}`}
					>
						Going
					</button>
				</form>
				<form method="POST" action="?/setRsvp">
					<input type="hidden" name="status" value="Maybe" />
					<button
						type="submit"
						class={`rsvp-button ${pokerEvent.myRsvpStatus === 'Maybe' ? 'is-selected' : ''}`}
					>
						Maybe
					</button>
				</form>
				<form method="POST" action="?/setRsvp">
					<input type="hidden" name="status" value="NotGoing" />
					<button
						type="submit"
						class={`rsvp-button ${pokerEvent.myRsvpStatus === 'NotGoing' ? 'is-selected is-negative' : ''}`}
					>
						Not going
					</button>
				</form>
			</div>
		{/if}
	</div>
</section>

<style>
	.rsvp-actions {
		display: grid;
		grid-template-columns: repeat(3, minmax(0, 1fr));
		overflow: hidden;
		border: 1px solid var(--border-strong);
		border-radius: 0.65rem;
		background: var(--surface-muted);
	}

	.rsvp-actions form {
		display: contents;
	}

	.rsvp-button {
		min-height: 2.6rem;
		border: 0;
		border-right: 1px solid var(--border-strong);
		background: transparent;
		color: #334139;
		font-weight: 900;
	}

	.rsvp-actions form:last-child .rsvp-button {
		border-right: 0;
	}

	.rsvp-button:hover {
		background: white;
	}

	.rsvp-button.is-selected {
		background: var(--brand);
		color: white;
	}

	.rsvp-button.is-selected.is-negative {
		background: var(--danger);
	}

	@media (max-width: 900px) {
		.rsvp-actions {
			grid-template-columns: 1fr;
		}

		.rsvp-button {
			border-right: 0;
			border-bottom: 1px solid var(--border-strong);
		}

		.rsvp-actions form:last-child .rsvp-button {
			border-bottom: 0;
		}
	}
</style>
