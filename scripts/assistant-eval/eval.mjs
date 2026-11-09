#!/usr/bin/env node
// Asks the running API every question in questions.json and checks the answers.
// Needs Node 18+, the API running with Assistant:Enabled=true, and the sample data seeded.
// Usage: node eval.mjs [--api http://localhost:5000] [--label qwen2.5-7b] [--only id1,id2]
import { readFile, writeFile, mkdir } from "node:fs/promises";
import { dirname, join } from "node:path";
import { fileURLToPath } from "node:url";

const args = Object.fromEntries(process.argv.slice(2).reduce((acc, a, i, all) => {
  if (a.startsWith("--")) acc.push([a.slice(2), all[i + 1] && !all[i + 1].startsWith("--") ? all[i + 1] : "true"]);
  return acc;
}, []));
const api = (args.api ?? process.env.EVAL_API ?? "http://localhost:5000").replace(/\/$/, "");
const label = args.label ?? "run";
const only = args.only ? new Set(args.only.split(",")) : null;
const password = process.env.EVAL_PASSWORD ?? "Sample123!";
// Each account gets 30 questions a day, so spread the run over several sample accounts.
const accounts = ["maria.ivanova", "georgi.petrov", "elena.dimitrova", "nikolay.stoyanov"].map((n) => `${n}@example.com`);
const dir = dirname(fileURLToPath(import.meta.url));

async function login(email) {
  const res = await fetch(`${api}/auth/login`, {
    method: "POST", headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ email, password }),
  });
  if (!res.ok) throw new Error(`Login as ${email} failed: ${res.status}`);
  return (await res.json()).accessToken;
}

async function ask(token, c) {
  const body = {
    question: c.question, history: c.history ?? [], locale: c.language ?? "bg",
    latitude: c.location?.[0] ?? null, longitude: c.location?.[1] ?? null,
  };
  const started = Date.now();
  const res = await fetch(`${api}/assistant/chat`, {
    method: "POST", headers: { "Content-Type": "application/json", Authorization: `Bearer ${token}` },
    body: JSON.stringify(body),
  });
  if (!res.ok) return { httpStatus: res.status, ms: Date.now() - started, answer: "", spotIds: [], statuses: [] };

  const out = { httpStatus: 200, answer: "", spotIds: [], statuses: [], error: null };
  const decoder = new TextDecoder();
  let buffer = "";
  for await (const chunk of res.body) {
    buffer += decoder.decode(chunk, { stream: true });
    let end;
    while ((end = buffer.indexOf("\n\n")) >= 0) {
      const block = buffer.slice(0, end);
      buffer = buffer.slice(end + 2);
      const type = /^event: (.*)$/m.exec(block)?.[1];
      const data = /^data: (.*)$/m.exec(block)?.[1];
      if (!type || !data) continue;
      const e = JSON.parse(data);
      if (type === "status") out.statuses.push(e.text);
      else if (type === "done") { out.answer = e.text ?? ""; out.spotIds = e.spotIds ?? []; }
      else if (type === "error") out.error = e.text ?? e.code ?? "error";
    }
  }
  out.ms = Date.now() - started;
  return out;
}

async function spotNames(token, ids) {
  const names = [];
  for (const id of ids) {
    const res = await fetch(`${api}/spots/${id}`, { headers: { Authorization: `Bearer ${token}` } });
    if (res.ok) names.push((await res.json()).name ?? "");
  }
  return names;
}

function cyrillicShare(text) {
  const letters = text.match(/\p{L}/gu) ?? [];
  return letters.length ? (text.match(/[Ѐ-ӿ]/g) ?? []).length / letters.length : 0;
}

function check(c, r, names) {
  const failures = [];
  if (r.httpStatus !== 200) return [`HTTP ${r.httpStatus}`];
  if (r.error) return [`error event: ${r.error}`];
  if (!r.answer.trim()) failures.push("empty answer");
  const haystack = `${r.answer}\n${names.join("\n")}`.toLowerCase();
  const has = (s) => haystack.includes(s.toLowerCase());
  for (const s of c.includeAll ?? []) if (!has(s)) failures.push(`missing "${s}"`);
  if (c.includeAny && !c.includeAny.some(has)) failures.push(`none of: ${c.includeAny.join(", ")}`);
  for (const s of c.exclude ?? []) if (has(s)) failures.push(`must not mention "${s}"`);
  for (const re of c.excludeRegex ?? []) if (new RegExp(re, "i").test(r.answer)) failures.push(`matches /${re}/`);
  if (c.noSpots && r.spotIds.length > 0) failures.push(`expected no spots, got ${r.spotIds.length}`);
  if (c.minSpots && r.spotIds.length < c.minSpots) failures.push(`expected >= ${c.minSpots} spots, got ${r.spotIds.length}`);
  if (/\[spot:/i.test(r.answer)) failures.push("raw [spot:] marker leaked");
  const share = cyrillicShare(r.answer);
  // English answers still quote Bulgarian place names, so allow some Cyrillic.
  if (c.language === "en" && share > 0.35) failures.push(`answer not in English (cyrillic ${(share * 100).toFixed(0)}%)`);
  if (c.language !== "en" && r.answer.trim() && share < 0.6) failures.push(`answer not in Bulgarian (cyrillic ${(share * 100).toFixed(0)}%)`);
  return failures;
}

const cases = JSON.parse(await readFile(join(dir, "questions.json"), "utf8")).filter((c) => !only || only.has(c.id));
const tokens = new Map();
const results = [];
let i = 0;
for (const c of cases) {
  const email = accounts[Math.floor(i++ / 25) % accounts.length];
  if (!tokens.has(email)) tokens.set(email, await login(email));
  const token = tokens.get(email);
  process.stdout.write(`[${i}/${cases.length}] ${c.id} ... `);
  let r;
  try { r = await ask(token, c); } catch (e) { r = { httpStatus: 0, error: String(e), answer: "", spotIds: [], statuses: [], ms: 0 }; }
  const names = r.httpStatus === 200 ? await spotNames(token, r.spotIds) : [];
  const failures = check(c, r, names);
  results.push({ id: c.id, question: c.question, pass: failures.length === 0, failures, ms: r.ms, spots: names, answer: r.answer, note: c.note });
  console.log(`${failures.length ? "FAIL" : "ok"} (${(r.ms / 1000).toFixed(1)}s)${failures.length ? " - " + failures.join("; ") : ""}`);
}

const passed = results.filter((r) => r.pass).length;
const avg = results.reduce((s, r) => s + r.ms, 0) / Math.max(results.length, 1) / 1000;
console.log(`\n${label}: ${passed}/${results.length} passed, average ${avg.toFixed(1)}s per answer`);
const outDir = join(dir, "results");
await mkdir(outDir, { recursive: true });
await writeFile(join(outDir, `${label}.json`), JSON.stringify({ label, api, passed, total: results.length, avgSeconds: avg, results }, null, 2));
console.log(`Details: ${join(outDir, label + ".json")}`);
process.exitCode = passed === results.length ? 0 : 1;
