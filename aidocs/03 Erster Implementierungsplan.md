# **Implementierungsplan: Aggregateless Event Sourcing mit Command Context Consistency (MVP)**

Dieser Plan skizziert die Implementierung des Event Sourcing Kernels und des CRM-Prototypen in iterativen Schritten, mit Fokus auf schneller Testbarkeit und schrittweiser Funktionserweiterung.

## **Grundprinzipien des Plans**

* **Iterativ & Inkrementell:** Jede Phase und Iteration liefert ein kleines, testbares Stück Funktionalität.  
* **Testgetrieben:** Unit- und Integrationstests sind der primäre "Nutzen" in Abwesenheit eines Frontends.  
* **Logische Abfolge:** Die Schritte bauen aufeinander auf, um Abhängigkeiten zu minimieren und einen reibungslosen Fortschritt zu gewährleisten.  
* **Trennung der Verantwortlichkeiten:** Klare Abgrenzung zwischen Kernel- und CRM-Prototyp-Assemblys.

## **Phasen und Iterationen**

### **Phase 1: Kernel Core \- Event Store Grundlagen**

**Ziel:** Eine minimale, testbare Event Store Implementierung, die Events speichern und lesen kann, zunächst ohne vollständige CCC-Logik.

* **Iteration 1.1: Definition der Kern-Interfaces und \-Klassen**  
  * **Kernel Assembly:**  
    * Definieren der Interfaces: IEvent, ICommand, IContextQueryDefinition, IEventStore.  
    * Definieren der Klassen: EventContextSnapshot, ConcurrencyException.  
  * **Tests:**  
    * Erstellen eines Testprojekts für die Kernel Assembly.  
    * Grundlegende Unit-Tests, die sicherstellen, dass die Interfaces und Klassen korrekt definiert sind (z.B. Instanziierung).  
* **Iteration 1.2: In-Memory Event Store Implementierung**  
  * **Kernel Assembly:**  
    * Implementieren von InMemoryEventStore : IEventStore.  
    * AppendEventsAsync: Einfache Speicherung in einer List\<IEvent\>, zunächst ohne Optimistic Locking.  
    * ReadContextEventsAsync: Einfache Filterung der List\<IEvent\> nach EventTypeFilter und fromVersion. PayloadFilterExpression kann zunächst ignoriert oder rudimentär (z.B. nach einem festen Property) implementiert werden.  
  * **Tests:**  
    * Unit-Tests für InMemoryEventStore:  
      * Events hinzufügen und lesen.  
      * Lesen ab einer bestimmten Version (fromVersion).  
      * Filterung nach EventTypeFilter.  
* **Iteration 1.3: Basis PostgreSQL Event Store Setup**  
  * **Kernel Assembly:**  
    * Einrichten der PostgreSQL-Verbindung (z.B. mit Npgsql).  
    * Implementieren einer Methode zum Erstellen der events-Tabelle (siehe Spezifikation, Abschnitt 5.1).  
    * Erste, rudimentäre Implementierung von PostgresEventStore : IEventStore:  
      * AppendEventsAsync: Einfaches INSERT der Events in die events-Tabelle (Payload als JSONB), zunächst ohne Optimistic Locking.  
      * ReadContextEventsAsync: Einfaches SELECT nach EventTypeFilter und id \> fromVersion. PayloadFilterExpression wird noch nicht angewendet.  
  * **Tests:**  
    * Integrationstests für PostgresEventStore:  
      * Stellen Sie sicher, dass die Tabelle erstellt werden kann.  
      * Events können hinzugefügt und wieder gelesen werden.  
      * Lesen ab einer bestimmten Version funktioniert.  
      * **Wichtig:** Tests sollten eine saubere Testdatenbank-Umgebung verwenden (z.B. Transaktionen pro Test oder temporäre Datenbanken).

### **Phase 2: Kernel Core \- Command Context Consistency (CCC)**

**Ziel:** Implementierung der vollständigen CCC-Logik im PostgresEventStore, einschließlich kontextbasierter Lese- und atomarer Schreiboperationen mit Optimistic Locking.

* **Iteration 2.1: Erweiterte Kontext-Abfragen in PostgreSQL**  
  * **Kernel Assembly:**  
    * Erweitern von PostgresEventStore.ReadContextEventsAsync:  
      * Anwenden des PayloadFilterExpression im SQL-Query (z.B. mit jsonb\_extract\_path\_text oder \-\>\> Operatoren für einfache Schlüssel-Wert-Paare).  
      * Sicherstellen, dass die ContextVersion (höchste id im Kontext) korrekt zurückgegeben wird.  
    * Implementieren einer JSON-Serialisierungs-/Deserialisierungslogik für IEvent Payloads (z.B. mit System.Text.Json oder Newtonsoft.Json).  
  * **Tests:**  
    * Integrationstests für PostgresEventStore.ReadContextEventsAsync:  
      * Testen der Filterung nach Event-Typen und Payload-Inhalten.  
      * Verifizieren der korrekten ContextVersion.  
* **Iteration 2.2: Atomares Schreiben mit Optimistic Locking (CTEs)**  
  * **Kernel Assembly:**  
    * Vollständige Implementierung von PostgresEventStore.AppendEventsAsync unter Verwendung der CTE-basierten Optimistic Locking Logik (siehe Spezifikation, Abschnitt 5.2).  
    * Sicherstellen, dass ConcurrencyException bei Konflikten ausgelöst wird.  
  * **Tests:**  
    * Integrationstests für PostgresEventStore.AppendEventsAsync:  
      * Erfolgreiches Hinzufügen von Events.  
      * Testen von Konkurrenzsituationen: Zwei gleichzeitige Versuche, Events zum *gleichen* Kontext mit der *gleichen* expectedVersion hinzuzufügen, wobei einer fehlschlagen muss.  
      * Verifizieren, dass die ConcurrencyException korrekt ausgelöst wird.

### **Phase 3: CRM Prototyp \- Erster Command (Create)**

**Ziel:** Implementierung des ersten CRM-Commands und des zugehörigen Handlers, der den Kernel nutzt, um einen neuen Kontakt zu erstellen.

* **Iteration 3.1: Definition der CRM-Domänen-Events und \-Commands**  
  * **CRM Prototyp Assembly:**  
    * Definieren der Klassen: ContactCreatedEvent (implementiert IEvent), CreateContactCommand (implementiert ICommand).  
    * Sicherstellen, dass die Event-Payloads für JSON-Serialisierung geeignet sind.  
  * **Tests:**  
    * Unit-Tests für die Domänen-Events und \-Commands (z.B. korrekte Konstruktion).  
* **Iteration 3.2: Implementierung des Create Command Handlers**  
  * **CRM Prototyp Assembly:**  
    * Implementieren von CreateContactCommandHandler.  
    * Der Handler erhält IEventStore per Dependency Injection.  
    * Erstellt eine ContactContextQueryDefinition für den neuen Kontakt (zunächst leerer Kontext erwartet).  
    * Ruft \_eventStore.ReadContextEventsAsync auf.  
    * Generiert ContactCreatedEvent.  
    * Ruft \_eventStore.AppendEventsAsync auf.  
  * **Tests:**  
    * Unit-Tests für CreateContactCommandHandler unter Verwendung des InMemoryEventStore (Mocking oder direkte Instanziierung).  
* **Iteration 3.3: Integrationstests für Create Contact**  
  * **CRM Prototyp Tests:**  
    * Integrationstests für den CreateContactCommand unter Verwendung des PostgresEventStore.  
    * Verifizieren, dass ein Kontakt erfolgreich erstellt und dessen Event im Event Store persistiert wird.  
    * Testen des Verhaltens bei doppelter Erstellung (falls durch Business-Logik oder DB-Constraint abgedeckt).

### **Phase 4: CRM Prototyp \- Update Command (Change Name)**

**Ziel:** Implementierung eines Update-Commands, der die Rekonstruktion des Kontexts und die Anwendung von Business-Logik demonstriert.

* **Iteration 4.1: Definition der Update-Events und \-Commands**  
  * **CRM Prototyp Assembly:**  
    * Definieren der Klassen: ContactNameChangedEvent (implementiert IEvent), ChangeContactNameCommand (implementiert ICommand).  
* **Iteration 4.2: Implementierung des Change Name Command Handlers**  
  * **CRM Prototyp Assembly:**  
    * Implementieren von ChangeContactNameCommandHandler.  
    * Der Handler erhält IEventStore per Dependency Injection.  
    * Erstellt eine ContactContextQueryDefinition für den zu aktualisierenden Kontakt.  
    * Ruft \_eventStore.ReadContextEventsAsync auf, um alle Events für diesen Kontakt zu laden.  
    * **Wichtig:** Rekonstruiert einen minimalen Zustand des Kontakts aus den geladenen Events (z.B. ContactState Klasse mit Apply(IEvent event) Methode).  
    * Wendet Business-Logik an (z.B. Name darf nicht leer sein).  
    * Generiert ContactNameChangedEvent.  
    * Ruft \_eventStore.AppendEventsAsync auf, unter Verwendung der ContextVersion des gelesenen Snapshots.  
  * **Tests:**  
    * Unit-Tests für ChangeContactNameCommandHandler unter Verwendung des InMemoryEventStore.  
* **Iteration 4.3: Integrationstests für Change Name**  
  * **CRM Prototyp Tests:**  
    * Integrationstests für den ChangeContactNameCommand unter Verwendung des PostgresEventStore.  
    * Testen des gesamten Ablaufs: Kontakt erstellen, Name ändern, Verifizieren, dass das ContactNameChangedEvent persistiert wurde.  
    * Testen von Konkurrenzsituationen beim Ändern des Namens.  
    * Testen von Fehlerfällen (z.B. Kontakt existiert nicht, ungültiger Name).

### **Phase 5: Verfeinerung & Tooling**

**Ziel:** Verbesserung der Robustheit, Wartbarkeit und Erweiterbarkeit des Prototypen.

* **Iteration 5.1: Robuste Event-Serialisierung und \-Deserialisierung**  
  * **Kernel Assembly:**  
    * Implementieren einer zentralen Event-Serialisierungs-/Deserialisierungsstrategie, die Event-Typen korrekt auflösen kann (z.B. unter Verwendung eines TypeResolver oder JsonConverter für IEvent).  
    * Sicherstellen, dass Events mit verschiedenen Payloads korrekt gespeichert und geladen werden können.  
  * **Tests:**  
    * Unit-Tests für die Serialisierungs-/Deserialisierungslogik.  
* **Iteration 5.2: Dependency Injection Setup**  
  * **Gesamtsystem:**  
    * Einrichten eines Dependency Injection Containers (z.B. Microsoft.Extensions.DependencyInjection) im Prototypen.  
    * Registrieren aller Kernel- und CRM-Komponenten.  
    * Konfigurieren der PostgreSQL-Verbindungszeichenfolge.  
  * **Tests:**  
    * Grundlegende Integrationstests, die sicherstellen, dass der DI-Container korrekt funktioniert und alle Abhängigkeiten aufgelöst werden können.  
* **Iteration 5.3: Logging und Fehlerbehandlung**  
  * **Gesamtsystem:**  
    * Integration einer Logging-Bibliothek (z.B. Serilog oder Microsoft.Extensions.Logging).  
    * Hinzufügen von Logging-Statements in kritischen Pfaden (Event Store, Command Handler).  
    * Verbessern der Fehlerbehandlung und Ausnahmebehandlung.  
* **Iteration 5.4: Erweiterung des IContextQueryDefinition (Optional, je nach Bedarf)**  
  * **Kernel Assembly:**  
    * Überlegen, ob der PayloadFilterExpression flexibler gestaltet werden muss (z.B. durch ein Expression Objekt, das in SQL übersetzt wird, oder eine eigene "Query-Sprache"). Für den MVP ist der String-Ansatz ausreichend, aber dies wäre der Punkt für Erweiterungen.  
  * **Tests:**  
    * Tests für erweiterte Filterlogik.

## **Nächste Schritte nach Abschluss des Plans**

Nachdem dieser Implementierungsplan abgeschlossen ist, haben wir einen voll funktionsfähigen, testbaren Kern für Aggregateless Event Sourcing und Command Context Consistency. Dies bildet eine solide Basis für weitere Erweiterungen, wie z.B.:

* Implementierung weiterer CRM-Features (Firmen, Todos).  
* Erstellung von Read Models und Query-Seiten (CQRS).  
* Integration von Message Brokern (z.B. Kafka) für Event-Distribution.  
* Implementierung von Snapshotting für Performance-Optimierungen.

Dieser Plan bietet einen klaren Fahrplan, um das Projekt strukturiert und effizient voranzutreiben.