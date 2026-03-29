namespace Rulixa.Infrastructure.Rendering;

internal static class VisualOutputJavaScriptAsset
{
    public static string Content =>
        """
        (() => {
          const dataElement = document.getElementById("rulixa-visual-data");
          if (!dataElement) {
            return;
          }

          const documentModel = JSON.parse(dataElement.textContent);
          const state = {
            activeViewId: documentModel.views[0]?.id ?? "overview",
            inspectorId: documentModel.initialInspectorId,
            query: ""
          };

          const headerHost = document.querySelector('[data-role="header-summary"]');
          const navHost = document.querySelector('[data-role="view-nav"]');
          const viewHost = document.querySelector('[data-role="view-host"]');
          const inspectorHost = document.querySelector('[data-role="inspector"]');
          const searchInput = document.querySelector('[data-role="search-input"]');

          renderHeader();
          renderNav();
          renderView();
          renderInspector();
          wireEvents();

          function wireEvents() {
            searchInput?.addEventListener("input", (event) => {
              state.query = event.target.value.trim().toLowerCase();
              renderView();
            });

            document.querySelectorAll('[data-role="jump-view"]').forEach((button) => {
              button.addEventListener("click", () => {
                state.activeViewId = button.dataset.targetView;
                renderNav();
                renderView();
              });
            });
          }

          function renderHeader() {
            const header = documentModel.header;
            headerHost.innerHTML = "";
            const grid = createElement("div", "header-grid");
            grid.appendChild(createHeaderCard("Root", [header.rootEntry, `goal: ${header.goal}`, `resolved kind: ${header.resolvedKind}`]));
            grid.appendChild(createHeaderCard("Center State", header.centerStates.length ? header.centerStates : ["unknown"]));
            grid.appendChild(createHeaderCard("System Summary", [header.systemSummary ?? "system summary は未確定です。"]));
            grid.appendChild(createHeaderCard("Next Candidates", header.nextCandidates.length ? header.nextCandidates : ["none"]));
            headerHost.appendChild(grid);
          }

          function renderNav() {
            navHost.innerHTML = "";
            documentModel.views.forEach((view) => {
              const button = document.createElement("button");
              button.type = "button";
              button.dataset.viewId = view.id;
              button.className = view.id === state.activeViewId ? "is-active" : "";
              button.innerHTML = `<strong>${view.label}</strong><br><span>${view.summary}</span>`;
              button.addEventListener("click", () => {
                state.activeViewId = view.id;
                renderNav();
                renderView();
              });
              navHost.appendChild(button);
            });
          }

          function renderView() {
            const view = documentModel.views.find((item) => item.id === state.activeViewId);
            if (!view) {
              return;
            }

            viewHost.innerHTML = "";
            const title = createElement("div", "section-card");
            title.innerHTML = `<p class="eyebrow">${view.label}</p><h2>${view.summary}</h2>`;
            viewHost.appendChild(title);
            view.sections.forEach((section) => viewHost.appendChild(createSection(section)));
          }

          function createSection(section) {
            const wrapper = createElement("section", "view-section");
            wrapper.dataset.view = state.activeViewId;
            const head = createElement("div", "section-head");
            const titleBlock = document.createElement("div");
            titleBlock.innerHTML = `<h3>${section.title}</h3><p class="section-summary">${describeSection(section)}</p>`;
            head.appendChild(titleBlock);

            const toggle = document.createElement("button");
            toggle.type = "button";
            toggle.className = "collapse-toggle";
            toggle.dataset.role = "collapse-toggle";
            toggle.textContent = section.collapsible ? "Collapse" : "Fixed";
            head.appendChild(toggle);
            wrapper.appendChild(head);

            const body = createElement("div", "section-body");
            renderSectionBody(section, body);
            wrapper.appendChild(body);

            if (section.collapsible) {
              toggle.addEventListener("click", () => {
                body.classList.toggle("is-collapsed");
                toggle.textContent = body.classList.contains("is-collapsed") ? "Expand" : "Collapse";
              });
            } else {
              toggle.disabled = true;
            }

            return wrapper;
          }

          function renderSectionBody(section, host) {
            const items = filterItems(section.items);
            const gridClass = section.kind === "cards" ? "card-grid" : section.kind === "table" ? "table-grid" : "list-grid";
            const grid = createElement("div", gridClass);
            items.forEach((item) => grid.appendChild(createInteractiveItem(section.kind, item)));

            if (!items.length) {
              const empty = createElement("div", "section-card");
              empty.textContent = "検索条件に一致する項目はありません。";
              host.appendChild(empty);
            } else {
              host.appendChild(grid);
            }

            if (section.graph) {
              host.appendChild(renderGraph(section.graph));
            }
          }

          function createInteractiveItem(kind, item) {
            const className = kind === "cards" ? "item-card" : kind === "table" ? "table-item" : "list-item";
            const wrapper = createElement("article", className);
            wrapper.dataset.searchText = item.searchText.toLowerCase();

            const button = document.createElement("button");
            button.type = "button";
            button.dataset.inspectorTarget = item.inspectorId ?? "";
            button.innerHTML = `<strong>${item.title}</strong><p>${item.summary}</p>`;
            if (item.inspectorId) {
              button.addEventListener("click", () => {
                state.inspectorId = item.inspectorId;
                renderInspector();
              });
            }
            wrapper.appendChild(button);

            if (item.meta.length) {
              const meta = createElement("div", "item-meta");
              item.meta.forEach((tag) => {
                const span = document.createElement("span");
                span.textContent = tag;
                meta.appendChild(span);
              });
              wrapper.appendChild(meta);
            }

            return wrapper;
          }

          function renderGraph(graph) {
            const host = createElement("div", "workflow-graph");
            host.dataset.role = "workflow-graph";
            const svgNs = "http://www.w3.org/2000/svg";
            const svg = document.createElementNS(svgNs, "svg");
            svg.setAttribute("viewBox", `0 0 ${Math.max(600, graph.nodes.length * 180)} 260`);
            host.appendChild(svg);

            graph.edges.forEach((edge) => {
              const fromIndex = graph.nodes.findIndex((node) => node.id === edge.from);
              const toIndex = graph.nodes.findIndex((node) => node.id === edge.to);
              if (fromIndex < 0 || toIndex < 0) {
                return;
              }

              const line = document.createElementNS(svgNs, "line");
              line.setAttribute("x1", `${120 + fromIndex * 170}`);
              line.setAttribute("y1", "120");
              line.setAttribute("x2", `${120 + toIndex * 170}`);
              line.setAttribute("y2", "120");
              line.setAttribute("stroke", "#ab4f2f");
              line.setAttribute("stroke-width", "3");
              svg.appendChild(line);
            });

            graph.nodes.forEach((node, index) => {
              const group = document.createElementNS(svgNs, "g");
              const rect = document.createElementNS(svgNs, "rect");
              rect.setAttribute("x", `${40 + index * 170}`);
              rect.setAttribute("y", "80");
              rect.setAttribute("width", "160");
              rect.setAttribute("height", "72");
              rect.setAttribute("rx", "18");
              rect.setAttribute("fill", node.emphasized ? "#ffe6d2" : "#fffaf2");
              rect.setAttribute("stroke", "#ab4f2f");
              rect.setAttribute("stroke-width", node.emphasized ? "3" : "1.5");
              group.appendChild(rect);

              const text = document.createElementNS(svgNs, "text");
              text.setAttribute("x", `${120 + index * 170}`);
              text.setAttribute("y", "122");
              text.setAttribute("text-anchor", "middle");
              text.setAttribute("dominant-baseline", "middle");
              text.setAttribute("font-size", "14");
              text.setAttribute("font-family", "Yu Gothic UI, sans-serif");
              text.textContent = node.label;
              group.appendChild(text);
              svg.appendChild(group);
            });

            return host;
          }

          function renderInspector() {
            inspectorHost.innerHTML = "";
            const inspector = documentModel.inspectorItems[state.inspectorId];
            if (!inspector) {
              inspectorHost.appendChild(createEmptyInspector());
              return;
            }

            inspectorHost.appendChild(createInspectorCard("Category", [inspector.category]));
            inspectorHost.appendChild(createInspectorCard("Facts", inspector.facts));
            if (inspector.filePath) {
              inspectorHost.appendChild(createInspectorCard("File", [inspector.filePath]));
            }
            if (inspector.symbol) {
              inspectorHost.appendChild(createInspectorCard("Symbol", [inspector.symbol]));
            }
            if (inspector.reason) {
              inspectorHost.appendChild(createInspectorCard("Reason", [inspector.reason]));
            }
            if (inspector.snippet) {
              inspectorHost.appendChild(createCodeInspector(inspector.snippet));
            }
            if (inspector.candidates?.length) {
              inspectorHost.appendChild(createInspectorCard("Candidates", inspector.candidates));
            }
          }

          function createHeaderCard(title, lines) {
            const card = createElement("section", "header-card");
            card.innerHTML = `<p class="eyebrow">${title}</p>`;
            card.appendChild(createList(lines));
            return card;
          }

          function createInspectorCard(title, lines) {
            const card = createElement("section", "inspector-card");
            card.innerHTML = `<p class="eyebrow">${title}</p>`;
            card.appendChild(createList(lines));
            return card;
          }

          function createCodeInspector(snippet) {
            const card = createElement("section", "inspector-card");
            card.innerHTML = `<p class="eyebrow">Snippet</p>`;
            const pre = document.createElement("pre");
            const code = document.createElement("code");
            code.textContent = snippet;
            pre.appendChild(code);
            card.appendChild(pre);
            return card;
          }

          function createEmptyInspector() {
            const card = createElement("section", "inspector-card");
            card.innerHTML = `<p class="eyebrow">Inspector</p><p>項目を選択すると詳細を表示します。</p>`;
            return card;
          }

          function createList(lines) {
            const list = createElement("ul", "plain-list");
            lines.forEach((line) => {
              const item = document.createElement("li");
              item.textContent = line;
              list.appendChild(item);
            });
            return list;
          }

          function createElement(tag, className) {
            const element = document.createElement(tag);
            element.className = className;
            return element;
          }

          function filterItems(items) {
            if (!state.query) {
              return items;
            }
            return items.filter((item) => item.searchText.toLowerCase().includes(state.query));
          }

          function describeSection(section) {
            switch (section.id) {
              case "overview-subsystems": return "sub-map cards";
              case "workflow-routes": return "局所 workflow 図";
              case "evidence-files": return "selected files";
              case "unknowns-list": return "unknown guidance";
              case "architecture-constraints": return "constraints";
              default: return section.kind;
            }
          }
        })();
        """;
}
