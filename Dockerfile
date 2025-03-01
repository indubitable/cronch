ARG DOTNET_SDK_PLATFORM=
FROM mcr.microsoft.com/dotnet/sdk:9.0-bookworm-slim${DOTNET_SDK_PLATFORM} AS build-env
WORKDIR /build/cronch
ARG CRONCH_VERSION=0.0.1
ENV CRONCH_VERSION=${CRONCH_VERSION}

# Copy csproj and restore as distinct layers
COPY cronch/cronch.csproj ./
RUN dotnet restore

# Copy everything else and build
WORKDIR /build
COPY . .
RUN cd cronch && dotnet publish --no-self-contained -p:PublishSingleFile=false -o out /p:MvcRazorCompileOnPublish=true

# Build runtime image
FROM mcr.microsoft.com/dotnet/aspnet:9.0-bookworm-slim
WORKDIR /opt/cronch
RUN apt update \
 && apt install -y --no-install-recommends curl wget jq tini
COPY --from=build-env /build/cronch/out .
ENV ASPNETCORE_ENVIRONMENT=Production
ENTRYPOINT ["/usr/bin/tini", "--", "dotnet", "cronch.dll"]

# ------ CONFIGURATION:

# Mount this volume for configuration persistence:
VOLUME /opt/cronch/cronchconfig

# Mount this volume for history persistence:
VOLUME /opt/cronch/cronchdata

# Set this to "true" when launching container if running behind reverse proxy:
ENV ASPNETCORE_FORWARDEDHEADERS_ENABLED=false

# Change this to bind to a specific interface or different port, if needed:
ENV CRONCH_HTTP_PORTS="8080"
EXPOSE 8080
