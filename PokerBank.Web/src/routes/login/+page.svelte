<script lang="ts">
	import type { ActionData } from './$types';

	let { form }: { form: ActionData } = $props();
</script>

<svelte:head>
	<title>Sign in | PokerBank</title>
</svelte:head>

<section class="mx-auto max-w-md">
	<h1 class="mt-1 text-4xl leading-none font-bold tracking-normal sm:text-5xl">Sign in</h1>

	{#if form?.error}
		<p class="mt-4 rounded-lg border border-red-200 bg-red-50 px-4 py-3 text-red-700">
			{form.error}
		</p>
	{/if}

	<form method="POST" class="mt-6 grid gap-4 rounded-lg border border-slate-200 bg-white p-5 shadow-xs">
		<label class="grid gap-1 text-sm font-bold text-slate-700">
			Email
			<input
				name="email"
				type="email"
				autocomplete="email"
				required
				value={form?.email ?? ''}
				class="rounded-md border border-slate-300 px-3 py-2"
			/>
		</label>

		<label class="grid gap-1 text-sm font-bold text-slate-700">
			Password
			<input
				name="password"
				type="password"
				autocomplete="current-password"
				required
				class="rounded-md border border-slate-300 px-3 py-2"
			/>
		</label>

		<button
			type="submit"
			class="rounded-md bg-emerald-900 px-4 py-3 font-bold text-white hover:bg-emerald-950"
		>
			Sign in
		</button>
	</form>
</section>
