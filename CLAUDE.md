# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

This is a prototype for an Aggregateless Event Sourcing (AES) and Command Context Consistency (CCC) architecture. The project implements a reusable kernel separated from domain-specific logic, demonstrated through a CRM application example.

## Architecture

The project consists of two main components:

1. **Event Sourcing Kernel** (`aec-ccc-prototype.kernel`): Generic C# library implementing AES and CCC core functionality
2. **CRM Prototype** (`aec-ccc-prototype.example-crm`): Domain-specific application demonstrating the kernel usage

## Project Structure

```
src/aec-ccc-prototype/
├── aec-ccc-prototype.sln                    # Main solution file
├── aec-ccc-prototype.kernel/                # Core event sourcing kernel
├── aec-ccc-prototype.example-crm/           # CRM domain example
└── tests/
    ├── aec-ccc-prototype.kernel.tests/      # Kernel unit tests
    └── aec-ccc-prototype.example-crm.tests/ # CRM tests
```

## Common Commands

### Build
```bash
cd src/aec-ccc-prototype
dotnet build
```

### Run Tests
```bash
cd src/aec-ccc-prototype
dotnet test
```

### Run Specific Test Project
```bash
cd src/aec-ccc-prototype
dotnet test tests/aec-ccc-prototype.kernel.tests/
dotnet test tests/aec-ccc-prototype.example-crm.tests/
```

## Technology Stack

- **.NET 8** with C#
- **NUnit** for testing
- **PostgreSQL** for event storage (production)
- **In-Memory** event store for testing
- Nullable reference types enabled

## Key Architectural Concepts

- **Aggregateless Design**: No traditional DDD aggregates; consistency boundaries are defined dynamically per command
- **Event-First**: Events are immutable facts not bound to specific aggregates
- **Context Queries**: Dynamic consistency boundaries defined through event queries
- **Optimistic Locking**: Consistency ensured through command context consistency checks

## Core Interfaces

The kernel defines key interfaces:
- `IEvent`: Base event interface
- `ICommand`: Base command interface  
- `IEventStore`: Event persistence and querying
- `IContextQueryDefinition`: Dynamic context boundary definition

## Development Notes

- The project is in early prototype phase with basic test scaffolding
- Implementation follows the technical specification in `aidocs/04 Technische Spezifikation.md`
- Event store supports both PostgreSQL and in-memory backends
- Focus on test-driven development with clear separation of concerns