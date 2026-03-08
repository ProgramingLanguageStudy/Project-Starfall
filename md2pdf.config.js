// md-to-pdf 설정 (Mermaid 다이어그램 지원)
const renderer = {
  code(code, infostring) {
    if (infostring === "mermaid") {
      return `<pre class="mermaid">${code}</pre>`;
    }
    return false;
  },
};

module.exports = {
  marked_extensions: [{ renderer }],
  script: [
    { url: "https://cdn.jsdelivr.net/npm/mermaid/dist/mermaid.min.js" },
    {
      content:
        "mermaid.initialize({ startOnLoad: false, flowchart: { useMaxWidth: true, nodeSpacing: 25, rankSpacing: 35 } }); (async () => { await mermaid.run(); })();",
    },
  ],
  pdf_options: {
    format: "A4",
    printBackground: true,
    margin: { top: "20mm", right: "20mm", bottom: "20mm", left: "20mm" },
  },
  stylesheet: [
    "https://cdnjs.cloudflare.com/ajax/libs/github-markdown-css/5.2.0/github-markdown.min.css",
    "./mermaid-pdf.css",
  ],
  body_class: "markdown-body",
};
