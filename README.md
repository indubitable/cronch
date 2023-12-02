# CRONCH! - A web-based job scheduler

## Current status

MVP achieved! It still shouldn't be considered stable, however. [Container images are available in GHCR.](https://github.com/indubitable/cronch/pkgs/container/cronch)

Next steps:

- further testing/bugfixing (ongoing)
- polishing UI; improving UX (ongoing)
- introducing HTMX for live refresh capabilities (partially completed)
- caching to reduce overall load when retrieving filesystem data

## Features

- ✅ cron-like scheduling (with additional enhancements)
- ✅ web interface for job management and monitoring
- ✅ live job insights such as stdout/stderr views
- ✅ saving of run histories with full output and timestamps
  - ✅ configurable removal of old histories to free up space: time- and/or count-based
- ✅ customizable post-run actions
  - ✅ custom script can ingest run metadata and act on it
  - (script could notify via email, Slack, Discord, other webhooks, etc.)
- ✅ configurable job parallelism
- ✅ configuration and history export/import capability
  - ✅ configuration is a diff-friendly XML file
  - ✅ history is SQLite, with stdout/stderr as minimally formatted text
- ✅ native support for containerization

## Non-features, i.e., things explicitly avoided (at least for MVP)

- logins
  - assume this app is either run on a secured system, or is behind an auth frontend such as Authelia
- multi-server setups
  - lots of complexity, not needed at the moment
  - a job could still be written to execute anywhere using external resources
- audit logs
  - no point without logins
