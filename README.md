![CRONCH!](https://github.com/indubitable/cronch/assets/344911/e091490d-fa64-423f-8153-585bd259dab3)

# A web-based job scheduler

## What is it?

CRONCH! is a cron-like job scheduler. It has a user-friendly web interface and convenient features.

## Features

- A six-part (with seconds) cron schedule using [Cronos](https://github.com/HangfireIO/Cronos)
- Web interface for configuration and monitoring
- Configuration is SCM- and diff-friendly
  - Configuration is stored as an XML file
  - Deploying CRONCH! configurations is as easy as copying this one file
- Advanced job configuration options
  - Specify a script and the application to execute that script (e.g., Bash, Perl, etc.)
  - Optionally enforce a time limit
  - Control parallel execution limits
  - Mark job executions as Warning or Error based on keywords found in stdout/stderr
- Post-run scripting
  - Set up a script to execute when any job succeeds, fails, etc.
  - Script can be used for notifications, cleanup, or anything else
- Execution history is saved
  - Start/stop times, status, and stdout/stderr are saved for every execution
  - Output timestamps are included in stdout/stderr
  - Old executions can optionally be removed based on count or age
- Live insights
  - See which jobs are currently executing
  - Inspect stdout/stderr during, and after, execution
- Natively containerized for painless deployment

## Non-features (i.e., things purposely avoided, at least for now)

- User management
  - CRONCH! should be running on an isolated system if security is a concern
  - Running behind an authorization frontend such as Authelia is an option as well
- Multi-server setups
  - Supporting the ability to execute jobs on multiple systems would be very complex and is not needed at the moment
  - It is possible to manually configure a script to execute remote commands via SSH, etc.
- Audit logs
  - Minimal amount of settings doesn't warrant this
  - Lack of user management would make this pointless

## Installation

### Docker images

Docker is by far the easiest way to install CRONCH! at the moment. The following images are provided:

`ghcr.io/indubitable/cronch` - [see details](https://github.com/indubitable/cronch/pkgs/container/cronch)

This is the standard CRONCH! image, based on Debian. It's a multi-arch image with support for amd64 (x86_64), arm64, and armv7. It comes with Bash and Perl for scripting.

`ghcr.io/indubitable/cronch-extra` - [see details](https://github.com/indubitable/cronch/pkgs/container/cronch-extra)

This is a larger CRONCH! image, also based on Debian and also multi-arch. In addition to Bash and Perl, it supports Node, Python, Ruby, PHP, and PowerShell Core (pwsh) for scripting.

The sizes of the two images are significantly different. On amd64, standard is under 300MB, while "extra" is over 1GB.

### Configuration

By default, CRONCH! listens on port 8080 inside the container. Of course, that can be mapped to any external port. Additionally, two volumes should be mounted: one for configuration, and one for historical data storage. They are `/opt/cronch/cronchconfig` and `/opt/cronch/cronchdata`, respectively.

Using plain Docker with filesystem mounts, the command might look like this:

```shell
docker run --restart=always --name=cronch -p 8080:8080 -v ./cronchconfig:/opt/cronch/cronchconfig -v ./cronchdata:/opt/cronch/cronchdata ghcr.io/indubitable/cronch:latest
```

An example `docker-compose.yml` file has been provided in this repository as well. Using Docker Compose is even easier than the above command.

## Current status

MVP achieved! It still shouldn't be considered stable, however.

Next steps:

- further testing/bugfixing (ongoing)
- polishing UI; improving UX (ongoing)
- introducing HTMX for live refresh capabilities (partially completed)
- caching to reduce overall load when retrieving filesystem data

## Screenshots

Home page:

![Home page](https://github.com/indubitable/cronch/assets/344911/fe7326c5-f94b-429d-b5c2-8ae98e5d7f2a "Home page")

Execution details page:

![Execution details page](https://github.com/indubitable/cronch/assets/344911/66cb6858-98c4-410d-94b5-cbccbc93da2e "Execution details page")
