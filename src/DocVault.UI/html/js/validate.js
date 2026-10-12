// Client-seitige Validierung, gespiegelt von den DataAnnotations in den DTOs
// (CreateFolderDto, UploadDocumentRequest, CreateTagDto) - spart unnoetige Requests.

export function validateRequired(value, fieldName, maxLength) {
    const trimmed = (value ?? "").trim();
    if (!trimmed) {
        return `${fieldName} darf nicht leer sein.`;
    }
    if (maxLength && trimmed.length > maxLength) {
        return `${fieldName} darf maximal ${maxLength} Zeichen haben.`;
    }
    return null;
}

export function validateFile(file) {
    if (!file) {
        return "Bitte eine Datei auswaehlen.";
    }
    if (file.size === 0) {
        return "Die Datei ist leer.";
    }
    return null;
}
