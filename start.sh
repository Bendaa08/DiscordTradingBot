#!/bin/bash

# Start the trading Discord bot
dotnet run --project Program.cs

# Start the HTTP server
dotnet run --project webserver.cs
