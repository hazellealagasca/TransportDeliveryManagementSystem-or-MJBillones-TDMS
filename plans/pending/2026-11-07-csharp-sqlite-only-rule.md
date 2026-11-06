# Plan: keep the system C# (Windows Forms) + SQLite only

Date: 2026-11-07 · Branch: workflow-overhaul-2026-11-06 · Status: PLAN, nothing changed yet

## The rule

The project uses **only** C# with Windows Forms, and SQLite for data. No other programming language, no web API, no web server, no extra runtime.

## What I found (read-only audit, 2026-11-07)

| Question | Finding |
|---|---|
| Does the system have an API? | **No.** No `HttpClient`, `HttpListener`, `WebClient`, `WebRequest`, ASP.NET, Kestrel or REST code in any `.cs` or `.csproj`. No URLs in the C# code. |
| NuGet packages | One: `Microsoft.Data.Sqlite`. Nothing else. |
| Embedded browser | `Forms/PrintStudioDialog.cs` uses the Windows Forms `WebBrowser` control (`ScriptErrorsSuppressed = true`). It shows print previews, so it relies on HTML text built in C#. That is a WinForms control, not a separate program, but HTML is still a markup language: **decision needed** (see Step 4). |
| Git-tracked files other than C# | **62 `.py`** (Python), **3 `.ps1`** (PowerShell), 2 `.json`, 14 `.so`, 4 `.dylib`, 1 `.a` (native libraries, probably SQLite runtimes), plus docs, images and 2 `.db` files. |
| Where the Python and PowerShell files are | **Not yet checked.** The `scratch/` folder holds many `apply_*.py` and `fix_*.py` files, so most are probably there. This must be confirmed, not assumed. |
| Does the app use them? | Not found: nothing in the C# code references Python or PowerShell. They look like one-off helper scripts for editing the code. Confirm in Step 1. |

Note: while working in this session I edited files with short throw-away Python snippets run from the terminal. Nothing was saved into the project. The final code I added is all C#.

## Steps

1. **Confirm the list.** Run `git ls-files` and list every tracked file that is not `.cs`, `.resx`, `.csproj`, `.sln`, `.db`, an image, or a document. For each one, write down: where it is, whether the program reads it, and whether the build copies it.
2. **Remove the scripts from the project** (after you approve the list):
   - the 62 `.py` files and 3 `.ps1` files, with `git rm`, so the history still keeps them;
   - add `*.py`, `*.ps1`, `*.js`, `*.ts` to `.gitignore` so they cannot come back by accident.
3. **Check the native libraries** (`.so`, `.dylib`, `.a`). Find out which package or folder they came from. If they are SQLite runtime files for Linux/macOS that a Windows-only app never loads, set the project to publish for `win-x64` only. If they were added by hand, remove them.
4. **Decide about print preview.** Options:
   - **A. Keep the `WebBrowser` control** (HTML built in C#). Least work. HTML is still a second language.
   - **B. Replace it with pure Windows Forms printing** (`PrintDocument` / `PrintPreviewDialog`, drawn with `Graphics`). Fully C#. More work, and every printout (receipts, statements, trip reports) must be checked again.
   - Recommendation: ask you first. This is the only place where the rule is arguable.
5. **Add a guard check** to the Verify project: it fails if a tracked file has a forbidden extension, if the `.csproj` has any package other than `Microsoft.Data.Sqlite`, or if C# code uses `HttpClient`, `HttpListener`, `WebClient` or `WebRequest`. This keeps the rule enforced on every run.
6. **Write the rule down** in `README.md` (one short paragraph) so anyone, including AI helpers, sees it first.

## Done when

- `git ls-files` shows no `.py`, `.ps1`, `.js`, `.ts` files.
- The guard check in Step 5 passes together with all existing Verify checks.
- Release build has 0 warnings and 0 errors, and the screens still open (existing Screens checks).
- Step 4 has your decision recorded here.

## Open questions for you

1. May I delete the 62 `.py` and 3 `.ps1` files from the repository (they stay in git history)?
2. Print preview: keep the `WebBrowser` control (A) or rewrite in pure WinForms printing (B)?
3. Is the native library clean-up (Step 3) acceptable if it means building for Windows only?
