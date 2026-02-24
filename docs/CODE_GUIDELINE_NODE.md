# Node Coding Standard: Explicit State & Scope

## 🎯 Core Principle: Clarity > Brevity
We prioritize code that explicitly declares its intent, storage, and scope. We avoid "compiler magic" (implicit captures, implicit returns) to ensure code is searchable, immutable, and easy to reason about without deep knowledge of specific language version quirks.

---

## 1. 📂 File & Module Structure

### Filenames
* **Rule:** Use **`kebab-case`** for all files and directories (e.g., `user-profile-controller.ts`).
* **Why:** Node.js imports are **file-path based**, not class-name based. Using lowercase everywhere prevents "Case Sensitivity" bugs (working on Mac/Windows but crashing on Linux/Docker) caused by mismatched casing in import paths.
* **Example:**
    * ✅ `user-profile.ts`
    * ❌ `UserProfile.ts`

### Exports
* **Rule:** Use **Named Exports** only. **Default Exports are forbidden.**
* **Why:** Named exports ensure the import name matches the definition, making "Find All References" and renaming reliable across the codebase.
* **Example:**
    ```typescript
    // ✅ Do
    export const formatDate = (date: Date) => { ... }

    // ❌ Don't
    export default (date: Date) => { ... }
    ```

---

## 2. 🛡️ Explicit State & Control Flow

### No Implicit Returns
* **Rule:** Always use explicit block bodies `{ ... }` and the `return` keyword in functions longer than one line or when returning objects.
* **Why:** Implicit returns make it harder to add logging/debugging later and can lead to confusing object literal syntax errors.
* **Example:**
    ```typescript
    // ✅ Do
    const getUser = (id: string) => {
      return db.user.find(id);
    }

    // ❌ Don't (Harder to debug later)
    const getUser = (id: string) => db.user.find(id);
    ```

### Explicit Types (No "Any")
* **Rule:** `any` is strictly forbidden. Use `unknown` if the type is truly uncertain, and narrow it with type guards.
* **Rule:** Explicitly define return types for public/exported functions.

### Searchable Constants
* **Rule:** Strings and numbers with business logic meaning must be assigned to `const` variables with semantic names.
* **Example:** `const MAX_RETRY_ATTEMPTS = 3;` instead of `if (x > 3)`.

---

## 3. ⚡ Async & Error Handling

### Explicit Async/Await
* **Rule:** Use `async/await` for all asynchronous flows. Avoid `.then()` chains.
* **Why:** Flattens the "callback hell" pyramid and makes try/catch scoping clear.

### Safe Error Handling
* **Rule:** Errors must be strictly typed or guarded.
* **Example:**
    ```typescript
    try {
      await processFile();
    } catch (error) {
      if (error instanceof Error) { // Narrow the type
        logger.error(error.message);
      }
    }
    ```

---

## 4. 🧱 Immutability & Variables

### Const by Default
* **Rule:** Use `const` for everything. Use `let` only if reassignment is mathematically necessary.
* **Forbidden:** `var` is strictly forbidden.

### Pure Functions
* **Rule:** Functions should avoid side effects (modifying arguments/global state) whenever possible. Returns new objects instead of mutating inputs.

---

## 5. 🌐 Next.js & React Specifics

### Images & Links
* **Rule:** ALWAYS use `next/image` for images.
* **Rule:** ALWAYS use `next/link` for internal navigation.

### Null Safety
* **Rule:** Handle `null` and `undefined` explicitly. Do not rely on loose equality (`==`).
* **Example:** `if (user !== null)` or use optional chaining `user?.name`.

### Component Definition
* **Rule:** Use Function Declarations or Arrow Functions assigned to `const`.
* **Rule:** Prop interfaces should be named `{ComponentName}Props`.