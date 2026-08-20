# Skill Creation Template

> **Purpose**: Template for creating or revising Mvp24Hours specialist skills  
> **Status**: Catalog complete (26 skills). Use this structure for updates.

## Template Structure

Each skill must follow this layered format (300-500 lines):

### 1. Header Section

```markdown
# [Skill Name] - Mvp24Hours [Architect|Specialist]

> **Role**: [One-sentence mission statement]  
> **MCP Integration**: Query [relevant docs/templates] via Mvp24Hours MCP DevKit

## Role & Expertise

You are a **[Role Name]** for Mvp24Hours solutions. Your mission is to [primary objective].

### Core Responsibilities
- [Responsibility 1]
- [Responsibility 2]
- [Responsibility 3]
- [Responsibility 4]

## Core Competencies

### [Competency Category 1]
- **[Item 1]**: Description
- **[Item 2]**: Description

### [Competency Category 2]
- **[Item 1]**: Description
```

### 2. Decision Framework Section

```markdown
## Decision Framework

**MCP Reference**:
\`\`\`bash
get_architecture_template "templateId": "[template-id]"
get_sample_tree "sampleId": "[sample-id]"
\`\`\`

### When to Use [This Pattern/Technology]

✅ **Choose [Pattern] When**:
- [Criterion 1]
- [Criterion 2]
- [Criterion 3]

❌ **Don't Choose [Pattern] When**:
- [Anti-criterion 1]
- [Anti-criterion 2]

### vs Alternative Approaches

| Aspect | [This Pattern] | [Alternative 1] | [Alternative 2] |
|--------|---------------|-----------------|-----------------|
| **[Aspect 1]** | [Value] | [Value] | [Value] |
| **[Aspect 2]** | [Value] | [Value] | [Value] |
```

### 3. Architecture Patterns Section

```markdown
## Architecture Patterns

### [Pattern 1]

**MCP Query**:
\`\`\`bash
get_doc "path": "docs/en-us/[path]"
get_sample_tree "sampleId": "[sample-id]"
\`\`\`

**Structure**:
\`\`\`
[Project/folder structure]
\`\`\`

**When to Use**: [Description]

**Mvp24Hours Packages**:
\`\`\`xml
<PackageReference Include="Mvp24Hours.[Package]" />
\`\`\`

**Key Characteristics**:
- [Characteristic 1]
- [Characteristic 2]

**Trade-offs**:
- ✅ [Advantage 1]
- ✅ [Advantage 2]
- ❌ [Disadvantage 1]
- ❌ [Disadvantage 2]
```

### 4. Implementation Guide Section

```markdown
## Implementation Guide

### 1. [Implementation Step 1]

**MCP Resource**: `mvp24hours://[resource-path]`

\`\`\`csharp
// [Step 1 code example using Mvp24Hours APIs]
using Mvp24Hours.[Namespace];

[Code block with comments explaining key points]
\`\`\`

**Key Principles**:
- [Principle 1]
- [Principle 2]

### 2. [Implementation Step 2]

[Repeat pattern]
```

### 5. Anti-Patterns Section

```markdown
## Anti-Patterns & Pitfalls

### 1. [Anti-Pattern Name]

**❌ WRONG**:
\`\`\`csharp
// [Example of incorrect approach]
[Code showing anti-pattern]
\`\`\`

**✅ CORRECT**:
\`\`\`csharp
// [Example of correct approach]
[Code showing correct pattern]
\`\`\`

**Why**: [Explanation of why correct approach is better]
```

### 6. Migration Paths Section

```markdown
## Migration Paths

### From [Simple Pattern] to [Complex Pattern]

**MCP Tool**:
\`\`\`bash
plan_architecture_migration 
  "current": "[current-pattern]",
  "target": "[target-pattern]"
\`\`\`

**Steps**:

1. **[Step 1]**
   \`\`\`bash
   [Commands or actions]
   \`\`\`

2. **[Step 2]**
   [Description]

[Continue for all steps]
```

### 7. Integration Scenarios Section

```markdown
## Integration Scenarios

### [Pattern 1] + [Pattern 2]

**Structure**:
\`\`\`
[Combined structure]
\`\`\`

**Setup**:
\`\`\`csharp
[Integration code example]
\`\`\`

**Benefit**: [Why this combination is valuable]

**Consult**: `[related-specialist-1].md`, `[related-specialist-2].md`
```

### 8. Testing Strategy Section

```markdown
## Testing Strategy

### [Test Type 1]

\`\`\`csharp
// [Test example relevant to this pattern]
public class [TestClass]
{
    [Fact]
    public async Task [TestName]()
    {
        // Arrange
        [Setup]

        // Act
        [Action]

        // Assert
        [Assertions]
    }
}
\`\`\`

**Key Points**:
- [Testing principle 1]
- [Testing principle 2]
```

### 9. Best Practices Checklist Section

```markdown
## Best Practices Checklist

### [Category 1]
- [ ] [Practice 1]
- [ ] [Practice 2]

### [Category 2]
- [ ] [Practice 1]
- [ ] [Practice 2]

[Additional categories as needed]
```

### 10. MCP Workflow Examples Section

```markdown
## MCP Workflow Examples

### [Workflow Name]

\`\`\`bash
# Step 1: [Description]
[MCP command]

# Step 2: [Description]
[MCP command]

# Step 3: [Description]
[MCP command]
\`\`\`
```

### 11. Further Resources Section

```markdown
## Further Resources

### Core MCP Resources
- `mvp24hours://[resource-1]` - [Description]
- `mvp24hours://[resource-2]` - [Description]

### Related Documentation (via MCP)
\`\`\`bash
search_docs "query": "[query-1]"
search_docs "query": "[query-2]"
\`\`\`

### Specialist Skills
- **[Related Skill 1]**: `[filename].md` - [Brief description]
- **[Related Skill 2]**: `[filename].md` - [Brief description]

### Mvp24Hours Packages
\`\`\`bash
dotnet add package Mvp24Hours.[Package1]
dotnet add package Mvp24Hours.[Package2]
\`\`\`

---

**Remember**: [Key takeaway for this specialty]
```

---

## Content Requirements

### MCP Integration (Critical)

Every skill MUST:

1. **Reference MCP tools in every major section**
   ```bash
   get_doc "path": "docs/en-us/..."
   get_architecture_template "templateId": "..."
   get_sample_tree "sampleId": "..."
   search_docs "query": "..."
   ```

2. **Link to samples via MCP** (not local paths)
   - ✅ `get_sample_tree "sampleId": "complex-cqrs-ef-customer-api"`
   - ❌ `samples/src/complex-cqrs-ef-customer-api` (local path)

3. **Query canonical documentation**
   - Use `mvp24hours://docs/[path]` URI format
   - Reference `search_docs` for discovery

### Code Examples (Critical)

Every skill MUST:

1. **Use Mvp24Hours APIs exclusively**
   - ✅ `AddMvpMediator()`, `IMediatorCommand<T>`
   - ❌ `AddMediatR()`, `IRequest<T>`

2. **Show complete, compilable examples**
   - Include necessary using statements
   - Show DI registration
   - Include configuration

3. **Demonstrate Mvp24Hours patterns**
   - Repository/UnitOfWork patterns
   - Specifications
   - Results pattern (`IBusinessResult<T>`)

### Decision Guidance (Critical)

Every skill MUST:

1. **Provide clear "when to use" criteria**
   - Quantifiable when possible (team size, domain complexity)
   - Business constraints (time, cost, expertise)

2. **Compare with alternatives**
   - Table format preferred
   - Clear trade-offs

3. **Include anti-pattern warnings**
   - Show wrong AND correct approaches
   - Explain why

### Sample References (Critical)

Every skill MUST:

1. **Link to relevant runnable samples**
   - Reference via MCP tools
   - Explain what each sample demonstrates

2. **Map samples to patterns**
   ```markdown
   | Pattern | Sample | When to Study |
   |---------|--------|---------------|
   ```

### Testing Guidance (Critical)

Every skill MUST:

1. **Show pattern-specific testing**
   - Unit test examples
   - Integration test examples
   - Use Mvp24Hours test helpers

2. **Reference test templates**
   ```bash
   get_test_scaffold "tier": "complex", "dataStore": "efcore"
   ```

---

## Content Sources

### Documentation to Query

Use `search_docs` and `get_doc` to reference:

- **Architecture**: `docs/en-us/guides/architecture/`
- **Core**: `docs/en-us/core/`
- **CQRS**: `docs/en-us/cqrs/`
- **Database**: `docs/en-us/database/`
- **Messaging**: `docs/en-us/broker.md`, `docs/en-us/broker-advanced.md`
- **Infrastructure**: `docs/en-us/infrastructure/`
- **Observability**: `docs/en-us/observability/`
- **Modernization**: `docs/en-us/modernization/`
- **Testing**: `docs/en-us/testing/`

### Two axes (never mix)

**Structure** (templates `minimal-api`, `simple-nlayers`, `complex-nlayers`): host/project layout. Confirm with `get_architecture_template`.

**Blueprint / Capability** (CQRS, DDD, Hexagonal, Clean, Event-Driven, Microservices, event sourcing, saga, Keycloak): pattern on top of a structure. Confirm sample `.Tier` with `list_samples` — **do not infer tier from the sample id prefix**. `complex-cqrs-ef-customer-api` is **Blueprint**, not Complex N-Layers.

### Samples (MCP `list_samples`) — required table in every skill

Use `list_samples`, `get_sample_tree`, `get_sample_file`. Every skill must include:

```markdown
## Samples (MCP `list_samples`)

Never infer tier from the sample id prefix.

| Sample id | MCP Tier | Role in this skill |
|-----------|----------|--------------------|
| `...` | Minimal \| Simple \| Complex \| Blueprint \| Capability | ... |
```

If a capability has no Minimal (or Simple) sample, say so and point to `solution-architect.md` for the structure to host it.

**Structure Minimal:** `minimal-crud-ef-customer-api`, `minimal-crud-mongodb-customer-api`, `minimal-pipeline-customer-api`

**Structure Simple:** `simple-crud-ef-customer-api`, `simple-crud-ef-dapper-customer-api`, `simple-crud-ef-entitylog-customer-api`, `simple-crud-mongodb-customer-api`, `simple-crud-redis-customer-api`, `simple-rabbitmq-customer-api`, `simple-observability-customer-api`, `simple-pipeline-customer-api`, `simple-hybridcache-rate-limit-api`, `simple-cronjob-worker`, `simple-webstatus`

**Structure Complex:** `complex-crud-ef-customer-api`, `complex-crud-ef-dapper-customer-api`, `complex-crud-ef-entitylog-customer-api`, `complex-crud-mongodb-customer-api`, `complex-pipeline-customer-api`, `complex-pipeline-builder-customer-api`, `complex-pipeline-ef-customer-api`, `complex-pipeline-ports-adapters-customer-api`

**Blueprint:** `complex-cqrs-ef-customer-api`, `complex-ddd-ef-customer-api`, `complex-hexagonal-customer-api`, `complex-clean-architecture-customer-api`, `complex-event-driven-rabbitmq-customer-api`, `microservices-aspire-customer`

**Capability:** `complex-event-sourcing-customer-api`, `complex-saga-rabbitmq-customer-api`, `complex-keycloak-customer-api`

### Templates to Reference

Use `get_architecture_template`:

**Structure:** `minimal-api`, `simple-nlayers`, `complex-nlayers`

**Blueprint:** `cqrs`, `ddd`, `clean-architecture`, `hexagonal`, `event-driven`, `microservices`

---

## Skill-Specific Guidance

### For Architect Skills (Broad)

Focus on:
- **Pattern selection** (decision trees)
- **Comparison tables** (when to use X vs Y)
- **Integration** with other patterns
- **Migration paths** between patterns
- **Multiple samples** showing variants

### For Specialist Skills (Deep)

Focus on:
- **Advanced features** of the technology
- **Deep implementation** patterns
- **Performance optimization**
- **Edge cases and troubleshooting**
- **Production best practices**

---

## Quality Checklist

Before considering a skill complete, verify:

- [ ] **MCP-first approach**: Every section references MCP resources
- [ ] **Decision framework**: Clear "when to use" criteria with comparisons
- [ ] **Implementation guide**: Step-by-step with Mvp24Hours APIs
- [ ] **Anti-patterns**: At least 3-5 common mistakes with corrections
- [ ] **Code examples**: Complete, compilable, using Mvp24Hours packages
- [ ] **Migration paths**: At least 1 progression path
- [ ] **Integration scenarios**: How it works with 2-3 other patterns
- [ ] **Testing strategy**: Specific test examples
- [ ] **Sample references**: Links to 2-3 relevant samples via MCP
- [ ] **MCP tier table**: Samples table with official `list_samples` Tier (never treat `complex-*` id as Complex structure)
- [ ] **MCP workflow**: Concrete query examples
- [ ] **Length**: 300-500 lines
- [ ] **No local paths**: All references via MCP tools
- [ ] **Mvp24Hours-specific**: Uses Mvp24Hours APIs, not generic patterns

---

## Example: Creating `efcore-specialist.md`

### 1. Research Phase

```bash
# Query relevant documentation
search_docs "query": "ef core repository"
get_doc "path": "docs/en-us/database/relational.md"
get_doc "path": "docs/en-us/database/efcore-advanced.md"
get_doc "path": "docs/en-us/database/use-repository.md"

# Find relevant samples
list_samples  # Look for *-ef-* samples
get_sample_tree "sampleId": "simple-crud-ef-customer-api"
get_sample_tree "sampleId": "complex-crud-ef-customer-api"

# Get architecture context
get_architecture_template "templateId": "simple-nlayers"
```

### 2. Structure Phase

- **Role**: EF Core persistence specialist
- **Decision Framework**: When to use EF Core vs MongoDB vs Dapper
- **Patterns**: Repository, UnitOfWork, Specifications, Migrations
- **Implementation**: DbContext, Configurations, Interceptors
- **Anti-patterns**: N+1 queries, missing indexes, large aggregates
- **Migration**: Simple → Complex (add specifications, interceptors)
- **Integration**: With CQRS (separate read/write), with caching
- **Testing**: InMemory provider, Integration tests with real DB

### 3. Content Phase

Fill each section following the template, ensuring:
- Every pattern has MCP reference
- Code uses `AddMvp24HoursDbContext()`, `AddMvp24HoursRepositoryAsync()`
- Samples referenced via MCP tools
- Anti-patterns show EF-specific mistakes
- Testing uses Mvp24Hours test helpers

---

## Priority Order

Create skills in this order to maximize value:

### Phase 1: Core Patterns (High Priority)
1. `data/data-architect.md` - Foundational
2. `data/efcore-specialist.md` - Most common
3. `cqrs/cqrs-architect.md` - Frequently requested
4. `messaging/messaging-architect.md` - Key integration pattern

### Phase 2: Advanced Patterns (Medium Priority)
5. `cqrs/mediator-patterns-specialist.md` - CQRS implementation
6. `messaging/rabbitmq-advanced-specialist.md` - Messaging implementation
7. `observability/observability-architect.md` - Production requirement
8. `webapi/webapi-architect.md` - HTTP interface design

### Phase 3: Specialized Patterns (Lower Priority but Important)
9. `data/mongodb-specialist.md` - NoSQL alternative
10. `cqrs/event-sourcing-specialist.md` - Advanced CQRS
11. `messaging/saga-orchestration-specialist.md` - Distributed transactions
12. `observability/resilience-patterns-specialist.md` - Production hardening

### Phase 4: Remaining Patterns
13-24. Complete remaining skills following template

---

## Automation Potential

These skills could be generated with an AI agent using:

1. **Input**: Skill name + category
2. **Research**: Query MCP for relevant docs/samples
3. **Generate**: Follow this template
4. **Validate**: Check against quality checklist

**Prompt Template**:
```
Create a Mvp24Hours specialist skill for [SKILL_NAME].

Category: [CATEGORY]
Type: [Architect|Specialist]
Focus: [PRIMARY_FOCUS]

Follow the template in skills/SKILL_TEMPLATE.md.

Use MCP to query:
- Relevant documentation in docs/en-us/[AREA]/
- Samples: [LIST_RELEVANT_SAMPLES]
- Templates: [LIST_RELEVANT_TEMPLATES]

Include:
- Decision framework comparing with [ALTERNATIVES]
- Implementation using Mvp24Hours.[PACKAGES]
- Anti-patterns specific to [TECHNOLOGY]
- Integration with [RELATED_SKILLS]
- Testing with Mvp24Hours test helpers

Length: 300-500 lines
MCP-first approach throughout
```

---

## Next Steps

1. **Create remaining 24 skills** following this template
2. **Validate each skill** against quality checklist
3. **Update README.md** to mark skills as completed
4. **Test skills** by using them in real projects
5. **Iterate** based on feedback

---

**Note**: This template is intentionally prescriptive to ensure consistency across all 28 skills in the ecosystem. Each skill should feel like part of a cohesive system while providing deep, specialized guidance for its area.
