# `sts-2-board-game` Agent Instructions

## Version control

After every user prompt that changes this repository:

1. Run `./check.sh` and make sure it passes.
2. Commit all in-scope changes with a descriptive commit message.
3. Push the commit to the current branch's configured upstream.

Do not create empty commits for prompts that make no repository changes. Do not
include unrelated pre-existing worktree changes in the commit. If the checks,
the commit, or the push fails, report the failure instead of claiming the task
is complete.
