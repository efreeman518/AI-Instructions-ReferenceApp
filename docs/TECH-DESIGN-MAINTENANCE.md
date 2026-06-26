# Tech Design Maintenance

`tech-design.html` is the canonical technical design document. Do not regenerate it from Markdown, and do not add a parallel `tech-design.md`.

Update rules:

- Keep `README.md` short and operational. Link to `docs/tech-design.html` for architecture detail.
- Edit `tech-design.html` directly when code, architecture, routes, workflows, tests, or deployment topology change.
- Keep styling in `tech-design.css` and theme behavior in `tech-design.js`.
- Keep diagrams as SVG files under `docs/diagrams/`, referenced from `tech-design.html`.
- Preserve stable heading `id` attributes because README links and external references may depend on them.
- Patch only affected sections. Do not reformat the whole HTML file for a small content change.
- Prefer semantic HTML: headings, tables, lists, `figure`, `img`, `code`, and `pre`.
- After edits, verify local references with a search for stale `tech-design.md` links and missing diagram paths.
