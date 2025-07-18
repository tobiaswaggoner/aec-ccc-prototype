# **Spezifikation: Aggregateless Event Sourcing mit Command Context Consistency (MVP)**

Dieses Dokument beschreibt die Architektur und Kernkomponenten eines Prototypen für ein System, das auf Aggregateless Event Sourcing (AES) und Command Context Consistency (CCC) basiert. Ziel ist es, einen wiederverwendbaren "Kernel" für diese Konzepte zu entwickeln, der von domänenspezifischer Logik getrennt ist.

## **1\. Einführung in Aggregateless Event Sourcing (AES) und Command Context Consistency (CCC)**

Traditionelles Event Sourcing verwendet oft Aggregate als Konsistenzgrenzen. AES argumentiert, dass Aggregate unnötige Komplexität einführen und die Flexibilität einschränken können. Stattdessen wird Konsistenz durch das Abfragen relevanter Events (Fakten) für jede Entscheidung definiert. Events sind reine, kontextunabhängige Aufzeichnungen von Fakten.

**Command Context Consistency (CCC)** ist der Mechanismus, der diese Konsistenz sicherstellt. Bevor ein Befehl verarbeitet wird, wird ein spezifischer "Kontext" von Events aus dem Event Store gelesen. Dieser Kontext dient als Basis für die Business-Regeln und Konsistenzprüfungen. Neue Events werden nur dann hinzugefügt, wenn der gelesene Kontext seit dem Lesen nicht verändert wurde (Optimistic Locking). Dies stellt die Atomizität der Zustandsänderung sicher, ohne auf ein explizites Domänenmodell-Aggregat angewiesen zu sein.

## **2\. Architektonische Vision: Kernel & Prototyp**

Wir verfolgen einen zweigeteilten Ansatz:

* **Event Sourcing Kernel (.NET Assembly):** Eine generische C\#-Bibliothek, die die Kernfunktionen von AES und CCC bereitstellt, einschließlich des Persistenz-Layers und der Logik für Kontext-Queries und Optimistic Locking. Dieser Kernel ist domänenunabhängig und potenziell wiederverwendbar.  
* **CRM Prototyp (.NET Assembly):** Eine domänenspezifische Anwendung (CRM für Kontakte, Firmen, Todos), die den Event Sourcing Kernel als Abhängigkeit nutzt. Hier werden die konkreten Events, Commands und Command-Handler definiert. Der Prototyp dient als "Wegwerf"-Implementierung zur Validierung des Kernel-Ansatzes.

**Technologien:**

* **Sprache:** C\# (.NET)  
* **Event Store Backend:** PostgreSQL (SQL-basierte Datenbank)  
* **Test-Backend:** In-Memory Event Store (für Unit-Tests des Kernels)

## **3\. Event Sourcing Kernel API Definition**

Der Kernel stellt folgende Schnittstellen und Klassen zur Verfügung:

### **3.1. Generische Typen für Events und Commands**

// Kernel Assembly  
public interface IEvent  
{  
    Guid EventId { get; }  
    DateTime Timestamp { get; }  
    // Weitere Metadaten wie CausationId, CorrelationId, UserId können hier hinzugefügt werden.  
}

// Kernel Assembly  
public interface ICommand  
{  
    Guid CommandId { get; }  
    // Weitere Metadaten für Tracing etc.  
}

### **3.2. Der Event Store Core (IEventStore)**

Das Herzstück des Kernels, verantwortlich für Persistenz und Kontext-Abfragen.

// Kernel Assembly  
public interface IEventStore  
{  
    /// \<summary\>  
    /// Fügt eine Liste von Events atomar hinzu und stellt Command Context Consistency sicher.  
    /// \</summary\>  
    /// \<param name="eventContextSnapshot"\>Der gelesene Kontext, auf dessen Basis die Konsistenz geprüft wird.\</param\>  
    /// \<param name="newEvents"\>Die neuen Events, die hinzugefügt werden sollen.\</param\>  
    /// \<param name="expectedVersion"\>Die erwartete Version des EventContexts zum Zeitpunkt des Schreibens (für Optimistic Locking).\</param\>  
    /// \<returns\>Die neue Version des EventContexts nach dem Schreiben.\</returns\>  
    /// \<exception cref="ConcurrencyException"\>Wird ausgelöst, wenn ein Optimistic Locking Konflikt auftritt.\</exception\>  
    Task\<long\> AppendEventsAsync(  
        EventContextSnapshot eventContextSnapshot,  
        IEnumerable\<IEvent\> newEvents,  
        long expectedVersion);

    /// \<summary\>  
    /// Liest Events basierend auf einem gegebenen Kontext und ab einer optionalen Startversion.  
    /// Dies ist entscheidend für Command Context Consistency.  
    /// \</summary\>  
    /// \<param name="contextQueryDefinition"\>Definiert, welche Events den Kontext bilden.\</param\>  
    /// \<param name="fromVersion"\>Die Version, ab der Events gelesen werden sollen. Standard ist 0 (alle Events des Kontexts).\</param\>  
    /// \<returns\>Eine Liste von Events, die den Kontext bilden, zusammen mit der aktuellen Version des Kontexts.\</returns\>  
    Task\<EventContextSnapshot\> ReadContextEventsAsync(IContextQueryDefinition contextQueryDefinition, long fromVersion \= 0);  
}

### **3.3. Abstraktionen für Command Context Consistency (CCC)**

// Kernel Assembly  
public interface IContextQueryDefinition  
{  
    /// \<summary\>  
    /// Eine Liste von Event-Typen (z.B. typeof(ContactCreatedEvent).FullName),  
    /// die für diesen Kontext relevant sind.  
    /// \</summary\>  
    IEnumerable\<string\> EventTypeFilter { get; }

    /// \<summary\>  
    /// Ein optionaler String, der eine "Query-Sprache" für die Payload-Filterung darstellt.  
    /// Die Interpretation obliegt der konkreten EventStore-Implementierung.  
    /// (Z.B. JSON-Path für Postgres JSONB, oder eine einfache Schlüssel-Wert-Struktur für In-Memory).  
    /// \</summary\>  
    string? PayloadFilterExpression { get; }  
}

// Kernel Assembly  
public class EventContextSnapshot  
{  
    public IEnumerable\<IEvent\> Events { get; }  
    public long ContextVersion { get; } // Die Version des Kontexts, wenn diese Events gelesen wurden.  
                                       // Entspricht dem 'expectedVersion' Parameter beim Schreiben.

    public EventContextSnapshot(IEnumerable\<IEvent\> events, long contextVersion)  
    {  
        Events \= events ?? throw new ArgumentNullException(nameof(events));  
        ContextVersion \= contextVersion;  
    }

    public static EventContextSnapshot Empty() \=\> new EventContextSnapshot(Enumerable.Empty\<IEvent\>(), 0);  
}

### **3.4. Ausnahmebehandlung**

// Kernel Assembly  
public class ConcurrencyException : Exception  
{  
    public ConcurrencyException(string message) : base(message) { }  
}

### **3.5. Backend-Implementierungen des Event Stores**

Der Kernel wird Implementierungen von IEventStore enthalten:

* PostgresEventStore : IEventStore  
* InMemoryEventStore : IEventStore (für Unit-Tests)

## **4\. CRM Prototyp Integration**

Die CRM-Anwendung wird die Kernel-Bibliothek nutzen:

* **Domänen-Events:** Klassen wie ContactCreatedEvent, ContactNameChangedEvent etc. werden im CRM-Projekt definiert und implementieren das IEvent-Interface des Kernels.  
* **Domänen-Commands:** Klassen wie CreateContactCommand, ChangeContactNameCommand etc. werden im CRM-Projekt definiert und implementieren das ICommand-Interface des Kernels.  
* **Command-Handler:** Für jeden Command wird ein Handler (z.B. CreateContactCommandHandler) implementiert. Dieser Handler wird den IEventStore injiziert bekommen.  
  * Er erstellt eine domänenspezifische Implementierung von IContextQueryDefinition (z.B. ContactByIdContextQueryDefinition), um den relevanten Kontext zu definieren.  
  * Er ruft IEventStore.ReadContextEventsAsync auf, um den aktuellen Kontext und dessen Version zu erhalten.  
  * Er wendet die Business-Logik an, basierend auf den gelesenen Events.  
  * Er generiert neue Events.  
  * Er ruft IEventStore.AppendEventsAsync auf, um die neuen Events unter Verwendung der gelesenen ContextVersion (für Optimistic Locking) zu persistieren.

**Beispiel Pseudo-Code für einen Command-Handler im CRM-Prototyp:**

// CRM Prototyp Assembly  
public class CreateContactCommandHandler  
{  
    private readonly IEventStore \_eventStore;

    public CreateContactCommandHandler(IEventStore eventStore)  
    {  
        \_eventStore \= eventStore;  
    }

    public async Task Handle(CreateContactCommand command)  
    {  
        // 1\. Kontext definieren: Für einen neuen Kontakt ist der Kontext leer,  
        //    aber wir könnten hier schon prüfen, ob ein Kontakt mit der gleichen E-Mail existiert.  
        //    Für den MVP lassen wir es einfach und erstellen einen leeren Kontext.  
        var contextQuery \= new ContactContextQueryDefinition(command.ContactId); // Beispiel: query by ContactId  
        var currentContext \= await \_eventStore.ReadContextEventsAsync(contextQuery);

        // Hier würde die Domänenlogik greifen. Für einen neuen Kontakt ist der Kontext leer.  
        // Wenn es ein Update wäre, würde hier der Zustand aus currentContext.Events rekonstruiert.  
        // Beispiel: Prüfen, ob Kontakt-ID bereits existiert (falls nicht durch DB-Constraint abgedeckt)  
        if (currentContext.Events.Any())  
        {  
            throw new InvalidOperationException("Contact with this ID already exists.");  
        }

        // 2\. Neue Events generieren  
        var newEvent \= new ContactCreatedEvent(  
            Guid.NewGuid(),  
            DateTime.UtcNow,  
            command.ContactId,  
            command.FirstName,  
            command.LastName,  
            command.Email);

        // 3\. Events atomar anhängen mit Optimistic Locking  
        try  
        {  
            await \_eventStore.AppendEventsAsync(currentContext, new\[\] { newEvent }, currentContext.ContextVersion);  
        }  
        catch (ConcurrencyException ex)  
        {  
            // Konfliktbehandlung (z.B. Retry-Mechanismus, Fehler an den Benutzer)  
            Console.WriteLine($"Concurrency conflict: {ex.Message}");  
            throw;  
        }  
    }  
}

// CRM Prototyp Assembly  
// Beispiel-Implementierung der IContextQueryDefinition für Kontakte  
public class ContactContextQueryDefinition : IContextQueryDefinition  
{  
    private readonly Guid \_contactId;

    public ContactContextQueryDefinition(Guid contactId)  
    {  
        \_contactId \= contactId;  
    }

    public IEnumerable\<string\> EventTypeFilter \=\> new\[\]  
    {  
        typeof(ContactCreatedEvent).FullName\!,  
        typeof(ContactNameChangedEvent).FullName\!,  
        // ... weitere relevante Event-Typen für den Kontakt-Kontext  
    };

    public string? PayloadFilterExpression \=\> $"$.ContactId \== \\"{\_contactId}\\""; // Beispiel: JSON-Path Filter  
}

## **5\. Postgres Event Store Implementierung Details**

### **5.1. Schema der events Tabelle**

Eine einzelne Tabelle events wird alle Events speichern.

CREATE TABLE events (  
    id BIGSERIAL PRIMARY KEY,           \-- Sequentielle Event-ID, dient als globale Ordnung und Version  
    event\_id UUID NOT NULL UNIQUE,      \-- Eindeutige ID des Events (vom IEvent)  
    event\_type VARCHAR(255) NOT NULL,   \-- Der vollqualifizierte Typname des Events (z.B. "MyNamespace.ContactCreatedEvent")  
    timestamp TIMESTAMP WITH TIME ZONE DEFAULT NOW() NOT NULL, \-- Zeitpunkt des Events  
    payload JSONB NOT NULL,             \-- Der Event-Inhalt als JSON  
    metadata JSONB                      \-- Optional: Zusätzliche Metadaten (z.B. User, CorrelationId, CausationId)  
);

\-- Indizes für effiziente Abfragen  
CREATE UNIQUE INDEX ix\_events\_event\_id ON events (event\_id);  
CREATE INDEX ix\_events\_event\_type ON events (event\_type);  
\-- Optional: Weitere Indizes auf häufig genutzte JSONB-Pfade im Payload, z.B. für Entity-IDs  
\-- CREATE INDEX ix\_events\_payload\_contact\_id ON events ((payload-\>\>'ContactId'));

### **5.2. Optimistic Locking mit Common Table Expressions (CTEs)**

Die AppendEventsAsync-Methode im PostgresEventStore wird eine Transaktion verwenden, um die Konsistenz zu gewährleisten. Die Kernidee ist, vor dem Einfügen neuer Events zu prüfen, ob die expectedVersion mit der aktuellen höchsten Version des Kontexts übereinstimmt.

**Pseudo-Code für AppendEventsAsync (Postgres):**

\-- SQL-Logik innerhalb der AppendEventsAsync-Methode  
\-- Angenommen:  
\--   @eventTypeFilter: Array von Event-Typen  
\--   @payloadFilterExpression: JSONB-Filterausdruck  
\--   @expectedVersion: Erwartete Version des Kontexts  
\--   @newEventsData: Array von (event\_id, event\_type, payload, metadata) für die neuen Events

BEGIN;

\-- 1\. Aktuelle höchste Version des Kontexts ermitteln  
WITH ContextEvents AS (  
    SELECT id, event\_type, payload  
    FROM events  
    WHERE event\_type \= ANY(@eventTypeFilter)  
    \-- Optional: Wenden Sie den PayloadFilterExpression an, falls vorhanden  
    \-- AND (@payloadFilterExpression IS NULL OR payload @\> @payloadFilterExpression)  
    \-- Für komplexere JSONB-Queries müsste der @payloadFilterExpression hier dynamisch eingefügt werden  
    \-- Beispiel für einen einfachen JSONB-Filter: AND payload-\>\>'ContactId' \= 'your-contact-id'  
),  
CurrentContextVersion AS (  
    SELECT COALESCE(MAX(id), 0\) AS max\_id  
    FROM ContextEvents  
)  
SELECT max\_id INTO @actualVersion FROM CurrentContextVersion;

\-- 2\. Optimistic Locking Prüfung  
IF @actualVersion \!= @expectedVersion THEN  
    RAISE EXCEPTION 'Concurrency conflict: Expected version %s, but actual version is %s', @expectedVersion, @actualVersion;  
END IF;

\-- 3\. Neue Events einfügen  
INSERT INTO events (event\_id, event\_type, timestamp, payload, metadata)  
VALUES  
    \-- Für jedes neue Event in @newEventsData  
    (@newEvent1\_id, @newEvent1\_type, @newEvent1\_timestamp, @newEvent1\_payload, @newEvent1\_metadata),  
    (@newEvent2\_id, @newEvent2\_type, @newEvent2\_timestamp, @newEvent2\_payload, @newEvent2\_metadata),  
    \-- ...  
;

\-- 4\. Neue höchste Version des Kontexts ermitteln (optional, für Rückgabe)  
SELECT COALESCE(MAX(id), 0\) FROM events WHERE event\_type \= ANY(@eventTypeFilter) AND id \> @actualVersion;

COMMIT;

*Hinweis zur Payload-Filterung:* Die dynamische Anwendung des PayloadFilterExpression in SQL erfordert Sorgfalt, um SQL-Injections zu vermeiden. Für den Prototypen könnte eine einfache String-Ersetzung für bekannte Muster oder die Verwendung von jsonb\_extract\_path\_text und Parametern ausreichend sein.

## **6\. Snapshotting (Zukünftige Erweiterung)**

Für den Minimal Viable Product (MVP) wird kein Snapshotting implementiert. Es ist als zukünftige Erweiterung vorgesehen.

Konzept für zukünftiges Snapshotting:  
Snapshotting kann als Caching-Schicht oberhalb des IEventStore im Command-Handler implementiert werden. Ein Snapshot würde die letzte bekannte ContextVersion und den daraus rekonstruierten Zustand enthalten. Der Command-Handler würde dann ReadContextEventsAsync mit dem fromVersion-Parameter aufrufen, um nur die Events zu laden, die seit dem letzten Snapshot aufgetreten sind.

## **7\. Zusammenfassung und Nächste Schritte**

Dieses Dokument skizziert einen klaren Plan für die Entwicklung des Event Sourcing Kernels und des CRM-Prototypen. Die Trennung in separate .NET-Assemblies, die Definition der Kern-APIs und die Nutzung von PostgreSQL mit CTEs für Optimistic Locking bilden ein solides Fundament.

**Nächste Schritte:**

1. **Implementierungsplan:** Detaillierung der einzelnen Implementierungsschritte für den Kernel und den CRM-Prototypen.  
2. **Architekturplan:** Spezifizierung der .NET-Projektstruktur, Dependency Injection und weiterer technischer Details.  
3. **Konkrete Implementierung:** Start der Entwicklung des Kernels und des CRM-Prototypen.