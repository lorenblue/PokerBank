<script lang="ts">
	import { page } from '$app/state';
	import type { LayoutData } from './$types';

	let { children, data }: { children: import('svelte').Snippet; data: LayoutData } = $props();

	const navItems = $derived([
		{ href: '/', label: data.isManager ? 'Dashboard' : 'My Summary' },
		{ href: '/games', label: data.isManager ? 'Games' : 'My Games' },
		...(data.isManager ? [{ href: '/players', label: 'Players' }] : []),
		{ href: '/payments', label: data.isManager ? 'Payments' : 'My Payments' }
	]);

	function isActive(href: string) {
		return href === '/' ? page.url.pathname === href : page.url.pathname.startsWith(href);
	}
</script>

<div class="app-shell">
	<aside class="app-sidebar">
		<a href="/" class="brand">
			<span class="brand-mark">PB</span>
			<span>PokerBank</span>
		</a>

		<p class="sidebar-label">{data.isManager ? 'Manager' : 'Member'}</p>
		<nav class="nav-list" aria-label="Primary">
			{#each navItems as item}
				<a href={item.href} class={`nav-link ${isActive(item.href) ? 'is-active' : ''}`}>{item.label}</a>
			{/each}
		</nav>

		<p class="sidebar-label">Session</p>
		<form method="POST" action="/logout">
			<button type="submit" class="nav-link">Sign out</button>
		</form>
	</aside>

	<header class="mobile-topbar">
		<div class="mobile-topbar-inner">
			<a href="/" class="brand">
				<span class="brand-mark">PB</span>
				<span>PokerBank</span>
			</a>

			<nav class="mobile-nav" aria-label="Primary">
				{#each navItems as item}
					<a href={item.href} class={`nav-link ${isActive(item.href) ? 'is-active' : ''}`}>{item.label}</a>
				{/each}
				<form method="POST" action="/logout">
					<button type="submit" class="nav-link">Sign out</button>
				</form>
			</nav>
		</div>
	</header>

	<main class="app-main">
		<div class="content-frame">
			<div class="content-inner">{@render children()}</div>
		</div>
	</main>
</div>
