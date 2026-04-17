# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

**LOG8715 TP3 – Latency Management** (due April 19, 2026)

A Unity 6000.0.65f1 multiplayer game implementing **client-side prediction and reconciliation** for a client-server simulation. The codebase uses Unity Netcode for GameObjects (NGO v2.8.0). Testing requires ParrelSync to run cloned editor instances simultaneously as server/client.

## Build & Testing

- **Unity version**: 6000.0.65f1 (must match exactly)
- **Multi-instance testing**: Use ParrelSync (`Window > ParrelSync > Clones Manager`) to open cloned projects; run one as Server, another as Client
- **Simulated latency**: Set via `NetworkManager > UnityTransport > Debug Simulator` (250ms packet delay = 500ms round-trip)
- **Entry point**: Start from `StartupScene`; choose Host/Server/Client mode, then `MainScene` loads automatically

No build CLI — Unity Editor only. No test suite.

## Architecture

### Ownership model
- **Server owns**: all `MovingCircle` instances (position/velocity updated server-side in `FixedUpdate`)
- **Client owns**: its own `Player` instance (sends inputs via RPC; server applies and confirms)
- **Ghosts are render-only**: `CircleGhost` and `PlayerGhost` never write network state — they only position a visual representation

### Prediction pipeline

**Circles (`CircleGhost.cs`)**
Runs entirely in `Update()`. On the client, reads `MovingCircle.Position`, `MovingCircle.Velocity`, and `MovingCircle.ServerTick` (a `NetworkVariable<int>` set each server `FixedUpdate`). Computes `localTick - serverTick` ticks-to-simulate (clamped to 2× tick rate), then re-runs `SimulateCircleTick` to predict ahead. The simulation logic must exactly mirror `MovingCircle.FixedUpdate`.

**Players (`Player.cs`)**
- `UpdateInputClient()` (called each `FixedUpdate`): reads WASD, records `InputRecord {Tick, Input}` into `m_InputHistory`, immediately applies to `m_PredictedPosition`, and sends input+tick via `SendInputServerRpc`.
- `UpdatePositionServer()`: server dequeues inputs in order, applies `SimulateMove`, and writes `m_ServerConfirmedTick`.
- `ReconcileIfNeeded()`: triggered by `OnServerTickConfirmed` callback on `m_ServerConfirmedTick`. Discards history up to the confirmed tick, then replays remaining unconfirmed inputs on top of `m_Position.Value` (the server-authoritative position) to recompute `m_PredictedPosition`.

**`PlayerGhost.cs`**: owner renders `Player.PredictedPosition`; others render `Player.Position`.

### Key NetworkVariables

| Variable | Owner | Purpose |
|---|---|---|
| `MovingCircle.m_Position` | Server | Authoritative circle position |
| `MovingCircle.m_Velocity` | Server | Authoritative circle velocity |
| `MovingCircle.m_ServerTick` | Server | Tick at which position was written (used for prediction offset) |
| `Player.m_Position` | Server | Authoritative player position |
| `Player.m_ServerConfirmedTick` | Server | Last input tick processed by server (triggers reconciliation) |
| `GameState.m_IsStunned` | Server | When true, all movement stops (both server and client) |

### Stun interaction
When `GameState.IsStunned` is true, both `MovingCircle.FixedUpdate` and `Player.FixedUpdate` early-return. `CircleGhost` also skips prediction and renders the last known position.

## Project Requirements (graded)

1. **Ghost prediction** (2 pts): Circles rendered at predicted position using tick offset — already implemented in `CircleGhost.cs`
2. **Input prediction** (2 pts): Player moves immediately on client without waiting for server — implemented in `Player.cs`
3. **Reconciliation**: Player re-simulates unconfirmed inputs when server-confirmed tick arrives — implemented in `Player.ReconcileIfNeeded()`
4. **Do NOT implement**: interpolation, extrapolation, or reliability techniques
5. **Do NOT use**: Unity's built-in prediction, third-party prediction libraries

## Critical constraints

- Use **tick difference** (`localTick - serverTick`) for prediction offset — never use `CurrentRTT`
- `SimulateCircleTick` must be an exact copy of the server's `MovingCircle` physics (same boundary logic, same `Time.fixedDeltaTime`)
- `SimulateMove` in `Player.cs` must identically match the server's movement logic
- Do not break existing functionality: circles spawning, stun, client disconnect returning to `StartupScene`
