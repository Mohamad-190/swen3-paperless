import {
    getDocuments,
    getFolders,
    createFolder,
    uploadDocument,
    ApiError
} from "./api.js";
import { validateRequired, validateFile } from "./validate.js";

let allDocuments = [];

async function loadFolders() {
    const folderList = document.getElementById("folderList");
    const folderEmpty = document.getElementById("folderEmpty");
    const uploadFolder = document.getElementById("uploadFolder");

    const folders = await getFolders();

    folderList.innerHTML = "";
    uploadFolder.innerHTML = '<option value="">Kein Ordner</option>';

    folderEmpty.hidden = folders.length > 0;

    for (const folder of folders) {
        const row = document.createElement("tr");
        row.innerHTML = `
            <td>${escapeHtml(folder.name)}</td>
            <td>${escapeHtml(folder.description ?? "")}</td>
            <td>${folder.documentCount}</td>
        `;
        folderList.appendChild(row);

        const option = document.createElement("option");
        option.value = folder.id;
        option.textContent = folder.name;
        uploadFolder.appendChild(option);
    }
}

async function loadDocuments() {
    allDocuments = await getDocuments();
    renderDocuments(allDocuments);
}

function renderDocuments(documents) {
    const documentList = document.getElementById("documentList");
    const documentEmpty = document.getElementById("documentEmpty");

    documentList.innerHTML = "";
    documentEmpty.hidden = documents.length > 0;

    for (const doc of documents) {
        const row = document.createElement("tr");
        row.dataset.href = `document.html?id=${doc.id}`;
        row.innerHTML = `
            <td>${escapeHtml(doc.title)}</td>
            <td>${escapeHtml(doc.fileName)}</td>
            <td>${escapeHtml(doc.status)}</td>
            <td>${new Date(doc.uploadedAt).toLocaleString("de-DE")}</td>
        `;
        row.addEventListener("click", () => {
            window.location.href = row.dataset.href;
        });
        documentList.appendChild(row);
    }
}

function escapeHtml(value) {
    const div = document.createElement("div");
    div.textContent = value;
    return div.innerHTML;
}

document.getElementById("folderForm").addEventListener("submit", async (e) => {
    e.preventDefault();
    const errorEl = document.getElementById("folderError");
    errorEl.textContent = "";

    const name = document.getElementById("folderName").value;
    const description = document.getElementById("folderDescription").value;

    const nameError = validateRequired(name, "Name", 100);
    if (nameError) {
        errorEl.textContent = nameError;
        return;
    }

    try {
        await createFolder({ name: name.trim(), description: description.trim() || null });
        document.getElementById("folderForm").reset();
        await loadFolders();
    } catch (err) {
        errorEl.textContent = err instanceof ApiError ? err.message : "Unerwarteter Fehler.";
    }
});

document.getElementById("uploadForm").addEventListener("submit", async (e) => {
    e.preventDefault();
    const errorEl = document.getElementById("uploadError");
    errorEl.textContent = "";

    const file = document.getElementById("uploadFile").files[0];
    const title = document.getElementById("uploadTitle").value;
    const folderId = document.getElementById("uploadFolder").value;

    const fileError = validateFile(file);
    if (fileError) {
        errorEl.textContent = fileError;
        return;
    }
    const titleError = validateRequired(title, "Titel", 200);
    if (titleError) {
        errorEl.textContent = titleError;
        return;
    }

    const formData = new FormData();
    formData.append("File", file);
    formData.append("Title", title.trim());
    if (folderId) {
        formData.append("FolderId", folderId);
    }

    try {
        await uploadDocument(formData);
        document.getElementById("uploadForm").reset();
        await Promise.all([loadDocuments(), loadFolders()]);
    } catch (err) {
        errorEl.textContent = err instanceof ApiError ? err.message : "Unerwarteter Fehler.";
    }
});

document.getElementById("searchInput").addEventListener("input", (e) => {
    const query = e.target.value.trim().toLowerCase();
    const filtered = query
        ? allDocuments.filter(d => d.title.toLowerCase().includes(query))
        : allDocuments;
    renderDocuments(filtered);
});

await Promise.all([loadFolders(), loadDocuments()]);
