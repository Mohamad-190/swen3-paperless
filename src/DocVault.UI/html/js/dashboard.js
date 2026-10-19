import {
    getDocuments,
    getFolders,
    createFolder,
    uploadDocument,
    ApiError
} from "./api.js";
import { validateRequired, validateFile } from "./validate.js";
import {
    escapeHtml,
    statusBadge,
    formatType,
    formatSize,
    formatDate,
    showMessage,
    clearMessage
} from "./format.js";

const state = {
    documents: [],
    folders: [],
    activeFolderId: null,   // null = alle Dokumente
    query: "",
    loading: true,
    loadError: null
};

const el = {
    documentCount: document.getElementById("documentCount"),
    documentsHeading: document.getElementById("documentsHeading"),
    documentList: document.getElementById("documentList"),
    documentEmpty: document.getElementById("documentEmpty"),
    folderList: document.getElementById("folderList"),
    searchInput: document.getElementById("searchInput"),
    uploadForm: document.getElementById("uploadForm"),
    uploadFile: document.getElementById("uploadFile"),
    uploadTitle: document.getElementById("uploadTitle"),
    uploadFolder: document.getElementById("uploadFolder"),
    uploadButton: document.getElementById("uploadButton"),
    uploadMessage: document.getElementById("uploadMessage"),
    dropzone: document.getElementById("dropzone"),
    dropzoneText: document.getElementById("dropzoneText"),
    dropzoneHint: document.getElementById("dropzoneHint"),
    folderForm: document.getElementById("folderForm"),
    folderName: document.getElementById("folderName"),
    folderDescription: document.getElementById("folderDescription"),
    folderButton: document.getElementById("folderButton"),
    folderMessage: document.getElementById("folderMessage")
};

const DROPZONE_DEFAULT_TEXT = el.dropzoneText.textContent;
const DROPZONE_DEFAULT_HINT = el.dropzoneHint.textContent;

// ---------- Laden ----------

async function loadAll() {
    state.loading = true;
    state.loadError = null;
    render();

    try {
        const [documents, folders] = await Promise.all([getDocuments(), getFolders()]);
        state.documents = documents;
        state.folders = folders;
    } catch (err) {
        state.loadError = err instanceof ApiError ? err.message : "Unerwarteter Fehler.";
    } finally {
        state.loading = false;
        render();
    }
}

// ---------- Rendern ----------

function render() {
    renderFolders();
    renderUploadFolderOptions();
    renderDocuments();
    renderCount();
}

function renderCount() {
    if (state.loading || state.loadError) {
        el.documentCount.textContent = "";
        return;
    }
    const n = state.documents.length;
    el.documentCount.textContent = n === 1 ? "1 Dokument" : `${n} Dokumente`;
}

function renderFolders() {
    const items = [
        { id: null, name: "Alle Dokumente", count: state.documents.length },
        ...state.folders.map(f => ({ id: f.id, name: f.name, count: f.documentCount }))
    ];

    el.folderList.innerHTML = items.map(item => `
        <li>
            <button type="button" class="folder-button" data-folder-id="${item.id ?? ""}"
                    aria-pressed="${item.id === state.activeFolderId}">
                <svg class="icon" viewBox="0 0 24 24" aria-hidden="true">
                    <path d="M3 7a2 2 0 0 1 2-2h4l2 2h8a2 2 0 0 1 2 2v8a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2Z"/>
                </svg>
                <span class="folder-name">${escapeHtml(item.name)}</span>
                <span class="folder-count">${state.loading ? "" : item.count}</span>
            </button>
        </li>
    `).join("");
}

function renderUploadFolderOptions() {
    const selected = el.uploadFolder.value || state.activeFolderId || "";
    el.uploadFolder.innerHTML = '<option value="">Kein Ordner</option>' +
        state.folders.map(f => `<option value="${f.id}">${escapeHtml(f.name)}</option>`).join("");
    el.uploadFolder.value = state.folders.some(f => f.id === selected) ? selected : "";
}

function renderDocuments() {
    const activeFolder = state.folders.find(f => f.id === state.activeFolderId);
    el.documentsHeading.textContent = activeFolder ? activeFolder.name : "Alle Dokumente";

    if (state.loading) {
        el.documentEmpty.hidden = true;
        el.documentList.innerHTML = Array.from({ length: 3 }, () => `
            <tr aria-hidden="true">
                <td><span class="skeleton" style="width: 60%"></span></td>
                <td><span class="skeleton" style="width: 70%"></span></td>
                <td><span class="skeleton" style="width: 40px"></span></td>
                <td><span class="skeleton" style="width: 48px; margin-left: auto"></span></td>
                <td><span class="skeleton" style="width: 80px"></span></td>
                <td><span class="skeleton" style="width: 110px"></span></td>
            </tr>
        `).join("");
        return;
    }

    if (state.loadError) {
        el.documentList.innerHTML = "";
        showEmpty(
            "Dokumente konnten nicht geladen werden.",
            state.loadError,
            '<button type="button" class="btn btn-quiet" id="retryButton">Erneut versuchen</button>'
        );
        document.getElementById("retryButton").addEventListener("click", loadAll);
        return;
    }

    const visible = filterDocuments();
    el.documentList.innerHTML = visible.map(documentRow).join("");

    if (visible.length > 0) {
        el.documentEmpty.hidden = true;
    } else if (state.query) {
        showEmpty(`Keine Treffer für „${escapeHtml(state.query)}“.`, "Suche nach einem anderen Titel oder Dateinamen.");
    } else if (activeFolder) {
        showEmpty("Dieser Ordner ist noch leer.", "Wähle beim Hochladen diesen Ordner aus.");
    } else {
        showEmpty("Noch keine Dokumente.", "Lade oben dein erstes Dokument hoch.");
    }
}

function filterDocuments() {
    const q = state.query.toLowerCase();
    return state.documents.filter(d =>
        (state.activeFolderId === null || d.folderId === state.activeFolderId) &&
        (!q || d.title.toLowerCase().includes(q) || d.fileName.toLowerCase().includes(q)));
}

function documentRow(doc) {
    const href = `document.html?id=${doc.id}`;
    const folder = state.folders.find(f => f.id === doc.folderId);
    return `
        <tr data-href="${href}">
            <td>
                <a class="doc-title" href="${href}">${escapeHtml(doc.title)}</a>
                <span class="doc-file">${escapeHtml(doc.fileName)}</span>
            </td>
            <td class="nowrap">${folder ? escapeHtml(folder.name) : '<span class="muted">Kein Ordner</span>'}</td>
            <td>${escapeHtml(formatType(doc.contentType))}</td>
            <td class="num nowrap">${formatSize(doc.fileSize)}</td>
            <td>${statusBadge(doc.status)}</td>
            <td class="nowrap">${formatDate(doc.uploadedAt)}</td>
        </tr>
    `;
}

function showEmpty(title, text, actionHtml = "") {
    el.documentEmpty.innerHTML = `<strong>${title}</strong>${escapeHtml(text)}${actionHtml ? `<div>${actionHtml}</div>` : ""}`;
    el.documentEmpty.hidden = false;
}

// ---------- Interaktion: Ordner & Suche ----------

el.folderList.addEventListener("click", (e) => {
    const button = e.target.closest(".folder-button");
    if (!button) return;
    state.activeFolderId = button.dataset.folderId || null;
    el.uploadFolder.value = state.activeFolderId ?? "";
    render();
});

el.searchInput.addEventListener("input", (e) => {
    state.query = e.target.value.trim();
    renderDocuments();
});

// Ganze Zeile klickbar, der Titel bleibt ein echter Link (Tastatur, Mittelklick).
el.documentList.addEventListener("click", (e) => {
    if (e.target.closest("a")) return;
    const row = e.target.closest("tr[data-href]");
    if (row) window.location.href = row.dataset.href;
});

// ---------- Upload ----------

function updateDropzone() {
    const file = el.uploadFile.files[0];
    el.dropzone.classList.toggle("has-file", Boolean(file));
    el.dropzoneText.textContent = file ? file.name : DROPZONE_DEFAULT_TEXT;
    el.dropzoneHint.textContent = file
        ? `${formatType(file.type) || "Unbekannter Typ"}, ${formatSize(file.size)}. Klicken, um eine andere Datei zu wählen.`
        : DROPZONE_DEFAULT_HINT;

    // Titel aus dem Dateinamen vorschlagen, solange noch keiner eingetragen ist
    if (file && !el.uploadTitle.value.trim()) {
        el.uploadTitle.value = file.name.replace(/\.[^.]+$/, "");
    }
}

el.uploadFile.addEventListener("change", () => {
    clearMessage(el.uploadMessage);
    updateDropzone();
});

["dragenter", "dragover"].forEach(type => el.dropzone.addEventListener(type, (e) => {
    e.preventDefault();
    el.dropzone.classList.add("is-dragover");
}));

["dragleave", "drop"].forEach(type => el.dropzone.addEventListener(type, () => {
    el.dropzone.classList.remove("is-dragover");
}));

el.dropzone.addEventListener("drop", (e) => {
    e.preventDefault();
    const file = e.dataTransfer.files[0];
    if (!file) return;
    const transfer = new DataTransfer();
    transfer.items.add(file);
    el.uploadFile.files = transfer.files;
    clearMessage(el.uploadMessage);
    updateDropzone();
});

el.uploadForm.addEventListener("submit", async (e) => {
    e.preventDefault();
    el.uploadTitle.removeAttribute("aria-invalid");

    const file = el.uploadFile.files[0];
    const title = el.uploadTitle.value;
    const folderId = el.uploadFolder.value;

    const fileError = validateFile(file);
    if (fileError) {
        showMessage(el.uploadMessage, fileError, "error");
        return;
    }
    const titleError = validateRequired(title, "Titel", 200);
    if (titleError) {
        el.uploadTitle.setAttribute("aria-invalid", "true");
        showMessage(el.uploadMessage, titleError, "error");
        el.uploadTitle.focus();
        return;
    }

    const formData = new FormData();
    formData.append("File", file);
    formData.append("Title", title.trim());
    if (folderId) {
        formData.append("FolderId", folderId);
    }

    el.uploadButton.disabled = true;
    el.uploadButton.textContent = "Wird hochgeladen …";
    try {
        await uploadDocument(formData);
        el.uploadForm.reset();
        updateDropzone();
        showMessage(el.uploadMessage, `„${title.trim()}“ wurde hochgeladen.`, "success");
        await loadAll();
    } catch (err) {
        showMessage(el.uploadMessage, err instanceof ApiError ? err.message : "Unerwarteter Fehler.", "error");
    } finally {
        el.uploadButton.disabled = false;
        el.uploadButton.textContent = "Hochladen";
    }
});

// ---------- Neuer Ordner ----------

el.folderForm.addEventListener("submit", async (e) => {
    e.preventDefault();
    el.folderName.removeAttribute("aria-invalid");

    const name = el.folderName.value;
    const description = el.folderDescription.value;

    const nameError = validateRequired(name, "Name", 100);
    if (nameError) {
        el.folderName.setAttribute("aria-invalid", "true");
        showMessage(el.folderMessage, nameError, "error");
        el.folderName.focus();
        return;
    }

    el.folderButton.disabled = true;
    try {
        await createFolder({ name: name.trim(), description: description.trim() || null });
        el.folderForm.reset();
        showMessage(el.folderMessage, `Ordner „${name.trim()}“ angelegt.`, "success");
        await loadAll();
    } catch (err) {
        showMessage(el.folderMessage, err instanceof ApiError ? err.message : "Unerwarteter Fehler.", "error");
    } finally {
        el.folderButton.disabled = false;
    }
});

await loadAll();
