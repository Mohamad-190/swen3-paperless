import { getDocument, updateDocument, deleteDocument, getFolders, ApiError } from "./api.js";
import { validateRequired } from "./validate.js";
import { statusBadge, formatType, formatSize, formatDate, showMessage, clearMessage } from "./format.js";

const documentId = new URLSearchParams(window.location.search).get("id");

const el = {
    loading: document.getElementById("loading"),
    notFound: document.getElementById("notFound"),
    loadError: document.getElementById("loadError"),
    loadErrorText: document.getElementById("loadErrorText"),
    detail: document.getElementById("documentDetail"),
    title: document.getElementById("docTitle"),
    status: document.getElementById("docStatus"),
    fileName: document.getElementById("docFileName"),
    type: document.getElementById("docType"),
    size: document.getElementById("docSize"),
    folder: document.getElementById("docFolder"),
    uploadedAt: document.getElementById("docUploadedAt"),
    updatedAt: document.getElementById("docUpdatedAt"),
    summary: document.getElementById("docSummary"),
    editForm: document.getElementById("editForm"),
    editTitle: document.getElementById("editTitle"),
    editFolder: document.getElementById("editFolder"),
    saveButton: document.getElementById("saveButton"),
    editMessage: document.getElementById("editMessage"),
    deleteStart: document.getElementById("deleteStart"),
    deleteConfirm: document.getElementById("deleteConfirm"),
    deleteButton: document.getElementById("deleteButton"),
    confirmDeleteButton: document.getElementById("confirmDeleteButton"),
    cancelDeleteButton: document.getElementById("cancelDeleteButton"),
    deleteMessage: document.getElementById("deleteMessage")
};

let folders = [];

function show(section) {
    for (const s of [el.loading, el.notFound, el.loadError, el.detail]) {
        s.hidden = s !== section;
    }
}

async function load() {
    show(el.loading);

    if (!documentId) {
        show(el.notFound);
        return;
    }

    try {
        const [doc, folderList] = await Promise.all([getDocument(documentId), getFolders()]);
        folders = folderList;
        renderDocument(doc);
        renderFolderOptions(doc.folderId);
        show(el.detail);
    } catch (err) {
        if (err instanceof ApiError && (err.status === 404 || err.status === 400)) {
            show(el.notFound);
            return;
        }
        el.loadErrorText.textContent = err instanceof ApiError ? err.message : "Unerwarteter Fehler.";
        show(el.loadError);
    }
}

function renderDocument(doc) {
    document.title = `${doc.title} – DocVault`;
    el.title.textContent = doc.title;
    el.status.innerHTML = statusBadge(doc.status);
    el.fileName.textContent = doc.fileName;
    el.type.textContent = formatType(doc.contentType);
    el.size.textContent = formatSize(doc.fileSize);
    el.folder.textContent = folders.find(f => f.id === doc.folderId)?.name ?? "Kein Ordner";
    el.uploadedAt.textContent = formatDate(doc.uploadedAt);
    el.updatedAt.textContent = doc.updatedAt ? formatDate(doc.updatedAt) : "Noch nicht geändert";

    el.summary.classList.toggle("is-pending", !doc.summary);
    el.summary.textContent = doc.summary
        ?? "Noch keine Zusammenfassung. Sie wird automatisch erstellt, sobald der Text des Dokuments erkannt wurde.";

    el.editTitle.value = doc.title;
}

function renderFolderOptions(currentFolderId) {
    el.editFolder.innerHTML = '<option value="">Kein Ordner</option>';
    for (const folder of folders) {
        const option = document.createElement("option");
        option.value = folder.id;
        option.textContent = folder.name;
        option.selected = folder.id === currentFolderId;
        el.editFolder.appendChild(option);
    }
}

el.editForm.addEventListener("submit", async (e) => {
    e.preventDefault();
    el.editTitle.removeAttribute("aria-invalid");

    const title = el.editTitle.value;
    const titleError = validateRequired(title, "Titel", 200);
    if (titleError) {
        el.editTitle.setAttribute("aria-invalid", "true");
        showMessage(el.editMessage, titleError, "error");
        el.editTitle.focus();
        return;
    }

    el.saveButton.disabled = true;
    try {
        const updated = await updateDocument(documentId, {
            title: title.trim(),
            folderId: el.editFolder.value || null
        });
        renderDocument(updated);
        showMessage(el.editMessage, "Änderungen gespeichert.", "success");
    } catch (err) {
        showMessage(el.editMessage, err instanceof ApiError ? err.message : "Unerwarteter Fehler.", "error");
    } finally {
        el.saveButton.disabled = false;
    }
});

// Loeschen in zwei Schritten statt Modal: erst "Loeschen ...", dann explizit bestaetigen.
el.deleteButton.addEventListener("click", () => {
    clearMessage(el.deleteMessage);
    el.deleteStart.hidden = true;
    el.deleteConfirm.hidden = false;
    el.cancelDeleteButton.focus();
});

el.cancelDeleteButton.addEventListener("click", () => {
    el.deleteConfirm.hidden = true;
    el.deleteStart.hidden = false;
    el.deleteButton.focus();
});

el.confirmDeleteButton.addEventListener("click", async () => {
    el.confirmDeleteButton.disabled = true;
    try {
        await deleteDocument(documentId);
        window.location.href = "/";
    } catch (err) {
        showMessage(el.deleteMessage, err instanceof ApiError ? err.message : "Unerwarteter Fehler.", "error");
        el.confirmDeleteButton.disabled = false;
    }
});

document.getElementById("retryButton").addEventListener("click", load);

await load();
