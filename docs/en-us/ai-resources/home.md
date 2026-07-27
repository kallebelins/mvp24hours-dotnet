# AI & MCP Resources

Mvp24Hours separates documentation by audience:

- Human architecture decisions live in [Architecture Guides](../guides/architecture/home.md).
- Packages, APIs, options, and DI registration live in canonical module documentation.
- Machine retrieval should use the external Mvp24Hours MCP service when its public endpoint and repository are available.

## Current machine context

- [Compact LLM context](../../llms_compact_en.txt)
- [Complete LLM context](../../llms_complete_en.txt)
- [Cursor rule](../../mvp24hours.mdc)

These files are compatibility resources. Canonical human documentation wins if generated context conflicts with source or tests.

## External AI frameworks

Semantic Kernel and Microsoft Agent Framework are not Mvp24Hours product modules. Their former templates are no longer first-class library documentation. The external MCP/AI project destination is still pending; this page will link it after ownership and a stable URL are confirmed.

Semantic Kernel Graph documentation lives at [skgraph.dev](https://skgraph.dev/) and the docs repository [semantic-kernel-graph-docs](https://github.com/kallebelins/semantic-kernel-graph-docs). The earlier `semantic-kernel-graph` repository URL is not the current public docs destination.

## Retrieval order for agents

1. Use the Mvp24Hours MCP when the endpoint becomes available.
2. Use canonical module documentation and Architecture Guides.
3. Use the compatibility downloads above only as an offline fallback.
4. Verify exact APIs against source and tests.
