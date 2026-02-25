#!/bin/bash
cd $(dirname "$0")
export RUN_SYSTEM_TESTS=true
dotnet test "$@"
