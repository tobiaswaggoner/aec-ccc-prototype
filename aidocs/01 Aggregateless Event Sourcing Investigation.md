# **Investigating Aggregateless Event Sourcing and Command Context Consistency**

## **1\. Introduction to Aggregateless Event Sourcing**

Aggregateless Event Sourcing (AES) represents a notable architectural departure from traditional Domain-Driven Design (DDD) aggregates. In this paradigm, events are no longer rigidly bound to a single aggregate entity; instead, they are treated as "pure facts" that are independent and self-descriptive, recorded chronologically in an append-only log.1 The fundamental concept involves capturing all system changes as immutable facts, thereby eliminating the need for the often over-engineered and stateful "aggregates" typically associated with DDD's tactical patterns.3

This approach consciously removes rigid entities and complex object graphs from the architectural mental model. The focus shifts entirely to simpler data structures, specifically events and commands, and pure functions that encapsulate decision-making logic.1 Crucially, consistency, which in traditional event sourcing is managed by aggregates, is instead managed dynamically per command, based on the specific context that command requires for its operation.1

### **Motivations and Core Principles: Why remove aggregates?**

The adoption of aggregateless event sourcing is driven by several compelling motivations, primarily aimed at simplifying system design and enhancing flexibility. Traditional DDD aggregates frequently present challenges as system requirements evolve, often leading to issues with "wrong aggregate boundaries" where aggregates become overly complex, accumulate too many responsibilities, or require intricate splitting or merging.5 Such refactoring can be particularly difficult and costly in event-sourced systems, especially when aggregates share an event stream.5

Removing aggregates dramatically simplifies the system by stripping away rigid object-oriented representations and their associated complexities, such as heavyweight repositories and layers of indirection.1 This approach fosters a clear separation of concerns, where events serve as pure persistence and decisions embody pure logic, leading to a more streamlined and comprehensible architecture.1

Furthermore, aggregates can inadvertently create dependencies that undermine the autonomy of feature slices. If multiple slices rely on the same entity, their independence is compromised.3 AES promotes truly self-contained feature slices, where each slice manages its own state reconstruction and logic. These slices operate on the same underlying event stream but selectively process only the events relevant to their specific command context.1

Event Sourcing inherently aligns with functional programming principles, primarily through its emphasis on immutability.3 AES further embraces this by leveraging pure functions for decision-making and state reconstruction. This ensures that the core logic is referentially transparent and free from side effects, making the system more predictable and easier to reason about.3

### **Key Differences from Traditional Aggregate-Based Event Sourcing**

The distinctions between aggregateless and traditional aggregate-based event sourcing are fundamental:

* **Consistency Boundary:** In traditional event sourcing, consistency is strictly enforced at the aggregate boundary, often necessitating the rehydration of the entire aggregate state from its event stream to validate incoming commands.7 In contrast, AES employs a dynamic consistency model, defined precisely by the specific query that establishes the "command context" for a given operation.1  
* **Event Ownership:** Traditional event sourcing often implies that an event "belongs" to a specific aggregate.1 AES explicitly discards this notion; events are considered pure, independent facts, designed to be reusable and interpretable by diverse feature slices without inherent coupling.1  
* **Domain Model Representation:** Traditional DDD utilizes rich, stateful aggregate objects that encapsulate both data and behavior.3 AES moves away from these rigid object-oriented representations, favoring simpler data structures and pure functions. This approach inherently reduces the "object-relational impedance mismatch" 9 by decoupling the persistence model from a complex mutable object graph.1  
* **State Rehydration:** In traditional event sourcing, reconstructing an object's state typically involves replaying all events related to that object.9 In AES, only the subset of events relevant to the current command's specific context is queried and used to build a minimal, temporary state, which is then discarded after the decision is made.1

### **Benefits of the Aggregateless Approach**

The aggregateless approach offers several advantages:

* **Enhanced Simplicity and Reduced Overhead:** By eliminating the complexities of aggregates, AES streamlines the system, reduces architectural overhead, and avoids the need for intricate locking mechanisms or managing shared aggregate state.1

* **Increased Flexibility and Event Reusability:** The concept of "pure events" allows for their easy reuse and interpretation across different feature slices. This flexibility means that new behaviors or decisions can emerge from existing events without requiring significant structural refactoring, enabling the system to adapt more naturally to evolving real-world scenarios.1  
* **True Service Autonomy:** Feature slices achieve genuine independence, as each is responsible for defining and managing its own consistency without relying on complex orchestration or hidden dependencies across the system.1 This significantly simplifies the process of adding new features or modifying existing decision logic.1  
* **Superior Auditability and "Time Travel" Capabilities:** Inheriting from general Event Sourcing principles, AES retains all historical facts, providing an exceptionally strong audit log. This enables powerful "time travel" capabilities for debugging, thorough root cause analysis, and conducting "what if" scenarios by replaying events to reconstruct past states.2

A fundamental shift in the architectural philosophy underpinning event-sourced systems is observed. Traditional DDD with aggregates typically positions the aggregate itself as the transactional consistency boundary and the primary representation of business rules.8 However, in AES, the repeated emphasis is on the removal of rigid entities and aggregates.1 Instead, consistency is managed "per command, based on the context that command cares about".4 This indicates a profound reorientation: the event log, as a sequence of immutable facts, becomes the unquestionable source of truth.10 The traditional "domain model," often conceived as a mutable object graph, is supplanted by "pure functions" operating on "query-defined contexts".1 This architectural evolution implies that domain logic becomes more stateless and functional, mitigating the "impedance mismatch" 9 not merely between objects and relational databases, but more fundamentally between a mutable object model and an immutable event stream. The consequence is that domain logic is no longer centered on mutating a central object but on interpreting a stream of facts to inform new decisions. This suggests that development teams considering AES may find substantial advantage in cultivating a robust understanding of functional programming paradigms.

Another significant observation is the strong connection between AES and functional programming. While Event Sourcing is often associated with DDD, which typically employs an object-oriented approach, several sources explicitly link AES to functional programming principles.3 They highlight the use of "pure functions," "immutable events," and the "I/O via imperative shell" pattern.3 This connection points to a broader trend in software architecture, where functional paradigms are increasingly leveraged to manage complexity in distributed environments. The elimination of stateful aggregates aligns seamlessly with functional programming's preference for stateless computation and immutability. This suggests that AES is not merely an alternative event sourcing variant, but a specific, advanced application of functional principles designed to achieve clear, flexible, and comprehensible systems within a distributed context.3 The approach inherently fosters a more modular and testable design by clearly separating concerns into pure functional cores and imperative shells.

Furthermore, the commonly perceived "cost of entry" for Event Sourcing appears to be significantly mitigated by the aggregateless approach. A common concern is that "many teams love the idea of an immutable event log yet never adopt it because classic Event Sourcing demand aggregates, per-entity streams, and deep Domain-Driven Design... \[which\] raises the cost of entry".8 However, proponents of AES assert that removing aggregates "unlocks simplicity" 1 and that "no sophisticated framework is required".3 The core complexity in traditional Event Sourcing often stems from the intricacies of managing aggregate boundaries, the process of rehydration, and the associated object-oriented overhead.3 By consciously eliminating these elements, AES directly addresses a substantial portion of the perceived complexity. This indicates that AES could substantially lower the barrier to entry for adopting event sourcing, making it a more accessible and attractive option for teams who found traditional, DDD-heavy Event Sourcing too complex or cost-prohibitive to implement. The apparent contradiction between the general perception of Event Sourcing's complexity and the simplicity claimed by AES advocates underscores this potential for broader adoption.

## **2\. Achieving Consistency: Command Context Consistency**

In aggregateless event sourcing, the concept of a consistency boundary is decoupled from a fixed aggregate entity. Instead, it is dynamically defined by the "context" that is relevant to a specific decision or command being executed.1 This "context" is precisely the minimal set of facts (events) that are necessary for a particular decision to be made safely and correctly.1 For instance, when an operation involves binding a device to an asset, the system only needs to verify the existence and availability of that specific device and asset, rather than rehydrating their entire historical state.1 This dynamic approach results in consistency boundaries that are flexible and simple, adapting precisely to the requirements of the current decision or action.1

### **The Role of Event Queries in Defining Context**

When a command is received and a decision needs to be made, the system initiates a query against the event log to establish its relevant context.1 These queries are designed to retrieve only the necessary events and do not inherently require explicit tags or IDs in every event, as the event itself can be self-descriptive, and the context is derived from the specific query criteria.1 The query is constructed using an

EventFilter, which specifies the relevant event types and, if needed, includes key-value checks within the JSON payload of the events to narrow down the context.6 This filter is critically important because it serves a dual purpose: defining the context to load for decision-making and acting as the guard condition when attempting to append new events.6

### **Mechanisms for Ensuring Consistency (Atomic Inserts, Optimistic Locking with CTE)**

Consistency in AES is primarily ensured by guaranteeing that the observed context remains unchanged from the moment it is read until the new event(s) generated by the command are successfully recorded.1 This guarantee is achieved through an

**atomic insert operation** that incorporates a crucial check: it verifies that no new events have appeared in the event log that would invalidate the context initially observed by the command.1

A robust and common implementation for this atomic check, particularly in relational databases like PostgreSQL, involves **optimistic locking using a Common Table Expression (CTE)**.6 The CTE first re-evaluates the filter condition that was used during the initial context read. It then identifies the

MAX(sequence\_number) (or a similar version identifier) among all events that match this re-evaluated filter. This max\_seq represents the latest event within the context that the command initially observed. The INSERT statement for the new events is then made conditional. It will only proceed if the max\_seq obtained from the context CTE is still precisely equal to the expectedMaxSequence that was returned by the query operation when the command first loaded its context.6 If the

max\_seq has changed due to a concurrent write, it signals a concurrency conflict. In this scenario, the INSERT operation will fail silently (i.e., it will not insert any rows), providing a clear signal for the system to retry the entire command.6 This mechanism effectively replaces complex, blocking locking approaches.1

### **Handling Concurrent Operations and Conflict Resolution**

The optimistic locking approach, particularly when implemented with CTEs, allows multiple writers to operate in parallel without introducing blocking locks.6 Should the context change due to another concurrent operation, the current write attempt is rejected, and the operation can be retried.1 This retry loop is a fundamental component of optimistic concurrency control.7 In traditional aggregate-based systems, concurrency is often managed by versioning each aggregate's event stream, ensuring that only one update per aggregate version is committed.7 While AES doesn't use explicit aggregate versions, the

sequence\_number in the event store, combined with the max\_seq check in the CTE, serves an analogous purpose by providing a version for the specific query-defined context.6

### **Event Ordering and Versioning Considerations**

The consistency of events within the event store and their precise chronological order are paramount, as the sequence of changes directly determines the current state of any derived model.2 To ensure correct ordering and aid in debugging, adding a timestamp to every event is beneficial. Furthermore, annotating events with an incremental identifier, such as a

sequence\_number, is a common and effective practice.6 In scenarios where two concurrent actions attempt to add events that affect the same context simultaneously, the event store can be designed to reject one of the events if it matches an existing entity identifier and event identifier, thereby preventing inconsistencies.6 A core principle of event sourcing is that event data, once recorded, should be immutable and never updated. Any logical "changes" or "undos" are instead represented by appending new, compensating events to the log.10 Schema evolution of events requires careful planning. Strategies include ensuring that event handlers are designed to support all historical versions of events or, less ideally, updating historical events to a new schema (though the latter compromises immutability).10

A profound implication of aggregateless event sourcing is that the "context" for a command is a dynamic, query-defined transactional boundary, rather than a static domain object. In traditional Event Sourcing, aggregates serve as explicit, static transactional boundaries.8 However, AES explicitly "removes aggregates".1 The core mechanism for consistency, "command context consistency" 4, is "defined purely through querying relevant facts".1 This indicates a fundamental shift from a predefined, object-centric boundary to a dynamically constructed, data-centric boundary. The transactional scope is no longer a fixed entity but a flexible set of events, determined by the command's immediate needs. This flexibility is a direct consequence of treating events as pure facts. It allows for more granular and potentially wider consistency checks that might span what would traditionally be multiple, tightly coupled aggregates.1 This design choice can significantly reduce the "wrong aggregate boundary" problem 5 by adapting the consistency scope to the actual business decision rather than a fixed domain model construct.

Another important aspect is how optimistic concurrency in AES leverages the event log's chronological nature as an implicit versioning system. While optimistic locking in traditional Event Sourcing often relies on explicit aggregate versions 7, AES, despite lacking explicit aggregates, utilizes the

MAX(sequence\_number) within the queried context as its optimistic lock mechanism.6 This highlights a subtle yet powerful characteristic: the inherent chronological ordering and immutability of the event log 2 provides a natural, implicit versioning system. The

sequence\_number effectively acts as a global or contextual version. This means that even without a "domain model" in memory to explicitly hold a version, the event store itself, when queried for a specific context, provides the necessary "version" (the latest sequence\_number in that context) to ensure consistency. This simplifies the command-side logic by offloading version management to the persistence layer's inherent properties, reinforcing the "pure logic, pure persistence" separation.

A key architectural pattern enabling the management of complexity in AES is the "Imperative Shell, Functional Core." Sources 6 explicitly describe this pattern, where a "Functional core" consists of pure functions that take a list of past events plus an incoming command and return new events or an error, with no side effects. The "Imperative shell" then handles all input and output, such as loading the context from a database and atomically persisting the computation results. This architectural pattern is a direct consequence of the "pure logic, pure persistence" separation.1 It isolates the complexities of distributed systems, concurrency, and I/O within the "shell." This isolation makes the core business logic highly testable, as pure functions are deterministic and do not require mocks. It also makes the logic easier to reason about. This provides a robust framework for building reliable distributed systems by clearly delineating responsibilities and containing side effects, which is particularly beneficial in the context of AES's flexible consistency boundaries.

## 

## **3\. Implementing the Aggregateless Event Store**

The implementation of an aggregateless event store adheres to a set of core design principles that prioritize immutability, chronological ordering, and flexible data representation.

### **Core Design Principles for an Aggregateless Event Store**

* **Append-Only Log:** The foundational requirement for any event store, including an aggregateless one, is that it must operate as an append-only log. Events are stored immutably and strictly in chronological order.2 This characteristic inherently provides a complete and unalterable audit trail, enabling the reconstruction of any past state by replaying the sequence of events.2  
* **Immutable Events:** Once an event is recorded in the store, it is considered a historical fact and should never be updated or deleted. Any subsequent logical "changes" or "undos" to the system state are represented by appending new, compensating events to the log.10  
* **Single Table Schema (Recommended for RDBMS):** For relational database implementations, a single events table is the recommended approach to store all events, irrespective of their specific type or the "entity" they conceptually relate to.6 A typical schema for this table would include a  
  sequence\_number (serving as a BIGSERIAL PRIMARY KEY for unique identification and strict ordering), occurred\_at (a timestamp indicating when the event happened), event\_type (a text field specifying the event's classification), payload (a JSONB column to store the full event data, offering high flexibility for varying event structures), and metadata (an optional JSONB field for tracking additional context like tracing IDs, source systems, or user IDs).6 This unified schema significantly simplifies database migrations and schema evolution, as new event types or changes to existing event payloads do not necessitate DDL changes to the core event store table. It also facilitates flexible querying of diverse events within a single logical unit.6  
* **Minimal Interface:** The event store should expose a minimal, well-defined interface. The two core operations are typically query (to load events based on a specified filter, defining the command's context) and append (to add new events, incorporating a crucial consistency check).6

### **Optimizations: Snapshots without a Domain Model**

Snapshots are a critical performance optimization designed to reduce the time and computational load required to reconstruct a system's state, especially when dealing with event streams that have accumulated a very large number of events.8 Even in the absence of a traditional "aggregate" domain model, the underlying principle remains valid: a snapshot captures the

*derived state* of a specific context at a particular point in time.16 This derived state then serves as an efficient starting point, requiring only the events that occurred

*after* the snapshot to be replayed to reconstruct the current state.9

#### **Strategies for Snapshot Creation**

Several tactics exist for deciding when to take a snapshot:

* **After each event:** This approach virtually eliminates event replay, as business logic can always operate on the latest snapshot. However, it can significantly impact write performance if snapshot storage is synchronous with event persistence.16  
* **Every N number of events:** A widely adopted tactic where only a maximum of N events need to be replayed after loading the most recent snapshot.16  
* **When a specified event type occurs:** This strategy aligns snapshots with significant business milestones, such as a CashierShiftEnded event, mirroring a "closing the books" pattern.16  
* **Every selected period:** Snapshots can be scheduled to occur at regular intervals (e.g., daily, hourly). The risk here is that high event processing spikes between scheduled snapshots might diminish their performance benefit.16  
* **When initialization time exceeds a threshold:** Snapshots can be dynamically triggered if the time taken to reconstruct a context's state from events surpasses a predefined performance threshold.18

#### **Storage Options for Snapshots**

Snapshots can be stored in various locations:

* **As events in the same or separate stream:** Snapshots can be stored as special events within the primary event log or in a dedicated snapshot stream.16 A separate stream is often recommended for simpler lifetime management and easier rebuilding processes.16  
* **In a separate database:** Snapshots can be stored in a distinct database, such as a relational, document, or key-value store.16 This separate store often serves as the read model itself, providing optimized querying capabilities.17  
* **In-memory or Cache (e.g., Redis):** Utilizing in-memory or caching solutions offers the advantage of setting a Time-To-Live (TTL) for snapshots, which can reduce the need for complex data migration when the snapshot schema evolves. However, this necessitates rebuilding the snapshot after it expires or is invalidated.16

#### **Reconstructing State from Snapshots in an Aggregateless Context**

The process involves first loading the most recent snapshot for the relevant query-defined context (if one exists). Subsequently, only the events that occurred *after* that snapshot's recorded sequence\_number are replayed to derive the current state.9 In an aggregateless setup, the snapshot explicitly captures the derived state of the

*query-defined context* rather than a fixed aggregate object.16 The snapshot itself typically includes metadata, such as the

max\_sequence\_number or stream\_revision, indicating the point in the event stream from which subsequent events should be read.16

#### **Disadvantages and Design Considerations for Snapshots**

While beneficial, snapshots also come with considerations:

* **Potential Design Flaw Indicator:** The very need for snapshots might suggest an underlying design flaw in the domain model, indicating that event streams could potentially be shorter-lived through more refined domain modeling (e.g., by applying a "closing the books" pattern).16  
* **Versioning Problem:** As the structure of business objects evolves, data migration for existing snapshots can become complicated, particularly if snapshots are also serving as read models.16 A simpler alternative might be to discard old snapshots and rebuild them from the complete event stream using the new schema.19  
* **Performance Impact:** Storing snapshots synchronously with event writes can introduce performance overhead.16 Asynchronous snapshot creation, typically using event subscriptions, is often recommended to mitigate this impact, though it still requires careful management.16  
* **Consistency with Read Models:** If a snapshot is directly used as a read model, it is crucial to ensure that events and snapshots are saved within the same transaction. Alternatively, a robust retry mechanism must be in place to handle scenarios where event persistence succeeds but snapshot updates fail, maintaining eventual consistency.17

A significant understanding regarding snapshots in aggregateless event sourcing is that they function as a "derived state cache" for specific query contexts, rather than representing "aggregate state." In traditional Event Sourcing, snapshots are typically understood as capturing the state of an aggregate.8 However, AES explicitly lacks a traditional aggregate. Despite this, snapshots are still utilized for performance optimization.8 The sources refer to them as "the current state of an aggregate at a particular point in time" 16 or "the persistent state of the aggregate".8 In an aggregateless context, this "aggregate" refers to the

*derived state* of a specific, query-defined context.16 Therefore, snapshots are essentially a cached, materialized view of a particular slice of the event log, optimized for faster "rehydration" of that specific context. They are not snapshots of a mutable domain object. This implies that snapshot design in AES is primarily driven by read-model optimization and query performance rather than being an intrinsic part of the write-model consistency mechanism, which is handled by the atomic append operation. This distinction is crucial for understanding how to effectively implement and manage snapshots in an aggregateless architecture.

## 

## **4\. Technology Suitability for an Aggregateless Event Store**

The choice of technology for implementing an aggregateless event store is critical, as it must support the core principles of append-only storage, immutability, efficient querying of context, and robust optimistic concurrency. Various database types offer different strengths and weaknesses in this role.

### **RDBMS (Relational Database Management Systems)**

Relational databases like PostgreSQL are viable candidates for an aggregateless event store, particularly for systems with moderate to high transaction volumes where strong consistency and transactional guarantees are paramount.

* **Implementation Details:** A common approach involves a single events table with columns such as sequence\_number (primary key, auto-incrementing for ordering), occurred\_at (timestamp), event\_type (for classification), payload (JSONB for flexible event data), and metadata (JSONB for additional context).6 This schema simplifies evolution, as new event types or payload changes do not require DDL modifications to the core table.6  
* **Optimistic Locking with CTE:** PostgreSQL, for instance, excels at implementing the crucial optimistic locking mechanism using Common Table Expressions (CTEs).6 This allows an atomic check and insert operation, ensuring that new events are only appended if the observed context (identified by the latest  
  sequence\_number matching the query filter) remains unchanged.6 If the context has changed, the insert fails silently, signaling a concurrency conflict and prompting a retry.6  
* **Advantages:** RDBMS offers strong ACID guarantees, ensuring data integrity and consistency.20 Their mature transactional capabilities are well-suited for the atomic append operations required by event sourcing.3 They provide robust indexing capabilities (e.g., GIN and B-tree indexes for JSONB columns in PostgreSQL) for efficient querying of event contexts.6  
* **Considerations:** While append-only operations are generally efficient, complex queries to reconstruct state can be slow if not offloaded to read models (CQRS).9 Performance can be a bottleneck for extremely high throughput scenarios if not properly indexed and partitioned.15 Schema evolution for events, while simplified by JSONB, still requires careful management in application code.10

### **NoSQL Databases**

NoSQL databases offer flexibility and scalability, making them attractive for event stores.

* **MongoDB:** MongoDB can be used to build an event store, particularly given its document-oriented nature which aligns well with storing events as JSON-like objects.21 It supports flexible schemas for event payloads. The critical requirement is the ability to enforce atomic writes and optimistic concurrency, which MongoDB can achieve through mechanisms like versioning or custom logic.21 It is suitable for scenarios where high throughput and flexible querying of event data are needed.6  
* **Cassandra:** Cassandra is known for its high write throughput and availability, making it a potential candidate for storing large volumes of events.23 However, its eventual consistency model and lack of native transactional support across multiple operations make it less straightforward for the atomic consistency checks required by the write model in event sourcing.21 It is more commonly considered for the read-side (projections) in CQRS architectures.23 Implementing optimistic concurrency and strict event ordering in Cassandra for the write model would require significant custom application-level logic.24  
* **Advantages:** NoSQL databases often provide high scalability, flexible schemas, and high write performance, which are beneficial for an append-only event log.20  
* **Considerations:** Ensuring strong consistency and atomic writes, especially for the optimistic locking pattern, can be more complex and require more application-level logic compared to RDBMS.21 The choice depends heavily on the specific NoSQL database and its consistency model.20

### **Apache Kafka**

Apache Kafka is a distributed streaming platform often used in event-driven architectures, but its suitability as a *primary* event store for event sourcing has nuances.

* **Role in Event Streaming vs. Event Sourcing:** Kafka excels as an event *streaming* platform, providing a distributed, durable, and scalable log for real-time event distribution and processing.14 It is ideal for connecting various microservices and driving processes from event streams.14 Event Sourcing, while related, is a more specialized pattern focused on storing the  
  *source of truth* for an application's state as immutable events.14  
* **Limitations as Primary Event Store:**  
  * **Transactional Guarantees:** While Kafka has transaction capabilities, providing the strict atomicity needed for the optimistic locking pattern (read context, decide, atomically write new events if context unchanged) can be challenging.27 Traditional event stores focus on consistency and durability, whereas Kafka's primary focus is delivery, throughput, and integration.21  
  * **Ordering:** Kafka guarantees ordering *within a partition*, but not across partitions.27 This can complicate replaying events for a specific context if events are spread across multiple partitions and strict global or contextual ordering is required for state reconstruction.27  
  * **Querying:** Kafka is not designed for arbitrary queries against historical data to reconstruct specific states efficiently. Replaying all events from a topic to derive a state can be slow and resource-intensive, especially with large event counts.28 While state stores (e.g., RocksDB with Kafka Streams) can cache derived states, they are typically projections for reads, not the primary write-side event store.28  
* **Appropriate Role:** Kafka is best utilized *in conjunction* with a dedicated event store. It can serve as an event *bus* to publish events from the event store to various read models and downstream consumers, facilitating real-time reactions and integrations.2 It provides the "data in motion" aspect, while a dedicated event store handles the "data at rest" source of truth.26 Snapshots can be built from Kafka topics, but this typically involves external processing engines like Tinybird.14

### 

### **Specialized Event Stores (e.g., EventStoreDB)**

Dedicated event store technologies are purpose-built for event sourcing and often provide native support for its core requirements.

* **EventStoreDB:** EventStoreDB is a popular choice specifically designed for event sourcing.29 It natively supports appending events, optimistic concurrency control, and reading events from streams.29 It provides persistent subscriptions for real-time event delivery to consumers and offers built-in system projections (e.g.,  
  $by\_category) to group events by type or category, which can be adapted to an aggregateless context.29  
* **Advantages:** Purpose-built for event sourcing, offering strong guarantees for event ordering and atomic writes.21 Simplifies implementation of optimistic concurrency, as it's a native feature.29 Provides robust subscription mechanisms for building read models and integrations.29 Supports clustering for high availability and durability.29  
* **Considerations:** While EventStoreDB does not provide snapshotting functionality out-of-the-box, it can be easily implemented by storing derived states in separate streams.19 Sharding also requires manual implementation.29 Its primary focus is the event log, meaning complex analytical queries often still require separate read models.29

### 

### **Comparative Analysis and Recommendations**

| Feature / Technology | RDBMS (e.g., PostgreSQL) | NoSQL (e.g., MongoDB) | Apache Kafka (as primary store) | Specialized Event Store (e.g., EventStoreDB) |
| :---- | :---- | :---- | :---- | :---- |
| **Append-Only Log** | Excellent | Good | Excellent | Excellent |
| **Immutable Events** | Excellent | Excellent | Excellent | Excellent |
| **Single Table Schema** | Recommended | Flexible (document) | N/A (topics/partitions) | N/A (streams) |
| **Optimistic Concurrency** | Achievable with CTEs 6 | Achievable with custom logic/versions 21 | Challenging for atomic write-time checks 27 | Native support 29 |
| **Querying Event Context** | Good (JSONB indexes) 6 | Good (flexible queries) 21 | Poor (not designed for arbitrary queries) 28 | Good (stream-based reads) 29 |
| **Snapshots** | Implementable (separate table/JSON) 16 | Implementable (separate collection/cache) 16 | Implementable (external processing) 14 | Implementable (separate streams) 29 |
| **Transactional Guarantees** | Strong ACID | Varies (eventual/strong) 20 | Eventual (within partitions) 27 | Strong (atomic writes) 21 |
| **Scalability** | Moderate to High | High | Very High | High |
| **Complexity** | Moderate | Moderate to High | High (as primary store) | Moderate |

For an aggregateless event sourcing architecture, the choice of event store technology is contingent on specific system requirements, particularly concerning consistency guarantees, throughput, and operational complexity.

* **For robust, strongly consistent write models with moderate to high throughput:** A **Relational Database like PostgreSQL** is a highly suitable choice. Its native transactional capabilities and the ability to implement optimistic locking with CTEs provide the necessary atomic guarantees for command context consistency.6 The JSONB capabilities allow for flexible event schema evolution without complex DDL changes.  
* **For highly scalable, flexible write models where eventual consistency for projections is acceptable (and strong consistency for writes is managed at the application layer):** **NoSQL databases like MongoDB** can be considered. They offer schema flexibility and high write throughput. However, the implementation of optimistic concurrency might require more custom logic compared to RDBMS.  
* **For systems prioritizing real-time event distribution and integration across many services:** **Apache Kafka** is invaluable as an event *streaming platform* and *event bus*. However, it is generally *not recommended as the primary event store* for the write model due to its limitations in providing the strict transactional guarantees and efficient arbitrary querying needed for command context consistency.27 It should complement a dedicated event store.  
* **For systems where event sourcing is a core architectural pillar and native support for event stream operations is desired:** A **Specialized Event Store like EventStoreDB** is an excellent fit. It is purpose-built for event sourcing, offering native support for core requirements like optimistic concurrency and persistent subscriptions.29 While snapshotting needs to be implemented, its stream-centric model aligns well with the aggregateless philosophy.

The selection should ultimately balance the need for strong consistency in the write path with the flexibility and scalability requirements of the overall system. Often, a combination of technologies (e.g., PostgreSQL or EventStoreDB for the write-side event store, and Kafka for event distribution to various read models built on other databases) provides the most robust and performant solution.

## **5\. Conclusions**

Aggregateless Event Sourcing (AES) presents a compelling evolution in event-driven architectures, offering a path to greater simplicity, flexibility, and independent feature slices by moving beyond the rigid boundaries of traditional Domain-Driven Design aggregates. This architectural style redefines consistency not around fixed domain objects, but dynamically, based on the specific context a command requires, achieved through precise event queries and atomic write operations leveraging optimistic locking.

The analysis indicates that the philosophical shift from a "domain model as source of truth" to an "event log as source of truth, with context as the consistency boundary" is fundamental to AES. This transition encourages a more functional approach to domain logic, where pure functions operate on immutable event streams, inherently reducing the impedance mismatch common in traditional state-based systems. This functional alignment, coupled with the "imperative shell, functional core" pattern, significantly enhances testability and maintainability, making complex distributed systems more manageable. Furthermore, by addressing the complexities associated with aggregate management, AES has the potential to lower the barrier to entry for adopting event sourcing, making its powerful benefits more accessible to a wider range of development teams.

Implementing an aggregateless event store requires adherence to core principles: an append-only log of immutable events, often best served by a single, flexible table schema (e.g., JSONB in RDBMS). Snapshots, even without a traditional aggregate model, remain a vital optimization. They function as cached, derived states of specific query contexts, enabling efficient state reconstruction by replaying only recent events. However, careful consideration is needed regarding snapshot creation strategies, storage locations, and the potential for introducing new versioning complexities. The decision to implement snapshots should be driven by demonstrated performance needs, and their design should prioritize supporting flexible query contexts rather than fixed object states.

Regarding technology choices, **Relational Databases (e.g., PostgreSQL)** are highly suitable due to their strong transactional guarantees and ability to implement robust optimistic locking with CTEs, which is critical for command context consistency. **NoSQL databases (e.g., MongoDB)** offer flexibility and scalability, but require more deliberate application-level design to ensure transactional integrity for the write model. **Apache Kafka**, while excellent for event streaming and distribution, is generally not recommended as the *primary* event store for the write model due to its inherent limitations in strict transactional guarantees and arbitrary historical querying; its strength lies in complementing a dedicated event store as an event bus. **Specialized Event Stores (e.g., EventStoreDB)** are purpose-built for event sourcing and provide native support for many core requirements, simplifying implementation.

In conclusion, adopting aggregateless event sourcing is a strategic decision that can lead to more adaptable and resilient systems. It is particularly well-suited for domains where consistency boundaries are fluid, requirements evolve frequently, and a rich, auditable history of changes is paramount. Teams considering this architecture should prioritize a deep understanding of functional programming principles, carefully design their event schemas for evolution, and select an event store technology that natively supports atomic appends and optimistic concurrency for query-defined contexts, potentially leveraging a hybrid approach with streaming platforms for broader event distribution.

#### **References**

1. Aggregateless Event Sourcing \- Rico Fritzsche, Accessed Juli 18, 2025, [https://ricofritzsche.me/aggregateless-event-sourcing/](https://ricofritzsche.me/aggregateless-event-sourcing/)  
2. Beginner's Guide to Event Sourcing \- Kurrent.io, Accessed Juli 18, 2025, [https://www.kurrent.io/event-sourcing](https://www.kurrent.io/event-sourcing)  
3. Beyond Aggregates: Lean, Functional Event Sourcing \- Rico Fritzsche, Accessed Juli 18, 2025, [https://ricofritzsche.me/functional-event-sourcing/](https://ricofritzsche.me/functional-event-sourcing/)  
4. blog.ricofritzsche.de, Accessed Juli 18, 2025, [https://blog.ricofritzsche.de/event-sourcing-building-an-event-store-without-ddd-aggregates-828e88fe804d\#:\~:text=Event%20sourcing%20without%20DDD%20aggregates%20is%20an%20emerging%20approach%20that,context%20that%20command%20cares%20about.](https://blog.ricofritzsche.de/event-sourcing-building-an-event-store-without-ddd-aggregates-828e88fe804d#:~:text=Event%20sourcing%20without%20DDD%20aggregates%20is%20an%20emerging%20approach%20that,context%20that%20command%20cares%20about.)  
5. Event Sourcing — Oops, wrong Aggregate Boundary | by Ashraf Mageed | Nerd For Tech, Accessed Juli 18, 2025, [https://medium.com/nerd-for-tech/event-sourcing-oops-wrong-aggregate-boundary-74b4e98249f4](https://medium.com/nerd-for-tech/event-sourcing-oops-wrong-aggregate-boundary-74b4e98249f4)  
6. How I Built an Aggregateless Event Store with TypeScript and PostgreSQL \- Rico Fritzsche, Accessed Juli 18, 2025, [https://ricofritzsche.me/how-i-built-an-aggregateless-event-store-with-typescript-and-postgresql/](https://ricofritzsche.me/how-i-built-an-aggregateless-event-store-with-typescript-and-postgresql/)  
7. cqrs \- Consistency in event sourcing \- Stack Overflow, Accessed Juli 18, 2025, [https://stackoverflow.com/questions/78763275/consistency-in-event-sourcing](https://stackoverflow.com/questions/78763275/consistency-in-event-sourcing)  
8. CQRS \+ Event Sourcing for the Rest of Us : r/softwarearchitecture \- Reddit, Accessed Juli 18, 2025, [https://www.reddit.com/r/softwarearchitecture/comments/1l0yobs/cqrs\_event\_sourcing\_for\_the\_rest\_of\_us/](https://www.reddit.com/r/softwarearchitecture/comments/1l0yobs/cqrs_event_sourcing_for_the_rest_of_us/)  
9. Pattern: Event sourcing \- Microservices.io, Accessed Juli 18, 2025, [https://microservices.io/patterns/data/event-sourcing.html](https://microservices.io/patterns/data/event-sourcing.html)  
10. Event Sourcing pattern \- Azure Architecture Center | Microsoft Learn, Accessed Juli 18, 2025, [https://learn.microsoft.com/en-us/azure/architecture/patterns/event-sourcing](https://learn.microsoft.com/en-us/azure/architecture/patterns/event-sourcing)  
11. 1 Year of Event Sourcing and CQRS | by Teiva Harsanyi \- ITNEXT, Accessed Juli 18, 2025, [https://itnext.io/1-year-of-event-sourcing-and-cqrs-fb9033ccd1c6](https://itnext.io/1-year-of-event-sourcing-and-cqrs-fb9033ccd1c6)  
12. Understanding Event Sourcing with Marten, Accessed Juli 18, 2025, [https://martendb.io/events/learning](https://martendb.io/events/learning)  
13. Event sourcing, one event, state of two aggregates changed, Accessed Juli 18, 2025, [https://softwareengineering.stackexchange.com/questions/342033/event-sourcing-one-event-state-of-two-aggregates-changed](https://softwareengineering.stackexchange.com/questions/342033/event-sourcing-one-event-state-of-two-aggregates-changed)  
14. Event sourcing with Kafka: A practical example \- Tinybird, Accessed Juli 18, 2025, [https://www.tinybird.co/blog-posts/event-sourcing-with-kafka](https://www.tinybird.co/blog-posts/event-sourcing-with-kafka)  
15. cqrs \- Using an RDBMS as event sourcing storage \- Stack Overflow, Accessed Juli 18, 2025, [https://stackoverflow.com/questions/7065045/using-an-rdbms-as-event-sourcing-storage](https://stackoverflow.com/questions/7065045/using-an-rdbms-as-event-sourcing-storage)  
16. Snapshots in Event Sourcing \- Kurrent.io, Accessed Juli 18, 2025, [https://www.kurrent.io/blog/snapshots-in-event-sourcing](https://www.kurrent.io/blog/snapshots-in-event-sourcing)  
17. Snapshots in Event Sourcing for Write Models \- Google Groups, Accessed Juli 18, 2025, [https://groups.google.com/g/dddcqrs/c/Es\_ARzZKjZc](https://groups.google.com/g/dddcqrs/c/Es_ARzZKjZc)  
18. Event Snapshots \- AxonIQ Docs, Accessed Juli 18, 2025, [https://docs.axoniq.io/axon-framework-reference/4.11/tuning/event-snapshots/](https://docs.axoniq.io/axon-framework-reference/4.11/tuning/event-snapshots/)  
19. Aggregates Snapshot in EventStoreDB \- Kurrent Discuss Forum, Accessed Juli 18, 2025, [https://discuss.kurrent.io/t/aggregates-snapshot-in-eventstoredb/2967](https://discuss.kurrent.io/t/aggregates-snapshot-in-eventstoredb/2967)  
20. Is it useful to implement both an SQL database and an NO-SQL ..., Accessed Juli 18, 2025, [https://www.quora.com/Is-it-useful-to-implement-both-an-SQL-database-and-an-NO-SQL-database-for-using-event-sourcing-in-a-highly-complex-web-application](https://www.quora.com/Is-it-useful-to-implement-both-an-SQL-database-and-an-NO-SQL-database-for-using-event-sourcing-in-a-highly-complex-web-application)  
21. How to build MongoDB Event Store \- Event-Driven.io, Accessed Juli 18, 2025, [https://event-driven.io/en/mongodb\_event\_store/](https://event-driven.io/en/mongodb_event_store/)  
22. PDMLab/mongo-eventstore: A simple MongoDB event store \- GitHub, Accessed Juli 18, 2025, [https://github.com/PDMLab/mongo-eventstore](https://github.com/PDMLab/mongo-eventstore)  
23. Cassandra \+ kafka for event sourcing \- Stack Overflow, Accessed Juli 18, 2025, [https://stackoverflow.com/questions/35524462/cassandra-kafka-for-event-sourcing](https://stackoverflow.com/questions/35524462/cassandra-kafka-for-event-sourcing)  
24. Cassandra \+ kafka for event sourcing \- Google Groups, Accessed Juli 18, 2025, [https://groups.google.com/g/dddcqrs/c/ZLQkv9kH46o](https://groups.google.com/g/dddcqrs/c/ZLQkv9kH46o)  
25. Event sourcing in Go. : r/golang \- Reddit, Accessed Juli 18, 2025, [https://www.reddit.com/r/golang/comments/fo2pn6/event\_sourcing\_in\_go/](https://www.reddit.com/r/golang/comments/fo2pn6/event_sourcing_in_go/)  
26. Event Sourcing vs Stream Processing: Progressing to Real-Time ..., Accessed Juli 18, 2025, [https://developer.confluent.io/courses/event-sourcing/event-sourcing-vs-event-streaming/](https://developer.confluent.io/courses/event-sourcing/event-sourcing-vs-event-streaming/)  
27. Event Sourcing: Why Kafka is not suitable as an Event Store : r/apachekafka \- Reddit, Accessed Juli 18, 2025, [https://www.reddit.com/r/apachekafka/comments/w3hd74/event\_sourcing\_why\_kafka\_is\_not\_suitable\_as\_an/](https://www.reddit.com/r/apachekafka/comments/w3hd74/event_sourcing_why_kafka_is_not_suitable_as_an/)  
28. Event sourcing using Kafka. When building an event sourced system ..., Accessed Juli 18, 2025, [https://blog.softwaremill.com/event-sourcing-using-kafka-53dfd72ad45d](https://blog.softwaremill.com/event-sourcing-using-kafka-53dfd72ad45d)  
29. eugene-khyst/eventstoredb-event-sourcing: EventStoreDB ... \- GitHub, Accessed Juli 18, 2025, [https://github.com/eugene-khyst/eventstoredb-event-sourcing](https://github.com/eugene-khyst/eventstoredb-event-sourcing)  
30. eventstoredb-event-sourcing/README.md at main \- GitHub, Accessed Juli 18, 2025, [https://github.com/evgeniy-khist/eventstoredb-event-sourcing/blob/main/README.md](https://github.com/evgeniy-khist/eventstoredb-event-sourcing/blob/main/README.md)