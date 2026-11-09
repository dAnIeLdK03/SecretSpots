# Assistant eval

Measures how well the spot assistant answers, so a model change (Ollama `qwen2.5:7b` → `3b` → Claude Haiku) is a number instead of a feeling.

`questions.json` has ~23 bg/en questions with automatic checks: expected places appear, pending/rejected spots never do, "no result" and prompt-injection questions return no places and leak no tool names, the answer language matches, and no raw `[spot:…]` marker leaks.

## Run

1. Start the app (Aspire) with sample data seeded and `Assistant:Enabled=true`, with the model you want to measure (`Assistant:Model`).
2. `node scripts/assistant-eval/eval.mjs --api http://localhost:5000 --label qwen2.5-7b`

Options: `--only id1,id2` runs some questions; env `EVAL_PASSWORD` overrides the sample password. Questions are spread over several sample accounts because each has 30 questions a day.

Results go to `results/<label>.json` (git-ignored) with each answer, so two labels can be compared. A local CPU model takes roughly 30–110 s per answer; Haiku costs a few cents for a full run.

## Limits

- Checks are substring based and a "pass" is not proof of a good answer. Read the failures and a few passes in the JSON.
- Rating-dependent and distance questions rely on the seeded data; if the seeder changes, update `questions.json`.
- Injection through *data* (a spot description containing instructions) isn't automated: create such a spot as an admin, approve it, and ask about it.
