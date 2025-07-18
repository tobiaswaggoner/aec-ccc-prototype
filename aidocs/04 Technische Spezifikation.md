# Technische Spezifikation: Aggregateless Event Sourcing & Command Context Consistency (AEC/CCC)

## 1. Ziel und Kontext

Dieses Dokument beschreibt die technische Zielarchitektur und die wichtigsten Implementierungsdetails für den Prototypen einer Aggregateless Event Sourcing (AES) und Command Context Consistency (CCC) Lösung. Es dient als Referenz für die Entwicklung und wird bei architektonischen Änderungen fortlaufend angepasst.

---

## 2. Architekturübersicht

### 2.1. Komponenten

**a) Event Sourcing Kernel (.NET 8 Assembly)**
- Generische, domänenunabhängige C#-Bibliothek
- Kernfunktionen: Event Persistenz, Kontext-Queries, Optimistic Locking
- Schnittstellen: IEvent, ICommand, IEventStore, IContextQueryDefinition
- Implementierungen: PostgreSQL Event Store, In-Memory Event Store

**b) CRM Prototyp (.NET 8 Assembly)**
- Domänenspezifische Anwendung (Kontakte, Firmen, Todos)
- Definiert konkrete Events, Commands und Command-Handler
- Nutzt den Kernel als Abhängigkeit

---

### 2.2. Architekturprinzipien

- **Aggregateless:** Keine klassischen DDD-Aggregate, sondern kontextbasierte Konsistenzgrenzen
- **Events als Fakten:** Events sind unveränderliche, kontextunabhängige Fakten
- **Command Context Consistency:** Konsistenz wird pro Command durch dynamische Kontext-Queries und Optimistic Locking sichergestellt
- **Trennung von Infrastruktur und Domäne:** Kernel und Prototyp sind klar getrennt
- **Testgetrieben:** Fokus auf Unit- und Integrationstests

---

## 3. Schnittstellen und Kernklassen

### 3.1. Event- und Command-Typen
```csharp
public interface IEvent
{
    Guid EventId { get; }
    DateTime Timestamp { get; }
    // Optionale Metadaten: CausationId, CorrelationId, UserId
}

public interface ICommand
{
    Guid CommandId { get; }
    // Optionale Metadaten für Tracing
}
```

### 3.2. Event Store API
```csharp
public interface IEventStore
{
    Task<long> AppendEventsAsync(EventContextSnapshot eventContextSnapshot, IEnumerable<IEvent> newEvents, long expectedVersion);
    Task<EventContextSnapshot> ReadContextEventsAsync(IContextQueryDefinition contextQueryDefinition, long fromVersion = 0);
}
```

### 3.3. Kontext-Query und Snapshot
```csharp
public interface IContextQueryDefinition
{
    IEnumerable<string> EventTypeFilter { get; }
    string? PayloadFilterExpression { get; }
}

public class EventContextSnapshot
{
    public IEnumerable<IEvent> Events { get; }
    public long ContextVersion { get; }
    public static EventContextSnapshot Empty() => new EventContextSnapshot(Enumerable.Empty<IEvent>(), 0);
}
```

### 3.4. Fehlerbehandlung
```csharp
public class ConcurrencyException : Exception
{
    public ConcurrencyException(string message) : base(message) { }
}
```

---

## 4. Event Store Backend

### 4.1. PostgreSQL Event Store
- Tabelle `events` mit Spalten: id (BIGSERIAL, PK), event_id (UUID, unique), event_type (string), timestamp, payload (JSONB), metadata (JSONB)
- Indizes auf event_id, event_type, ggf. JSONB-Pfade
- Optimistic Locking via CTE: Prüfung der ContextVersion vor Insert

### 4.2. In-Memory Event Store
- Für Unit-Tests und schnelle Entwicklung
- Speicherung in einer List<IEvent>

---

## 5. CRM Prototyp: Domänenspezifische Komponenten

- Eigene Event- und Command-Klassen, z.B. ContactCreatedEvent, CreateContactCommand
- Command-Handler, die IEventStore nutzen und Kontext-Queries definieren
- Beispiel:
```csharp
public class CreateContactCommandHandler
{
    private readonly IEventStore _eventStore;
    public async Task Handle(CreateContactCommand command)
    {
        var contextQuery = new ContactContextQueryDefinition(command.ContactId);
        var currentContext = await _eventStore.ReadContextEventsAsync(contextQuery);
        if (currentContext.Events.Any())
            throw new InvalidOperationException("Contact with this ID already exists.");
        var newEvent = new ContactCreatedEvent(...);
        await _eventStore.AppendEventsAsync(currentContext, new[] { newEvent }, currentContext.ContextVersion);
    }
}
```

---

## 6. Frameworks und Tools

- **.NET Version:** .NET 8
- **Unit Testing:** NUnit
- **Mocking:** NSubstitute
- **Dependency Injection:** Microsoft.Extensions.DependencyInjection
- **Logging:** Serilog oder Microsoft.Extensions.Logging
- **Datenbankzugriff:** Npgsql für PostgreSQL
- **JSON-Serialisierung:** System.Text.Json oder Newtonsoft.Json

---

## 7. Teststrategie

- **Unit-Tests:** Für Kernel und CRM-Prototyp, Fokus auf Logik und Schnittstellen
- **Integrationstests:** Für Event Store Backends (PostgreSQL, In-Memory)
- **Mocking:** NSubstitute für Abhängigkeiten
- **Testdatenbank:** Separate Umgebung für Integrationstests

---

## 8. Erweiterungen & Anpassbarkeit

- Das Dokument wird bei architektonischen Änderungen fortlaufend aktualisiert
- Geplante Erweiterungen: Snapshotting, weitere CRM-Features, CQRS-Readmodels, Event-Distribution

---

## 9. Referenzen

- Architektur- und Implementierungsdetails siehe README.md und Dokumente im Verzeichnis `aidocs/`
- Implementierungsplan: `03 Erster Implementierungsplan.md`
- Spezifikation: `02 Erste Projektdefinition.md`
- Theoretischer Hintergrund: `01 Aggregateless Event Sourcing Investigation_.md`
