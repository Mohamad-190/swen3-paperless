import { getDocument, updateDocument, deleteDocument, getFolders, ApiError } from "./api.js";
import { validateRequired } from "./validate.js";

const params = new URLSearchParams(window.location.search);
const documentId = params.get("id");

if (!documentId) {
    showNotFound();
} else {
    await init(documentId);
}

async function init(id) {
    let document_;
    try {
        document_ = await getDocument(id);
    } catch (err) {
        if (err instanceof ApiError && err.status === 404) {
            showNotFound();
            return;
        }
        throw err;
    }

    document.getElementById("documentDetail").hidden = false;
    document.getElementById("docTitle").textContent = document_.title;
    document.getElementById("docFileName").textContent = document_.fileName;
    document.getElementById("docStatus").textContent = document_.status;
    document.getElementById("docUploadedAt").textContent =
        new Date(document_.uploadedAt).toLocaleString("de-DE");

    document.getElementById("editTitle").value = document_.title;

    await populateFolders(document_.folderId);

    document.getElementById("editForm").addEventListener("submit", (e) =>
        onEditSubmit(e, id));
    document.getElementById("deleteButton").addEventListener("click", () =>
        onDelete(id));
}

async function populateFolders(currentFolderId) {
    const select = document.getElementById("editFolder");
    const folders = await getFolders();

    for (const folder of folders) {
        const option = document.createElement("option");
        option.value = folder.id;
        option.textContent = folder.name;
        option.selected = folder.id === currentFolderId;
        select.appendChild(option);
    }
}

async function onEditSubmit(e, id) {
    e.preventDefault();
    const errorEl = document.getElementById("editError");
    errorEl.textContent = "";

    const title = document.getElementById("editTitle").value;
    const folderId = document.getElementById("editFolder").value;

    const titleError = validateRequired(title, "Titel", 200);
    if (titleError) {
        errorEl.textContent = titleError;
        return;
    }

    try {
        await updateDocument(id, {
            title: title.trim(),
            folderId: folderId || null
        });
        document.getElementById("docTitle").textContent = title.trim();
    } catch (err) {
        errorEl.textContent = err instanceof ApiError ? err.message : "Unerwarteter Fehler.";
    }
}

async function onDelete(id) {
    const errorEl = document.getElementById("deleteError");
    errorEl.textContent = "";

    try {
        await deleteDocument(id);
        window.location.href = "/";
    } catch (err) {
        errorEl.textContent = err instanceof ApiError ? err.message : "Unerwarteter Fehler.";
    }
}

function showNotFound() {
    document.getElementById("notFound").hidden = false;
}
