namespace Rulixa.Infrastructure.Rendering;

internal static class VisualOutputCssAsset
{
    public static string Content =>
        """
        :root {
          --bg: #f4f1ea;
          --panel: #fffaf2;
          --panel-strong: #f0e7d6;
          --ink: #1d1d1b;
          --muted: #5e5a52;
          --accent: #ab4f2f;
          --accent-soft: #f3c3a1;
          --border: #d6c7ad;
          --shadow: 0 18px 48px rgba(43, 35, 22, 0.12);
          --radius: 18px;
          --radius-sm: 12px;
          --mono: "Consolas", "SFMono-Regular", monospace;
          --sans: "Yu Gothic UI", "Hiragino Sans", sans-serif;
        }

        * { box-sizing: border-box; }
        body {
          margin: 0;
          font-family: var(--sans);
          color: var(--ink);
          background:
            radial-gradient(circle at top left, rgba(171, 79, 47, 0.12), transparent 32%),
            linear-gradient(180deg, #fcfaf6 0%, var(--bg) 100%);
        }

        .app-shell {
          display: grid;
          grid-template-columns: 260px minmax(0, 1fr) 320px;
          gap: 16px;
          min-height: 100vh;
          padding: 16px;
        }

        .nav-pane, .main-pane, .inspector-pane {
          background: rgba(255, 250, 242, 0.88);
          border: 1px solid var(--border);
          border-radius: var(--radius);
          box-shadow: var(--shadow);
          backdrop-filter: blur(8px);
        }

        .nav-pane, .inspector-pane {
          padding: 20px;
          position: sticky;
          top: 16px;
          align-self: start;
        }

        .main-pane {
          padding: 20px;
          display: flex;
          flex-direction: column;
          gap: 20px;
        }

        .eyebrow {
          margin: 0 0 6px;
          font-size: 12px;
          letter-spacing: 0.12em;
          text-transform: uppercase;
          color: var(--muted);
        }

        .brand-title, .header-summary h2, .inspector-header h2 {
          margin: 0;
        }

        .brand-summary, .header-summary p, .inspector-body p, .section-summary {
          color: var(--muted);
          line-height: 1.6;
        }

        .search-block {
          display: flex;
          flex-direction: column;
          gap: 6px;
          margin: 20px 0;
        }

        .search-block input {
          width: 100%;
          border-radius: 999px;
          border: 1px solid var(--border);
          background: white;
          padding: 11px 14px;
          font: inherit;
        }

        .view-nav {
          display: grid;
          gap: 8px;
        }

        .view-nav button, .jump-links button, .collapse-toggle {
          border: 1px solid var(--border);
          background: white;
          border-radius: var(--radius-sm);
          padding: 10px 12px;
          text-align: left;
          cursor: pointer;
          font: inherit;
          transition: transform 120ms ease, border-color 120ms ease, background 120ms ease;
        }

        .view-nav button.is-active {
          background: linear-gradient(180deg, #fff4e8 0%, #ffe5cf 100%);
          border-color: var(--accent);
          transform: translateY(-1px);
        }

        .jump-links {
          display: grid;
          gap: 8px;
          margin-top: 20px;
        }

        .header-grid {
          display: grid;
          grid-template-columns: repeat(2, minmax(0, 1fr));
          gap: 12px;
        }

        .header-card, .section-card, .inspector-card {
          background: white;
          border: 1px solid var(--border);
          border-radius: var(--radius-sm);
          padding: 14px;
        }

        .view-section {
          border: 1px solid var(--border);
          border-radius: var(--radius);
          background: rgba(255, 255, 255, 0.88);
          overflow: hidden;
        }

        .section-head {
          display: flex;
          align-items: center;
          justify-content: space-between;
          gap: 12px;
          padding: 16px 18px;
          background: var(--panel-strong);
        }

        .section-body {
          padding: 18px;
          display: grid;
          gap: 14px;
        }

        .section-body.is-collapsed {
          display: none;
        }

        .card-grid, .list-grid, .table-grid {
          display: grid;
          gap: 12px;
        }

        .card-grid {
          grid-template-columns: repeat(auto-fit, minmax(220px, 1fr));
        }

        .item-card, .list-item, .table-item {
          border: 1px solid var(--border);
          border-radius: var(--radius-sm);
          background: white;
          padding: 14px;
          display: grid;
          gap: 8px;
        }

        .item-card button, .list-item button, .table-item button {
          all: unset;
          cursor: pointer;
        }

        .item-meta {
          display: flex;
          flex-wrap: wrap;
          gap: 8px;
        }

        .item-meta span {
          border-radius: 999px;
          background: var(--panel-strong);
          padding: 4px 8px;
          font-size: 12px;
          color: var(--muted);
        }

        .workflow-graph {
          width: 100%;
          min-height: 280px;
          border: 1px dashed var(--border);
          border-radius: var(--radius-sm);
          background: linear-gradient(180deg, rgba(240, 231, 214, 0.45), rgba(255, 255, 255, 0.8));
        }

        .workflow-graph svg {
          width: 100%;
          height: 100%;
          display: block;
        }

        .inspector-body {
          display: grid;
          gap: 12px;
        }

        .plain-list {
          margin: 0;
          padding-left: 18px;
          display: grid;
          gap: 6px;
        }

        code {
          font-family: var(--mono);
          font-size: 0.95em;
        }

        @media (max-width: 1200px) {
          .app-shell {
            grid-template-columns: 1fr;
          }

          .nav-pane, .inspector-pane {
            position: static;
          }

          .header-grid {
            grid-template-columns: 1fr;
          }
        }
        """;
}
