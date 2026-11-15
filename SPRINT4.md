# Sprint 4 – Stand & offene Punkte

Branch: `feature/ocr-elasticsearch`

Ziel laut Abnahme: `HelloWorld.pdf` über http://localhost/ hochladen → Suche nach „Hello“ im Frontend zeigt das Dokument.

## Erledigt (1–5 + 9)

| # | Aufgabe | Wo |
|---|---|---|
| 1 | Elasticsearch in `docker-compose.yml` (`elasticsearch:8.19.25`, single-node, ohne Security, Port 9200, Volume `esdata`) | `docker-compose.yml` |
| 2 | `DocumentUploadedMessage` um `Title` und `ObjectKey` erweitert, damit der Worker die Datei im Storage findet | `Core/ServiceAgents/DocumentUploadedMessage.cs`, `DocumentService.cs` |
| 3 | Worker-Image installiert Ghostscript + Tesseract (deu + eng) | `OcrWorker/Dockerfile` |
| 4 | OCR im Worker: Datei aus RustFS holen → PDF mit Ghostscript zu PNGs (300 DPI) → Tesseract pro Seite. PNG/JPEG direkt an Tesseract. Externe Prozesse laufen über `ICommandRunner` (mockbar), Fehler als `OcrException` | `OcrWorker/Ocr/*`, `OcrMessageHandler.cs` |
| 5 | Indexierung: `ISearchIndexer` + `ElasticsearchIndexer` (Client `Elastic.Clients.Elasticsearch` 8.19.25). Index `documents`, Felder `documentId, title, fileName, content, uploadedAt`, Doc-ID = Document-ID. Fehler als `SearchException` | `Core/ServiceAgents/ISearchIndexer.cs`, `Core/Infrastructure/Search/*` |
| 9 | Worker in der Compose: S3- und Elasticsearch-Variablen, wartet auf `rustfs-init` und `elasticsearch` | `docker-compose.yml` |

Tests: 53/53 grün, neu sind Handler-Tests (Erfolg + alle Fehlerfälle) und Tests für die OCR-Engine mit Fake-Runner.

**Achtung:** Ein End-to-End-Lauf mit `docker compose up` ist noch **nicht** gemacht, bitte als Erstes prüfen:

```bash
docker compose up --build
# HelloWorld.pdf über http://localhost/ hochladen, dann:
docker compose logs ocr-worker        # sollte "OCR ... finished, ... characters recognized: Hello World" zeigen
curl "http://localhost:9200/documents/_search?q=Hello"
```

In der `.env` müssen auch `RABBITMQ_USER`, `RABBITMQ_PASSWORD`, `RUSTFS_ACCESS_KEY` und `RUSTFS_SECRET_KEY` gesetzt sein (siehe `.env.example`).

## Offen (6–8)

### 6. Status in der DB setzen
`DocumentStatus` (`Processing` / `Completed` / `Failed`) wird aktuell nirgends gesetzt, nach dem Upload bleibt alles auf `Uploaded`.

Zwei Möglichkeiten:
- **Einfach:** Der Worker bekommt `DocVaultDbContext` + `IDocumentRepository` und setzt den Status selbst (vor OCR `Processing`, danach `Completed`, im catch `Failed`).
- **Sauberer (empfohlen):** Der Worker schickt eine Result-Message (z. B. `ocr-result-queue` mit `DocumentId`, `Status`, optional OCR-Text) zurück, und die REST-API konsumiert sie und aktualisiert die DB. Dieses Muster brauchen wir in Sprint 5 sowieso für die GenAI-Summary.

### 7. Such-Endpoint in der REST-API
- `GET /documents/search?q=...` im `DocumentController`
- `ISearchIndexer` um `SearchAsync(string query)` erweitern (oder ein eigenes `IDocumentSearch`), in `ElasticsearchIndexer` implementieren:
  `multi_match` auf `content`, `title`, `fileName` mit `fuzziness: "AUTO"` → deckt auch die Fuzzy-Suche aus Use Case 2 ab.
- Treffer-IDs mit der DB abgleichen und `DocumentDto`s zurückgeben (gelöschte Dokumente rausfiltern).
- WebApi: `ElasticsearchOptions` + `ISearchIndexer` in `Program.cs` registrieren, `Elasticsearch__Url: http://elasticsearch:9200` in der Compose beim `webapi` ergänzen, `depends_on: elasticsearch`.
- `SearchException` im `GlobalExceptionHandler` auf 503 mappen.
- Leeren Suchbegriff validieren (400).
- Unit-Tests für Service + Controller.
- Nice to have: Beim Löschen eines Dokuments auch den ES-Eintrag (und die Datei in RustFS) löschen.

### 8. Suche im UI
- `api.js`: `searchDocuments(q)` → `GET /api/documents/search?q=...`
- `dashboard.js`: Das Suchfeld filtert aktuell nur lokal nach Titel/Dateiname. Stattdessen den Endpoint aufrufen (mit Debounce, ~300 ms). Bei leerem Feld wieder alle Dokumente anzeigen.
- Placeholder anpassen („Titel oder Dateiname suchen“ → z. B. „Dokumente durchsuchen“).

## Sonst noch
- `HelloWorld.pdf` als Beispiel ins Repo legen (z. B. `samples/`), damit wir die Abnahme vorführen können.
- `tests/DocVault.Tests/TestResults/` ist eingecheckt → in `.gitignore` aufnehmen und aus Git entfernen.
- Laut Angabe ist .NET ≥ 10 gefordert, wir sind auf `net8.0` → im Team klären.
- Angabe nennt MinIO, wir nutzen RustFS (S3-kompatibel) → Begründung für die Abgabe parat haben.
