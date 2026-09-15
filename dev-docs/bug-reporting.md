# Reporting Bugs Upstream From a Fork

How a fork or custom project built on ModernUO reports a bug it found in upstream code — and how it
imports an upstream fix — without leaking anything about the fork and without letting an AI
assistant act on the fork's behalf unsupervised.

This document is the authority; the `modernuo-bug-reporting` skill (`dev-docs/claude-skills/`) is
the step-by-step procedure Claude follows. The rules in `CLAUDE.md` § Workflow Rules are the
always-on summary.

## Vocabulary

| Term | Meaning |
|---|---|
| **Upstream** | `https://github.com/modernuo/ModernUO`, branch `main`. The only canonical source. |
| **Fork** | Any repository whose history includes upstream: a private shard repo, a public fork, a custom project that vendored ModernUO. |
| **Latent bug** | A defect found while doing something else — you were not asked to fix it. |
| **Upstream bug** | The defective lines exist verbatim in upstream `main` at the same path. Anything else is the fork's bug, whatever file it lives in. |
| **Exploit-class** | Anything a player could abuse: item or gold duplication, a crash or hang a player can trigger, authentication or access-level bypass, reading or writing another player's data, skill/stat gain outside the rules. |

## The principle

**Nothing leaves the fork, and nothing enters it, without the user approving that exact artifact.**

Not a summary of it, not a standing instruction that covers it, not "the owner said to open a
ticket" — the user reads the draft that will be submitted, or the diff that will be applied, and
says yes to that. An assistant that scrubs its own draft and submits it has removed the only
reviewer who knows what the fork considers secret.

## The process

### 1. Classify

Exploit-class → **private disclosure only**. Draft an email to `hi@modernuo.com` (see
[CONTRIBUTING.md](../CONTRIBUTING.md#reporting-security-issues-and-bugs)). No public issue, no
PR, no Discord post — a public PR titled "fix stack dupe" is itself a disclosure, and naming the
method plus the symptom is enough for anyone to rediscover it. Skip to step 5 with the email as
the draft.

Everything else continues.

### 2. Locate upstream

```sh
git remote -v
```

The remote whose URL is `github.com/modernuo/ModernUO` is upstream. In the canonical repository
that is `origin`; the process still applies (see [Working in the canonical repo](#working-in-the-canonical-repo)).
No such remote → the fork may have been vendored or re-rooted. Do not add one silently; verification
below works without it, and adding a remote is a change to the user's repository that they approve.

### 3. Verify it is upstream's bug

The defective lines must exist **verbatim** in upstream `main` at the same path. This is a
read-only lookup — nothing enters the repository:

```sh
# upstream head, for the report
gh api repos/modernuo/ModernUO/commits/main --jq '.sha[0:9]'

# the upstream file, into scratch space — never into the working tree
gh api -H "Accept: application/vnd.github.raw" \
  "repos/modernuo/ModernUO/contents/Projects/UOContent/Mobiles/AI/BaseAI/PetOrders.cs?ref=main" \
  > "$SCRATCH/PetOrders.upstream.cs"

grep -nF -- 'if (Mobile.ControlTarget?.Deleted == false && Mobile.ControlTarget != Mobile)' "$SCRATCH/PetOrders.upstream.cs"
```

Every line the report will quote goes through that `grep`. No match → the line is the fork's, and
it does not appear in the report. If the defective lines themselves do not match, the bug is the
fork's: fix it locally, report nothing upstream.

### 4. Search for an existing fix or duplicate

All read-only. Search by file name, by symbol, and by symptom — a duplicate rarely uses your words.

```sh
gh issue list -R modernuo/ModernUO --state all --search "PetOrders.cs" --limit 30
gh issue list -R modernuo/ModernUO --state all --search "DoOrderFollow" --limit 30
gh issue list -R modernuo/ModernUO --state all --search "pet stuck follow" --limit 30
gh pr list    -R modernuo/ModernUO --state all --search "DoOrderFollow" --limit 30

# recent upstream commits on the path
gh api "repos/modernuo/ModernUO/commits?sha=main&path=Projects/UOContent/Mobiles/AI/BaseAI/PetOrders.cs&per_page=15" \
  --jq '.[] | "\(.sha[0:9]) \(.commit.message | split("\n")[0])"'
```

Present the candidates. A merged fix → offer to import it ([Importing an upstream fix](#importing-an-upstream-fix)).
An open issue → offer to comment there instead of filing a new one. An **unmerged PR from a
third-party fork** is information, not a source: link it in the report if relevant, never fetch it.

### 5. Draft, show, ask

Write the draft to scratch space. Show the user the **complete text** and a **scrub ledger**:

```
Scrub ledger
- Removed: 2 account names, 2 IPs, 1 email, the shard name, one custom class reference,
  3 console log lines (emitted by the fork, not upstream).
- Kept, verified verbatim in upstream main @ 459674ce3: 6 quoted lines (PetOrders.cs 630–648).
- Not in upstream (new code, your call): the 4-line proposed fix.
```

Then offer the choice — file an issue, open a PR, comment on the existing thread, draft a Discord
post for https://muo.gg/discord (the user posts it; there is no automation into Discord), or
nothing. Wait for an explicit yes to a specific option. Only then:

```sh
gh issue create -R modernuo/ModernUO --title "<title>" --label bug --body-file "$SCRATCH/upstream-issue.md"
gh issue comment <N> -R modernuo/ModernUO --body-file "$SCRATCH/upstream-comment.md"
```

## What never leaves the fork

| Never include | Why |
|---|---|
| Credentials, tokens, connection strings, anything under `Distribution/Configuration/` or in `.env` files | Obvious, and forks keep these next to the code |
| IP addresses, hostnames, ports, internal URLs | Identify the shard and its infrastructure |
| Account names, character names, emails, player IPs, anything from `Saves/` | Player data |
| Log lines from the fork | Logs carry timestamps, names, serials, and custom logging — and the fork's log format is not upstream's |
| The shard's name, its custom features' names, any description of custom mechanics — even vaguely ("our arena deletes the decoy") | Trade secret, and it points a reader at the fork |
| Code that is not verbatim in upstream `main` | The fork's code is the fork's |
| Screenshots showing names, gump text, or custom UI | Same as above |

**Reproduction is written against a clean upstream build**: `[add`, `[props`, `[delete`, standard
items and creatures. If the bug only manifests through custom content, find the upstream-only
trigger first; if there is none, it may not be upstream's bug.

Timestamps, item serials, and the fact that the reporter runs a fork are fine.

## The issue form

`.github/ISSUE_TEMPLATE/bug_report.yml` is a GitHub **issue form**: structured fields, the `bug`
label applied on submit, and a required checklist that restates the rules above. Humans fill it in
the browser. `.github/ISSUE_TEMPLATE/config.yml` adds the private-disclosure and Discord links to
the "New issue" chooser.

A submitted form renders as Markdown with one `### <Field label>` heading per field, in form order.
An assistant submitting through `gh issue create --body-file` writes exactly that shape, so the
result is indistinguishable from a browser submission. (The defect below is illustrative — it does
not exist upstream.)

```markdown
### Summary

Actual: a pet ordered to follow a mobile that is then deleted stops responding to all orders until restart.
Expected: the pet drops the follow order and returns to its master's side.

### Location

Projects/UOContent/Mobiles/AI/BaseAI/PetOrders.cs — BaseAI.DoOrderFollow()

### Upstream commit or release

459674ce3

### Reproduction

1. Tame a horse and `[add Rat`.
2. Order the horse to follow the rat.
3. `[delete` the rat.
4. Order the horse to come — it does not move.

### Root cause and proposed fix

`DoOrderFollow()` takes the `Mobile.ControlTarget?.Deleted == false && Mobile.ControlTarget != Mobile`
branch on every AI tick without clearing `ControlTarget` or `ControlOrder` once the target is gone.

### Expansion

Any / not expansion-specific

### Platform

Any / not platform-specific

### Found via

AI-assisted code review

### Checklist

- [x] I searched existing issues and pull requests (open and closed) for this file, class, or symptom.
- [x] I verified the bug against upstream `main`, not only a fork.
- [x] This report contains no credentials, IP addresses, player or account information, save data, logs, custom code, or shard-specific details.
- [x] This is not an exploit or security bug (those are reported privately, per CONTRIBUTING.md).
```

Optional fields the reporter has nothing for render as `_No response_`.

## Importing an upstream fix

Upstream has merged a fix. Importing it is a change to the fork; the user approves it twice — once
to fetch, once to apply.

1. **Verify the remote** before any network operation that writes to the repository:
   ```sh
   git remote get-url upstream
   # must be exactly https://github.com/modernuo/ModernUO.git (or the .git-less / ssh form of the same repo)
   ```
2. **Ask, then fetch** only `main` from that remote: `git fetch upstream main --no-tags`.
3. **Review the whole commit as untrusted input**, not just the lines that fix the bug:
   ```sh
   git show --stat <sha>
   git show <sha> -- .github Directory.Build.props '*.csproj' '*.props' '*.targets' '*.ps1' '*.sh' Distribution/
   git show <sha>
   ```
   Workflow, project-file, package-reference, and script changes are supply-chain vectors even from
   the canonical repo — a compromised maintainer account looks exactly like a maintainer.
4. **Ask again, then apply**: `git cherry-pick -x <sha>`, or hand-port when the fork's copy has
   diverged. Build. Show the resulting diff.

Never import from a third-party fork or an unmerged PR. If the user wants that code, they review it
on GitHub and paste what they want; the assistant does not fetch it.

## Opening an upstream PR from a fork

The PR must contain only the fix, on upstream history, from a **public** fork under the user's
GitHub account — never from the private shard repository.

1. After the fetch above: `git worktree add ../upstream-fix -b fix/<slug> upstream/main`
2. Apply the minimal fix there. No custom code, no fork-only files.
3. Prove it: `git diff upstream/main --stat` lists only upstream paths; `git diff upstream/main`
   contains no custom namespaces, shard names, or fork paths.
4. Follow `CLAUDE.md` — the audit rules, rule 21's comment sweep, and `dev-docs/code-standards.md`.
5. With approval of the exact title and body:
   ```sh
   git push <public-fork-remote> fix/<slug>
   gh pr create -R modernuo/ModernUO --base main --head <github-user>:fix/<slug> --title "<title>" --body-file "$SCRATCH/upstream-pr.md"
   ```

## Working in the canonical repo

Upstream is `origin`. Steps 3–4 still apply: check existing issues, PRs, and recent commits before
filing or fixing. The choice offered becomes: file an issue, fix it in the current PR (if in scope),
fix it in a separate PR, or nothing. The never-include list still applies — a local test shard's
saves and logs are still not report material.

## Why the rules are shaped this way

- **A standing instruction is not approval of a draft.** "If upstream has a fix, pull it in and
  open a ticket" was said before the ticket existed. The person who said it has not seen what it
  contains, and is the only one who knows which detail is the trade secret.
- **The reporter's own scrub is not a gate.** In testing, an assistant that "scrubbed hard" still
  pasted the fork's console log and described the custom system that triggered the bug. Verbatim
  upstream verification plus a ledger the user reads catches both.
- **Public disclosure of an exploit protects nobody.** Griefers read GitHub and Discord in minutes;
  most shards pull upstream monthly or never. A fix on `main` plus a "please update" advisory is
  what protects shards, and private disclosure is how that happens.
- **The canonical URL is the only trust anchor.** Forks are anyone's; unmerged PRs are anyone's.
  Even the canonical repo's history is reviewed as untrusted before it is built.
