# Zed - Nissan Consult ECU Library

A .NET Framework 4.0 class library implementing the **Nissan Consult I** serial diagnostic protocol. Provides low-level ECU communication, real-time sensor streaming, fault-code retrieval, and active actuator tests for Nissan vehicles.

**Initiated:** 2012-10-16 · **Framework:** .NET Framework 4.0 · **Solution:** `Zed.sln`

---

## Overview

The `Consult` library encapsulates the complete Consult I communication stack: serial port initialisation, ECU handshake, command/response framing, and sensor register decoding.

---

## Supported Sensors

| Sensor | Register |
|--------|----------|
| Engine RPM | 0x00-0x01 |
| Mass Air Flow (MAF) | 0x04-0x05 |
| Coolant Temperature | 0x08 |
| O2 (Left / Right) | 0x09-0x0A |
| Vehicle Speed (km/h) | 0x0B |
| Battery Voltage | 0x0C |
| Throttle Position | 0x0D |
| Ignition Timing | 0x16 |
| AAC Idle Air Control Valve | 0x17 |
| Wastegate Solenoid | 0x28 |

---

## Communication Protocol

| Parameter | Value |
|-----------|-------|
| Interface | Nissan Consult I (14-pin) |
| Transport | RS-232 serial |
| Baud rate | 9,600 |
| ECU init sequence | 0xFF 0xFF 0xEF 0x00 |

---

## Active Tests

- Fuel injector pulse-width test (21 steps)
- Ignition timing sweep (11 steps)

---

> ECU.cs contains a small helper class by [Tangible Software Solutions](https://www.tangiblesoftwaresolutions.com) (free to use with attribution). See [THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md).
