![CRONCH!](https://github.com/indubitable/cronch/assets/344911/e091490d-fa64-423f-8153-585bd259dab3)

# A web-based job scheduler

[📦 Docker (standard)](https://github.com/indubitable/cronch/pkgs/container/cronch) - [📦 Docker (extra)](https://github.com/indubitable/cronch/pkgs/container/cronch-extra) - [📜 Features](#features) - [💽 Installation](#installation) - [📝 Configuration](#configuration) - [📷 Screenshots](#screenshots)

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

CRONCH! can be installed using pre-built Docker images or using binary artifacts provided in [GitHub releases](https://github.com/indubitable/cronch/releases). Both methods are described below.

In addition to platform-specific binaries, cross-platform (`xplat`) ones are provided as well. They are smaller because they require the [latest ASP.NET Core runtime](https://dotnet.microsoft.com/en-us/download/dotnet) to already be installed on the target system.

### Using Docker

Docker is the easiest way to install CRONCH! at the moment.

#### Image options

The following images are provided:

`ghcr.io/indubitable/cronch` - [see details](https://github.com/indubitable/cronch/pkgs/container/cronch)

This is the standard CRONCH! image, based on Debian. It's a multi-arch image with support for amd64 (x86_64), arm64, and armv7. It comes with Bash and Perl for scripting.

`ghcr.io/indubitable/cronch-extra` - [see details](https://github.com/indubitable/cronch/pkgs/container/cronch-extra)

This is a larger CRONCH! image, also based on Debian and also multi-arch. In addition to Bash and Perl, it supports Node, Python, Ruby, PHP, and PowerShell Core (pwsh) for scripting.

The sizes of the two images are significantly different. On amd64, standard is under 300MB, while "extra" is over 1GB.

#### Container configuration

By default, CRONCH! listens on port 8080 inside the container. Of course, that can be mapped to any external port. Additionally, two volumes should be mounted: one for configuration, and one for historical data storage. They are `/opt/cronch/cronchconfig` and `/opt/cronch/cronchdata`, respectively.

Using plain Docker with filesystem mounts, the command might look like this:

```shell
docker run --restart=always --name=cronch -p 8080:8080 -v ./cronchconfig:/opt/cronch/cronchconfig -v ./cronchdata:/opt/cronch/cronchdata ghcr.io/indubitable/cronch:latest
```

An example `docker-compose.yml` file has been provided in this repository as well. Using Docker Compose is even easier than the above command.

### Using Windows binaries

CRONCH! GitHub releases include Windows x64 and ARM64 binaries. Simply download and unzip the appropriate `-win-` file from the [latest GitHub release](https://github.com/indubitable/cronch/releases), and execute `cronch.exe`.

Note, when running in Windows, CRONCH! will default to outputting logs, including the configured HTTP listen address(es), to standard out as well as the Event Log. The latter is especially useful when running as a Windows service.

#### Windows service option

CRONCH! can register and run as a Windows service. To install it as a Windows service, execute `cronch.exe -i` from an elevated command prompt. To uninstall it, execute `cronch.exe -u` instead.

### Using Linux binaries

CRONCH! GitHub releases include Linux binaries for various architectures, including options for musl libc. Simply download and `tar xf` the appropriate `-linux-` file from the [latest GitHub release](https://github.com/indubitable/cronch/releases), and execute `./cronch`.

#### Integrating with systemd

An example systemd service file is [provided in the repository](https://github.com/indubitable/cronch/blob/main/cronch-daemon.service). To use it, download the file, and make any necessary changes to it, such as updating paths. Then, copy it inside `/etc/systemd/system/`. Once that is done, execute the following:

```bash
sudo systemctl daemon-reload
sudo systemctl enable cronch-daemon.service
sudo systemctl start cronch-daemon.service
```

### Using Mac binaries

_Instructions TBD..._

## Configuration

### Environment variables and appsettings.json

Pre-run configuration includes the ability to customize things like what port number CRONCH! listens on and directories for data storage.

| Environment variable | JSON config | Default | Description |
| --- | --- | --- | --- |
| `CRONCH_ConfigLocation` | `ConfigLocation` in appsettings.json | `./cronchconfig` | The storage location of the main configuration XML file that contains job definitions and runtime settings |
| `CRONCH_DataLocation` | `DataLocation` in appsettings.json | `./cronchdata` | The storage location of execution data (SQLite database and output files) |
| `CRONCH_HTTP_PORTS` | `HTTP_PORTS` in appsettings.Production.json | `8080` | The HTTP port to listen on |

For advanced hosting configurations using the built-in Kestrel web server, including setting up HTTPS, please see [Microsoft's documentation](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/servers/kestrel/endpoints?view=aspnetcore-8.0).

### In-app settings

The Settings page in CRONCH! contains additional configurable options, including:

- Maximum count of historical executions per job, which deletes older job execution records by count
- Maximum age of historical run executions in days, which deletes older job execution records by age
- Default script file location, which allows for job scripts to be placed in a custom directory by default
- Run completion script, which can be used to post-process job executions

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
