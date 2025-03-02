# CRONCH! authentication example

This folder contains an example Docker Compose configuration for integrating CRONCH! with Authelia for authentication purposes.

Authelia is an open-source authentication and authorization server that provides single sign-on (SSO) and two-factor authentication (2FA) for applications. This example demonstrates how to configure hosting such that CRONCH! is accessible only to authenticated users.

This example configuration also includes Caddy as a reverse proxy to handle HTTPS and routing.

## Security warnings

There are several default values in the provided configuration files that must be changed to ensure the security of your setup:

- **Authelia Configuration**: Update the `authelia/configuration.yml` file with secure values for secrets, keys, and other sensitive information.
- **User Database**: Modify the `authelia/users_database.yml` file to include your actual users and secure passwords.
- **Caddy Configuration**: Ensure that the `Caddyfile` contains the correct domain names and secure settings for your deployment.

Failure to update these values **will** result in security vulnerabilities!

## Files

- `docker-compose.yml`: The Docker Compose configuration file that sets up the necessary services, including CRONCH!, Authelia, and Caddy.
- `authelia/configuration.yml`: The configuration file for Authelia.
- `authelia/users_database.yml`: The user database file for Authelia.
- `caddy/Caddyfile`: The configuration file for Caddy.

## Prerequisites

- Docker
- Docker Compose

## Configuration

Refer to the [Authelia documentation](https://www.authelia.com/) as well as the [Caddy documentation](https://caddyserver.com/docs/) for detailed information on configuring Authelia and Caddy to suit your needs.

## Usage

1. Clone the repository and navigate to this subdirectory:
	```sh
	git clone https://github.com/indubitable/cronch.git
	cd cronch/auth-example
	```

2. Update the configuration files with your specific settings, including changing all secrets, keys, and passwords.

3. Start the services using Docker Compose, without daemonizing:
	```sh
	docker compose up
	```

4. Verify that the output of all services looks good and isn't showing fatal errors or crashes. If errors are present, fix your configuration as appropriate.

5. Start the services using Docker Compose:
	```sh
	docker compose up -d
	```

6. Access CRONCH! through Caddy and Authelia.
