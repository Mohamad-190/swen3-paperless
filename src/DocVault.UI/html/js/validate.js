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

// Muss mit DocumentService.AllowedContentTypes / MaxFileSizeBytes im Backend uebereinstimmen.
export const ALLOWED_CONTENT_TYPES = ["application/pdf", "image/png", "image/jpeg"];
export const MAX_FILE_SIZE_BYTES = 20 * 1024 * 1024;

export function validateFile(file) {
    if (!file) {
        return "Bitte zuerst eine Datei auswählen.";
    }
    if (file.size === 0) {
        return "Die Datei ist leer.";
    }
    if (file.size > MAX_FILE_SIZE_BYTES) {
        return `Die Datei darf maximal ${MAX_FILE_SIZE_BYTES / (1024 * 1024)} MB groß sein.`;
    }
    if (!ALLOWED_CONTENT_TYPES.includes(file.type)) {
        return "Nur PDF-, PNG- und JPEG-Dateien sind erlaubt.";
    }
    return null;
}
