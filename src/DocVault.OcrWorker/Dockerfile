FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY src/DocVault.OcrWorker/DocVault.OcrWorker.csproj src/DocVault.OcrWorker/
COPY src/DocVault.Core/DocVault.Core.csproj           src/DocVault.Core/
RUN dotnet restore src/DocVault.OcrWorker/DocVault.OcrWorker.csproj

COPY src/ src/
RUN dotnet publish src/DocVault.OcrWorker/DocVault.OcrWorker.csproj -c Release -o /app/publish --no-restore

# Worker hat keinen HTTP-Endpunkt -> schlankes Runtime-Image statt aspnet
FROM mcr.microsoft.com/dotnet/runtime:10.0

# Ghostscript rendert PDF-Seiten zu Bildern, Tesseract macht daraus Text (Deutsch + Englisch)
RUN apt-get update \
    && apt-get install -y --no-install-recommends ghostscript tesseract-ocr tesseract-ocr-deu tesseract-ocr-eng \
    && rm -rf /var/lib/apt/lists/*

WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "DocVault.OcrWorker.dll"]
