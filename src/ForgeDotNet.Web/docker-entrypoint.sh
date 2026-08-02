#!/bin/sh
set -eu

dotnet ForgeDotNet.Web.dll --migrate-only
exec dotnet ForgeDotNet.Web.dll
