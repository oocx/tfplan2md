# ADR-004: Use CSS Layers for Example Style Isolation

## Status

Accepted

## Context

The tfplan2md website (GitHub Pages) displays rendered examples of tool output to show users what the markdown will look like in Azure DevOps and GitHub. These rendered examples need to approximate the target platform's styling (Azure DevOps, GitHub) without interference from the website's own styles.

The problem surfaced when website CSS rules (like `border-radius: 6px` on `details` elements) interfered with rendered examples, making them look different from actual Azure DevOps rendering. Any change to website styles could break examples, and any fix for examples could break the website.

We need **style isolation** so that:
1. Website styles don't interfere with example rendering
2. Example styles don't interfere with website appearance
3. The solution is agent-maintainable (AI agents will maintain this website)

We evaluated six options:

### Option 1: iframe Isolation
Load rendered examples in `<iframe>` elements with separate stylesheets.

**Pros:**
- Complete style isolation (zero interference)
- Browser handles scoping automatically
- Can load actual platform CSS if needed

**Cons:**
- More complex to maintain (separate HTML files or data URIs)
- Height management requires JavaScript (ResizeObserver)
- Accessibility: screen readers treat as separate document
- More complex agent workflow

**Agent maintainability:** Medium

### Option 2: Shadow DOM
Attach Shadow DOM to `.rendered-view` containers with scoped styles.

**Pros:**
- Native browser encapsulation (styles don't leak)
- Clean separation without iframes
- Good browser support

**Cons:**
- Requires JavaScript to attach shadow roots
- Some CSS features behave differently in Shadow DOM
- Theme switching needs explicit handling
- Agent must understand Shadow DOM API

**Agent maintainability:** Medium-High

### Option 3: CSS Layers
Use `@layer` to create isolated style layers with controlled cascade priority.

```css
@layer website, examples;

@layer website {
  /* All website styles */
}

@layer examples {
  .rendered-view { /* isolated styles */ }
}
```

**Pros:**
- Native CSS feature (no JavaScript)
- Explicit cascade control
- Easy to understand and maintain
- Future-proof modern CSS solution

**Cons:**
- Requires refactoring existing CSS structure
- Browser support: Chrome 99+, Firefox 97+, Safari 15.4+ (sufficient for technical audience)
- All website styles must be wrapped in `@layer website`

**Agent maintainability:** High - clear, declarative CSS

### Option 4: CSS Reset (all: revert)
Apply `all: revert` to rendered examples, then rebuild needed styles.

**Pros:**
- Simple concept
- Works in all modern browsers

**Cons:**
- Aggressive - resets everything including display, position
- Must rebuild all needed styles from scratch
- Can break layout properties unexpectedly

**Agent maintainability:** Low - unpredictable side effects

### Option 5: Strict BEM/Namespacing
Use highly specific class naming like `.example-rendered__*` for all styles.

**Pros:**
- No special browser features needed
- Works everywhere
- Easy pattern for agents to follow

**Cons:**
- **No actual isolation** - just convention
- Website tag selectors (like `code`, `details`, `table`) still interfere
- Doesn't solve the current problem

**Agent maintainability:** High - but doesn't provide isolation

### Option 6: Separate Stylesheet + containment
Load `examples.css` after `style.css`, use `contain: style` property.

**Pros:**
- Clear separation of concerns (two files)
- Later stylesheet wins cascade conflicts

**Cons:**
- `contain: style` only prevents counter/quote leakage (limited scope)
- Doesn't prevent website tag selectors from affecting examples
- Still need specificity management

**Agent maintainability:** Medium - two files but clear boundaries

## Decision

Use **CSS Layers (`@layer`)** to isolate example styles from website styles.

## Rationale

CSS Layers provide the best balance of:
- **True isolation**: Explicit cascade control prevents interference
- **Agent maintainability**: Declarative CSS is easy for AI agents to understand and modify
- **Modern best practice**: CSS Layers are the standard solution for cascade management
- **No JavaScript**: Pure CSS solution keeps implementation simple
- **Incremental adoption**: Can refactor existing code gradually

Browser support is sufficient:
- Chrome 99+ (March 2022)
- Firefox 97+ (February 2022)
- Safari 15.4+ (March 2022)
- Edge 99+ (March 2022)

Our technical audience (DevOps engineers, developers) will have modern browsers.

## Implementation Plan

1. Define layer order at top of `style.css`:
   ```css
   @layer base, website, examples;
   ```

2. Wrap existing website styles in `@layer website { ... }`

3. Create `@layer examples { ... }` for `.rendered-view` styles

4. Base layer can contain CSS resets or shared utilities if needed

5. Layer order ensures examples layer always wins cascade conflicts

## Consequences

### Positive

- **Style isolation**: Website styles cannot interfere with examples, and vice versa
- **Maintainability**: Clear separation makes it obvious which styles affect what
- **Agent-friendly**: Declarative CSS is easy for AI agents to understand and modify
- **Future-proof**: Modern CSS standard that will be supported long-term
- **No JavaScript**: Keeps implementation simple and performant

### Negative

- **Refactoring required**: Must wrap all existing website CSS in `@layer website { ... }`
- **Browser support**: Drops IE11 (already unsupported, not a concern for technical audience)
- **Learning curve**: Team/agents must understand CSS cascade layers (well-documented feature)

## Related Decisions

- Website must remain agent-maintainable (core design goal)
- Examples must accurately represent Azure DevOps and GitHub rendering
- Solution must work without complex build tools (GitHub Pages constraint)
