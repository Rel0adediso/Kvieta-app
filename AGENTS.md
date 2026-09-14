# Repository exploration guidance

- Use Cerberus MCP as the primary method for repository exploration.
- Before reading full files, use Cerberus `search`, `get_symbol`, `context`, `blueprint`, `skeletonize`, `deps`, `call_graph`, or related tools.
- Prefer symbol-level and range-level reads over full-file reads.
- Only read entire files when necessary for editing or when Cerberus results are insufficient.
- Use Cerberus semantic search to locate relevant code before using grep or broad filesystem scans.
- Use Cerberus memory for durable project architecture and decision context when appropriate.
- Keep tool output and supplied context minimal to reduce token usage.
- Do not modify application behavior while creating or updating this instruction file.
