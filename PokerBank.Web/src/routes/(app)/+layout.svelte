<script lang="ts">
	import type { LayoutData } from './$types';

	let { children, data }: { children: import('svelte').Snippet; data: LayoutData } = $props();
</script>

<div class="min-h-screen">
	<header class="border-b border-slate-200 bg-white">
		<div class="mx-auto flex max-w-6xl flex-col gap-4 px-4 py-4 sm:flex-row sm:items-center sm:justify-between sm:px-8">
			<a href="/" class="text-lg font-extrabold text-emerald-950">PokerBank</a>

			<nav class="flex flex-wrap gap-1 text-sm font-bold text-slate-600">
				<a href="/" class="rounded-md px-3 py-2 hover:bg-slate-100 hover:text-slate-950">
					{data.isManager ? 'Dashboard' : 'My Summary'}
				</a>
				<a href="/games" class="rounded-md px-3 py-2 hover:bg-slate-100 hover:text-slate-950">
					{data.isManager ? 'Games' : 'My Games'}
				</a>
				{#if data.isManager}
					<a href="/players" class="rounded-md px-3 py-2 hover:bg-slate-100 hover:text-slate-950">Players</a>
				{/if}
				<a href="/payments" class="rounded-md px-3 py-2 hover:bg-slate-100 hover:text-slate-950">
					{data.isManager ? 'Payments' : 'My Payments'}
				</a>
				<form method="POST" action="/logout">
					<button type="submit" class="rounded-md px-3 py-2 hover:bg-slate-100 hover:text-slate-950">
						Sign out
					</button>
				</form>
			</nav>
		</div>
	</header>

	<main class="mx-auto max-w-6xl px-4 py-6 sm:px-8">{@render children()}</main>
</div>
