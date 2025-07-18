# Aggregateless Event Sourcing & Command Context Consistency (AEC/CCC) Prototype

## Projektüberblick

Dieses Repository enthält einen Prototypen für eine Architektur basierend auf Aggregateless Event Sourcing (AES) und Command Context Consistency (CCC). Ziel ist die Entwicklung eines wiederverwendbaren Kernels, der die Prinzipien von AES und CCC implementiert und von domänenspezifischer Logik getrennt ist. Der Prototyp demonstriert die Anwendung dieser Konzepte am Beispiel einer CRM-Anwendung (Kontakte, Firmen, Todos).

---

## Theoretischer Hintergrund

**Aggregateless Event Sourcing (AES)** ist ein Ansatz, bei dem Events als unabhängige, unveränderliche Fakten betrachtet werden, die nicht an ein einzelnes Aggregate gebunden sind. Die Architektur verzichtet bewusst auf klassische DDD-Aggregate und deren Komplexität. Stattdessen werden Entscheidungen und Konsistenz dynamisch pro Command und Kontext definiert. Die Konsistenzgrenze ist nicht mehr ein statisches Aggregate, sondern ein durch Event-Queries definierter Kontext.

**Command Context Consistency (CCC)** sorgt dafür, dass neue Events nur dann hinzugefügt werden, wenn der relevante Kontext seit dem Lesen nicht verändert wurde (Optimistic Locking). Dies wird typischerweise durch atomare Inserts und CTEs (Common Table Expressions) in der Datenbank realisiert.

---

## Architektur & Komponenten

Das Projekt ist in zwei Hauptteile gegliedert:

1. **Event Sourcing Kernel (.NET Assembly):**
   - Generische C#-Bibliothek mit den Kernfunktionen von AES und CCC
   - Schnittstellen für Events, Commands, Event Store und Kontext-Queries
   - Implementierungen für PostgreSQL und In-Memory Event Store
   - Optimistic Locking und Kontext-Queries als zentrale Mechanismen

2. **CRM Prototyp (.NET Assembly):**
   - Domänenspezifische Anwendung (Kontakte, Firmen, Todos)
   - Definiert konkrete Events, Commands und Command-Handler
   - Nutzt den Kernel als Abhängigkeit
   - Validiert die Architektur und den Kernel in einer realen Anwendung

---

## Technologien

- **Sprache:** C# (.NET)
- **Event Store Backend:** PostgreSQL (SQL)
- **Test-Backend:** In-Memory Event Store

---

## Implementierungsplan (Kurzfassung)

Das Projekt wird iterativ und testgetrieben umgesetzt:

1. **Kernel Core:**
   - Definition der Kern-Interfaces und -Klassen
   - In-Memory und PostgreSQL Event Store
   - Kontextbasierte Abfragen und atomare Schreiboperationen
2. **CRM Prototyp:**
   - Erste Commands und Events (z.B. Kontakt erstellen)
   - Command-Handler mit Kontext-Query und Event-Erzeugung
   - Integrationstests mit PostgreSQL
3. **Erweiterungen:**
   - Update-Commands, robustere Serialisierung, Dependency Injection, Logging
   - Optionale Erweiterungen wie Snapshotting, weitere Features

---

## Zielsetzung & Vorteile

- **Vereinfachte Architektur:** Keine Aggregate, keine komplexen Objektgraphen
- **Flexible Konsistenz:** Dynamische, kontextbasierte Konsistenzgrenzen
- **Wiederverwendbarer Kernel:** Trennung von Infrastruktur und Domäne
- **Testbarkeit & Erweiterbarkeit:** Fokus auf Unit- und Integrationstests
- **Niedrige Einstiegshürde:** Reduzierte Komplexität gegenüber klassischem Event Sourcing

---

## Nächste Schritte

- Detaillierte Implementierung gemäß Plan
- Entwicklung und Test des Kernels und CRM-Prototyps
- Dokumentation und Erweiterung nach Bedarf

---

## Weitere Informationen

Die theoretischen Grundlagen, die Spezifikation und der Implementierungsplan sind in den Markdown-Dokumenten im Verzeichnis `aidocs/` enthalten:
- `01 Aggregateless Event Sourcing Investigation_.md`
- `02 Erste Projektdefinition.md`
- `03 Erster Implementierungsplan.md`

Für technische Details und Architekturentscheidungen siehe diese Dokumente.
