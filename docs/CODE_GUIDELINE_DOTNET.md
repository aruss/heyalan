# C# Coding Standard: Explicit State & Scope

## Core Principle: Clarity > Brevity

We prioritize code that explicitly declares its intent, storage, and scope. We avoid "compiler magic" (implicit captures) to ensure code is searchable, immutable, and easy to reason about without deep knowledge of specific language version quirks.

## 1. Class Dependencies (The Explicit Field Pattern)

For all logic-bearing classes (Services, Controllers, Repositories), state must be declared and initialized explicitly.

### Standard Requirements:

1. **Explicit Fields:** Define dependencies as `private readonly`.

2. **Traditional Constructors:** Perform assignments manually in the constructor.

3. **Guard Clauses:** Validate dependencies at the entry point (null checks).

4. **Member Access:** Always use the `this.` prefix when accessing instance fields, properties, or methods.

### Rationale:

* **Searchability:** `this.member` provides an unambiguous search target for IDEs and Grep.

* **Immutability:** Primary constructors do not support `readonly` parameters; explicit fields do.

* **Visual Scope:** Differentiates class state from local method variables at a glance.

* **No Magic:** Avoids the "hidden field" generation behavior of C# 12 Primary Constructors, where parameters are "captured" into invisible, mutable fields.

### ✅ Do (Explicit)

```csharp
public class SomeService
{
    private readonly ILogger logger;

    public SomeService(ILogger logger)
    {
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public void DoStuff()
    {
        // Explicit use of 'this.' identifies instance members vs local scope
        this.logger.Info("Executing logic...");
    }
}
```

### ❌ Don't Do (Implicit)

```csharp
// Avoid: Relies on compiler-generated hidden fields and lacks 'readonly' enforcement
public class SomeService(ILogger logger)
{
    public void DoStuff()
    {
        // Ambiguous: Is 'logger' a local variable or a captured parameter?
        logger.Info("Executing logic..."); 
    }
}
```

## 2. Explicit Typing vs. Type Inference

We favor explicit type declarations over the `var` keyword for local variables, particularly when the type is not immediately obvious from the right-hand side of the assignment.

### Standard Requirements:

* **Prefer Explicit Types:** Always declare the specific type for variables returned from method calls.

* **Avoid `var` for Results:** Do not use `var` when the type is hidden behind a method signature.

### Rationale:

* **Self-Documenting Code:** The code should tell the reader exactly what it is working with.

* **Zero-IDE Readability:** Developers should be able to understand the code during a GitHub PR review or in a plain text editor without using "Go to Definition" or hovering to see tooltips.

### ✅ Do (Explicit)

```csharp
GeneratedSpeechVoice selectedVoice = ParseVoice(voice);
```

### ❌ Don't Do (Implicit)

```csharp
var selectedVoice = ParseVoice(voice); // Type is hidden; requires investigation
```

## 3. Exceptions

* **Records:** Use the concise positional syntax for pure DTOs (Data Transfer Objects) where logic is absent and the primary goal is simple data carrying.

* **Constructors with `new()`:** The use of `var` is acceptable only when the type is explicitly stated on the right-hand side (e.g., `var list = new List<string>();`).


## API DTO Naming & Structure Guidelines

### 1. Operation-Centric Naming
DTO names must strictly correspond to the Route Handler (Operation) they serve. This creates a predictable 1:1 mapping between endpoints and their contracts.

* **Pattern:** `[HandlerName]Input` / `[HandlerName]Result`
* **Example:**
    * Handler: `GetBoards`
    * Request DTO: `GetBoardsInput`
    * Response DTO: `GetBoardsResult`

### 2. Input Suffix Rule
All complex objects bound from the request (Body, Query, or combined `[AsParameters]`) must use the suffix `Input`.

* **Constraint:** Do not use "Request", "Dto", or "Model".
* **Example:** `UpdateBoardInput`, `LoginInput`.

### 3. Result Suffix Rule
All objects returned by the handler must use the suffix `Result`.

* **Constraint:** Do not use "Response", "Dto", or "ViewModel".
* **Reasoning:** The "Response" is the HTTP envelope (Status Code + Headers); the "Result" is the payload provided by the application logic.

### 4. Generic Type Prohibition (The Inheritance Rule)
Generic collection types (e.g., `List<T>`, `CursorList<T>`, `IEnumerable<T>`) are **forbidden** as top-level return types. You must create a concrete class that inherits from the generic type to enforce a specific schema name in the OpenAPI specification.

* **Bad:** `public async Task<CursorList<Board>> GetBoards(...)` -> Generates schema `CursorListOfBoard`
* **Good:** `public async Task<GetBoardsResult> GetBoards(...)` -> Generates schema `GetBoardsResult`

**Implementation:**
```csharp
// Define the concrete type for the schema
public record GetBoardsResult : CursorList<Board>;

// Return the concrete type
return TypedResults.Ok(new GetBoardsResult(data));