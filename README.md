# CRONCH! - A web-based job scheduler

## Features

- cron-like scheduling (with additional enhancements)
- web interface for job management and monitoring
- live job insights such as stdout/stderr views
- saving of run histories with full output and timestamps
  - configurable removal of old histories to free up space: time- and/or count-based
- customizable notifications
  - email, Slack, Discord, other webhooks?
  - could notify on: success, failure, warning, parallelism limit, more?
- configurable job parallelism
- configuration and history export/import capability
  - configuration is diff-friendly (TBD whether it's always so or just on export)
  - TBD whether history is plaintext, compressed, SQLite, or something else
- native support for containerization

## Non-features, i.e., things explicitly avoided (at least for MVP)

- logins
  - assume this app is either run on a secured system, or is behind an auth frontend such as Authelia
- multi-server setups
  - lots of complexity, not needed at the moment
  - a job could still be written to execute anywhere using external resources
- audit logs
  - no point without logins
