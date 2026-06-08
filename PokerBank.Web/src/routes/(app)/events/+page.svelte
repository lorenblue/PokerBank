<script lang="ts">
	import Modal from '$lib/components/Modal.svelte';
	import { formatDateTime } from '$lib/format';
	import type { PokerEvent } from '$lib/api/client';
	import type { PageData } from './$types';

	type FormData = {
		cancelled?: boolean;
		created?: boolean;
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

	let isCreateOpen = $state(false);
	let scheduledAtLocal = $state(toDateTimeLocalInput(new Date(Date.now() + 7 * 24 * 60 * 60 * 1000)));
	const scheduledAtUtc = $derived(scheduledAtLocal ? new Date(scheduledAtLocal).toISOString() : '');

	const scheduledEvents = $derived(data.events.filter((event) => event.status === 'Scheduled'));
	const cancelledEvents = $derived(data.events.filter((event) => event.status === 'Cancelled'));

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

	function eventCounts(event: PokerEvent) {
		return `Going ${event.goingCount} · Maybe ${event.maybeCount} · Not going ${event.notGoingCount}`;
	}

	function canStartGame(event: PokerEvent) {
		return new Date(event.scheduledAtUtc).getTime() <= Date.now();
	}

	function closeCreateModal() {
		isCreateOpen = false;
	}

	function toDateTimeLocalInput(date: Date) {
		const year = date.getFullYear();
		const month = pad(date.getMonth() + 1);
		const day = pad(date.getDate());
		const hour = pad(date.getHours());
		const minute = pad(date.getMinutes());

		return `${year}-${month}-${day}T${hour}:${minute}`;
	}

	function pad(value: number) {
		return value.toString().padStart(2, '0');
	}
</script>

<svelte:head>
	<title>Events | PokerBank</title>
</svelte:head>

<section class="page-header">
	<div>
		<h1 class="page-title">Events</h1>
	</div>

	{#if data.isManager}
		<div class="page-actions">
			<button type="button" class="btn btn-primary" onclick={() => (isCreateOpen = true)}>
				New event
			</button>
		</div>
	{/if}
</section>

{#if form?.error}
	<p class="alert alert-error">{form.error}</p>
{:else if form?.created}
	<p class="alert alert-success">Event created.</p>
{:else if form?.cancelled}
	<p class="alert alert-success">Event cancelled.</p>
{:else if form?.rsvpStatus}
	<p class="alert alert-success">RSVP set to {rsvpLabel(form.rsvpStatus)}.</p>
{/if}

<section class="grid-two">
	<div class="card card-pad">
		<div class="section-head">
			<h2 class="section-title">Scheduled</h2>
			<span class="badge badge-open">{scheduledEvents.length}</span>
		</div>

		{#if scheduledEvents.length === 0}
			<p class="empty-state">No scheduled events.</p>
		{:else}
			<div class="data-list">
				{#each scheduledEvents as event}
					<div class="event-row">
						<div class="event-main">
							<div class="event-title-row">
								<strong class="row-title">{event.title}</strong>
								{#if !data.isManager}
									<span class={`badge ${rsvpClass(event.myRsvpStatus)}`}>{rsvpLabel(event.myRsvpStatus)}</span>
								{/if}
							</div>
							<p class="row-meta">{formatDateTime(event.scheduledAtUtc)}</p>
							<p class="row-meta">{eventCounts(event)}</p>
						</div>

						{#if data.isManager}
							<div class="event-actions">
								{#if event.gameId}
									<a href={`/games/${event.gameId}`} class="btn btn-secondary">Open game</a>
								{:else if canStartGame(event)}
									<form method="POST" action="?/startGame">
										<input type="hidden" name="eventId" value={event.id} />
										<button type="submit" class="btn btn-primary">Start game</button>
									</form>
								{:else}
									<button type="button" class="btn btn-secondary" disabled>Not started</button>
								{/if}

								{#if !event.gameId}
									<form
										method="POST"
										action="?/cancelEvent"
										onsubmit={(submitEvent) => {
											if (!confirm('Cancel this event?')) submitEvent.preventDefault();
										}}
									>
										<input type="hidden" name="eventId" value={event.id} />
										<button type="submit" class="btn btn-subtle-danger">Cancel</button>
									</form>
								{/if}
							</div>
						{:else}
							<div class="rsvp-actions" aria-label={`RSVP for ${event.title}`}>
								<form method="POST" action="?/setRsvp">
									<input type="hidden" name="eventId" value={event.id} />
									<input type="hidden" name="status" value="Going" />
									<button
										type="submit"
										class={`rsvp-button ${event.myRsvpStatus === 'Going' ? 'is-selected' : ''}`}
									>
										Going
									</button>
								</form>
								<form method="POST" action="?/setRsvp">
									<input type="hidden" name="eventId" value={event.id} />
									<input type="hidden" name="status" value="Maybe" />
									<button
										type="submit"
										class={`rsvp-button ${event.myRsvpStatus === 'Maybe' ? 'is-selected' : ''}`}
									>
										Maybe
									</button>
								</form>
								<form method="POST" action="?/setRsvp">
									<input type="hidden" name="eventId" value={event.id} />
									<input type="hidden" name="status" value="NotGoing" />
									<button
										type="submit"
										class={`rsvp-button ${event.myRsvpStatus === 'NotGoing' ? 'is-selected is-negative' : ''}`}
									>
										Not going
									</button>
								</form>
							</div>
						{/if}
					</div>
				{/each}
			</div>
		{/if}
	</div>

	<div class="card card-pad">
		<div class="section-head">
			<h2 class="section-title">Cancelled</h2>
			<span class="badge badge-closed">{cancelledEvents.length}</span>
		</div>

		{#if cancelledEvents.length === 0}
			<p class="empty-state">No cancelled events.</p>
		{:else}
			<div class="data-list">
				{#each cancelledEvents as event}
					<div class="data-row">
						<div>
							<strong class="row-title">{event.title}</strong>
							<p class="row-meta">{formatDateTime(event.scheduledAtUtc)}</p>
							<span class={`badge ${statusClass(event.status)}`}>{event.status}</span>
						</div>
						{#if event.gameId}
							<a href={`/games/${event.gameId}`} class="btn btn-secondary">Open game</a>
						{/if}
					</div>
				{/each}
			</div>
		{/if}
	</div>
</section>

{#if isCreateOpen}
	<Modal title="Create event" onClose={closeCreateModal}>
		<form method="POST" action="?/createEvent" class="form-grid">
			<label class="field">
				Title
				<input name="title" required maxlength="120" placeholder="Friday poker" />
			</label>

			<label class="field">
				Scheduled time
				<input type="datetime-local" required bind:value={scheduledAtLocal} />
			</label>

			<input type="hidden" name="scheduledAtUtc" value={scheduledAtUtc} />

			<div class="form-actions">
				<button type="button" class="btn btn-secondary" onclick={closeCreateModal}>
					Cancel
				</button>
				<button type="submit" class="btn btn-primary">
					Create event
				</button>
			</div>
		</form>
	</Modal>
{/if}

<style>
	.event-row {
		display: grid;
		gap: 0.85rem;
		border: 1px solid #e8eee9;
		border-radius: 0.65rem;
		background: #fff;
		padding: 0.9rem;
	}

	.event-title-row {
		display: flex;
		flex-wrap: wrap;
		align-items: center;
		gap: 0.5rem;
		justify-content: space-between;
	}

	.event-actions {
		display: flex;
		flex-wrap: wrap;
		gap: 0.5rem;
		justify-content: flex-end;
	}

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
		min-height: 2.35rem;
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
