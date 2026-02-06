# Team Resurgent Presents, ZorkDotNet

[![License: MIT No Attribution](https://img.shields.io/badge/License-MIT%20No%20Attribution-green.svg)](LICENSE.md)
[![Build and test](https://github.com/OWNER/ZorkDotNet/actions/workflows/dotnet-desktop.yml/badge.svg)](https://github.com/OWNER/ZorkDotNet/actions/workflows/dotnet-desktop.yml)
[![Download](https://img.shields.io/badge/download-latest-brightgreen.svg?style=for-the-badge&logo=github)](https://github.com/OWNER/ZorkDotNet/releases/latest)

<p align="center">
  <img src="images/cover.png" alt="Zork cover art" width="400" height="400" />
</p>

A .NET port of [Zork](https://en.wikipedia.org/wiki/Zork), the classic interactive fiction game.

## Original source

The game originates from the 1977 version of Zork created at MIT by Tim Anderson, Marc Blank, Bruce Daniels, and Dave Lebling. The **1977 source code** is preserved and published by MIT’s Department of Distinctive Collections (DDC):

- **[MITDDC/zork](https://github.com/MITDDC/zork)** — Source code for the 1977 version of Zork (MDL, from the [Tapes of Tech Square (ToTS)](https://archivesspace.mit.edu/repositories/2/archival_objects/347748) collection).

This repository is an independent reimplementation in C#/.NET and is not affiliated with MIT or Infocom.

## Building and running

- **Build:** `dotnet build ZorkDotNet.sln`
- **Run:** `dotnet run --project ZorkDotNet`
- **Tests:** `dotnet test ZorkDotNet.sln`
