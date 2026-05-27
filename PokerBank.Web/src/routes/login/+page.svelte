<script lang="ts">
	import type { ActionData } from './$types';

	let { form }: { form: ActionData } = $props();
</script>

<svelte:head>
	<title>Sign in | PokerBank</title>
</svelte:head>

<section class="mx-auto grid min-h-screen max-w-md content-center px-4 py-12">
	<div class="mb-6">
		<a href="/" class="brand">
			<span class="brand-mark">PB</span>
			<span>PokerBank</span>
		</a>
	</div>

	<div class="card card-pad">
		<h1 class="page-title">Sign in</h1>

		{#if form?.error}
			<p class="alert alert-error mt-4">{form.error}</p>
		{/if}

		<form method="POST" class="form-grid mt-6">
			<label class="field">
				Email
				<input name="email" type="email" autocomplete="email" required value={form?.email ?? ''} />
			</label>

			<label class="field">
				Password
				<input name="password" type="password" autocomplete="current-password" required />
			</label>

			<button type="submit" class="btn btn-primary">Sign in</button>
		</form>
	</div>
</section>
