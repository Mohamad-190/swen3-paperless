// Gemeinsame Anzeige-Helfer fuer Dashboard und Detailseite.

const STATUS_LABELS = {
    Uploaded: "Hochgeladen",
    Processing: "In Bearbeitung",
    Completed: "Fertig",
    Failed: "Fehlgeschlagen"
};

const TYPE_LABELS = {
    "application/pdf": "PDF",
    "image/png": "PNG",
    "image/jpeg": "JPEG"
};

export function escapeHtml(value) {
    const div = document.createElement("div");
    div.textContent = value ?? "";
    return div.innerHTML;
}

export function statusBadge(status) {
    const label = STATUS_LABELS[status] ?? status;
    return `<span class="badge badge-${String(status).toLowerCase()}">${escapeHtml(label)}</span>`;
}

export function formatType(contentType) {
    return TYPE_LABELS[contentType] ?? contentType ?? "";
}

export function formatSize(bytes) {
    if (bytes < 1024) return `${bytes} B`;
    if (bytes < 1024 * 1024) return `${(bytes / 1024).toLocaleString("de-DE", { maximumFractionDigits: 0 })} KB`;
    return `${(bytes / (1024 * 1024)).toLocaleString("de-DE", { maximumFractionDigits: 1 })} MB`;
}

export function formatDate(iso) {
    return new Date(iso).toLocaleString("de-DE", {
        day: "2-digit",
        month: "2-digit",
        year: "numeric",
        hour: "2-digit",
        minute: "2-digit"
    });
}

// Zeigt eine Meldung unter einem Formular; Erfolgsmeldungen verschwinden nach ein paar Sekunden.
export function showMessage(element, text, type) {
    clearTimeout(element._hideTimer);
    element.textContent = text;
    element.className = `form-message is-${type}`;
    if (type === "success") {
        element._hideTimer = setTimeout(() => { element.textContent = ""; }, 4000);
    }
}

export function clearMessage(element) {
    clearTimeout(element._hideTimer);
    element.textContent = "";
}
