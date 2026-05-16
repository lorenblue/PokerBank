<script lang="ts">
	import './layout.css';
	import favicon from '$lib/assets/favicon.svg';

	let { children } = $props();
</script>

<svelte:head><link rel="icon" href={favicon} /></svelte:head>

<div class="min-h-screen">
	<header class="border-b border-slate-200 bg-white">
		<div class="mx-auto flex max-w-6xl flex-col gap-4 px-4 py-4 sm:flex-row sm:items-center sm:justify-between sm:px-8">
			<a href="/" class="text-lg font-extrabold text-emerald-950">PokerBank</a>

			<nav class="flex flex-wrap gap-1 text-sm font-bold text-slate-600">
				<a href="/" class="rounded-md px-3 py-2 hover:bg-slate-100 hover:text-slate-950">Dashboard</a>
				<a href="/games" class="rounded-md px-3 py-2 hover:bg-slate-100 hover:text-slate-950">Games</a>
				<a href="/players" class="rounded-md px-3 py-2 hover:bg-slate-100 hover:text-slate-950">Players</a>
				<a href="/payments" class="rounded-md px-3 py-2 hover:bg-slate-100 hover:text-slate-950">Payments</a>
			</nav>
		</div>
	</header>

	<main class="mx-auto max-w-6xl px-4 py-6 sm:px-8">{@render children()}</main>
</div>
