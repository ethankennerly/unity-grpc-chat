# Backend (gRPC + SQLite)

Minimal backend for [unity-grpc-chat](../README.md).
Provides message persistence (SQLite) and real-time streaming (gRPC).

---

## Quick Start

**Install dependencies**

    ./scripts/install-backend.sh

**Build**

    ./scripts/build-backend.sh

**Run server**

    ./scripts/start-backend.sh

---

## Project Layout

- [`Chat.Server/`](Chat.Server) — gRPC services & SQLite persistence
- [`Chat.Proto/`](Chat.Proto) — Protobuf definitions
- [`Chat.Tests/`](Chat.Tests) — NUnit test suite
- [`scripts/`](scripts) — Install/build/run scripts

---

## Details

- Runs on **.NET 8**
- SQLite in **WAL mode** for concurrency
- Keeps the last **1024 messages**
