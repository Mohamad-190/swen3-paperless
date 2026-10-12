// API-Client fuer DocVault. Alle Requests gehen relativ an /api/,
// nginx leitet das per reverse proxy an den webapi-Container weiter (siehe nginx.conf).
const API_BASE = "/api";

class ApiError extends Error {
    constructor(status, message) {
        super(message);
        this.status = status;
    }
}

async function request(path, options = {}) {
    let response;
    try {
        response = await fetch(`${API_BASE}${path}`, options);
    } catch {
        throw new ApiError(0, "Server nicht erreichbar. Bitte spaeter erneut versuchen.");
    }

    if (!response.ok) {
        const message = await extractErrorMessage(response);
        throw new ApiError(response.status, message);
    }

    if (response.status === 204) {
        return null;
    }

    return response.json();
}

async function extractErrorMessage(response) {
    try {
        const body = await response.json();
        return body.detail || body.title || body.message || `Fehler ${response.status}`;
    } catch {
        return `Fehler ${response.status}`;
    }
}

// Documents
export function getDocuments() {
    return request("/documents");
}

export function getDocument(id) {
    return request(`/documents/${id}`);
}

export function uploadDocument(formData) {
    return request("/documents", { method: "POST", body: formData });
}

export function updateDocument(id, dto) {
    return request(`/documents/${id}`, {
        method: "PUT",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(dto)
    });
}

export function deleteDocument(id) {
    return request(`/documents/${id}`, { method: "DELETE" });
}

// Folders
export function getFolders() {
    return request("/folders");
}

export function getFolder(id) {
    return request(`/folders/${id}`);
}

export function createFolder(dto) {
    return request("/folders", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(dto)
    });
}

export function updateFolder(id, dto) {
    return request(`/folders/${id}`, {
        method: "PUT",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(dto)
    });
}

export function deleteFolder(id) {
    return request(`/folders/${id}`, { method: "DELETE" });
}

export function getFolderDocuments(id) {
    return request(`/folders/${id}/documents`);
}

export function moveDocumentToFolder(folderId, documentId) {
    return request(`/folders/${folderId}/documents/${documentId}`, { method: "PUT" });
}

// Tags
export function getTags() {
    return request("/tags");
}

export function createTag(dto) {
    return request("/tags", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(dto)
    });
}

export function deleteTag(id) {
    return request(`/tags/${id}`, { method: "DELETE" });
}

export function assignTagToDocument(documentId, tagId) {
    return request(`/documents/${documentId}/tags/${tagId}`, { method: "PUT" });
}

export function removeTagFromDocument(documentId, tagId) {
    return request(`/documents/${documentId}/tags/${tagId}`, { method: "DELETE" });
}

export { ApiError };
