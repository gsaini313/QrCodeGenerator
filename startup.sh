#!/bin/bash

# Install Chromium
apt-get update && apt-get install -y chromium

# Set the ChromeBrowserPath environment variable for IronPDF
export ChromeBrowserPath="/usr/bin/chromium"

# Navigate to the directory containing the .csproj file
cd src/API

# Start the .NET application
dotnet API.dll