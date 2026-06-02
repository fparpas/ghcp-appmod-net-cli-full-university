# Modernization Summary: Task 004 — Migrate Local Storage to Azure Blob Storage

## Overview

Migrated teaching-material image uploads in `CoursesController` from local file-system I/O
to **Azure Storage Blob SDK** (`Azure.Storage.Blobs` 12.28.0) authenticated via
**Managed Identity** (`DefaultAzureCredential`). No connection strings or access keys are used.

---

## Files Changed

### New Files

| File | Purpose |
|------|---------|
| `Services/IBlobStorageService.cs` | New interface exposing `UploadAsync` and `DeleteAsync` — decouples the controller from any specific storage backend and enables unit testing. |
| `Services/AzureBlobStorageService.cs` | Singleton implementation using `BlobServiceClient` (injected) authenticated with `DefaultAzureCredential`. Container is created with `PublicAccessType.Blob` on first upload to preserve the same public-read semantics as the previous static-file serving. |
| `Tests/Services/BlobStorageServiceTests.cs` | 9 new unit tests covering: `CreateIfNotExistsAsync` is called on upload, correct blob name is passed to `GetBlobClient`, correct `ContentType` is set in `BlobHttpHeaders`, blob URI is returned from `UploadAsync`, stream is not pre-consumed before passing to SDK, `DeleteIfExistsAsync` is called for valid Azure URLs, and graceful no-ops for null / empty / legacy local paths. |
| `Tests/Controllers/CoursesControllerTests.cs` | 11 new unit tests covering: Create without image (no blob call), Create with JPEG/PNG (correct content type), invalid extension → model error, oversized file → model error, Edit without new file (no blob interaction), Edit with new file (deletes old blob, uploads new), Edit with invalid extension (no blob call), DeleteConfirmed with image (blob deleted), DeleteConfirmed without image (no-op), legacy local path on delete (graceful), contract assertion that IBlobStorageService is used (not local I/O). |

### Modified Files

| File | Change |
|------|--------|
| `Controllers/CoursesController.cs` | Replaced `IWebHostEnvironment` dependency with `IBlobStorageService`. Removed all `FileStream`, `Directory.CreateDirectory`, `File.Exists`, `File.Delete`, and `Server.MapPath`-equivalent operations. Upload uses `IFormFile.OpenReadStream()` → `IBlobStorageService.UploadAsync`; deletion uses `IBlobStorageService.DeleteAsync`. Stored path is now a full Azure Blob URL. Added `GetContentType()` helper to set correct MIME type. |
| `Program.cs` | Added `using Azure.Identity` and `using Azure.Storage.Blobs`. Registered `BlobServiceClient` as **Singleton** (`new Uri(serviceUri), new DefaultAzureCredential()`). Registered `AzureBlobStorageService` as **Singleton**. Removed the `Uploads/` static-file middleware (images are now served from Azure Blob Storage). |
| `appsettings.json` | Added `"Storage": { "ServiceUri": "https://YOUR_STORAGE_ACCOUNT_NAME.blob.core.windows.net", "ContainerName": "teaching-materials" }` — storage account name is read from configuration, never hardcoded. |
| `ContosoUniversity.csproj` | Added `Azure.Storage.Blobs` 12.28.0 package reference. Removed `Uploads\**\*` from `CopyToPublishDirectory` content items. |
| `Tests/ContosoUniversity.Tests.csproj` | Added `Azure.Storage.Blobs` 12.28.0 package reference so Azure SDK clients can be properly mocked in tests. |

---

## Key Design Decisions

### Authentication
`DefaultAzureCredential` is used throughout — zero secrets, zero connection strings. In
production the app-service/container-app's system-assigned Managed Identity must be granted
**Storage Blob Data Contributor** on the storage account (or the specific container).

### Container Access
The `teaching-materials` container is created with `PublicAccessType.Blob`. This preserves
the same publicly-accessible image semantics as the original `app.UseStaticFiles()` serving
of the `Uploads/` folder. Images embedded in `<img>` tags in the Razor views are rendered
directly from the blob URL stored in `TeachingMaterialImagePath`.

> **Note:** Public blob access requires the storage account's *Allow Blob Anonymous Access*
> setting to be enabled (the default for new accounts). If this is disabled at the account
> level, a SAS-based approach or an application-side proxy will be needed to serve images.

### Overwrite Semantics
`BlobUploadOptions` with `Conditions == null` is used for unconditional overwrite, matching
the original `FileMode.Create` semantics. A `// MIGRATION NOTE:` comment documents this at
each call site.

### Legacy Path Handling
`DeleteAsync` catches `UriFormatException` to gracefully skip deletion when the stored path
is a legacy local path (`~/Uploads/...`) that is not a valid absolute URI. This prevents
runtime errors for existing database records that have not been data-migrated.

### DI Lifetime
Both `BlobServiceClient` and `AzureBlobStorageService` are registered as **Singleton**,
following Azure SDK guidance that clients hold an HTTP pipeline and token cache that must be
reused across requests to avoid AAD throttling and connection-pool exhaustion.

---

## Removed Local I/O Operations

| Original Code | Replacement |
|--------------|-------------|
| `Path.Combine(_webHostEnvironment.ContentRootPath, "Uploads", "TeachingMaterials")` | Azure Blob container resolved from `IConfiguration["Storage:ContainerName"]` |
| `Directory.CreateDirectory(uploadsPath)` | `_containerClient.CreateIfNotExistsAsync(PublicAccessType.Blob)` |
| `new FileStream(filePath, FileMode.Create)` + `teachingMaterialImage.CopyTo(stream)` | `teachingMaterialImage.OpenReadStream()` → `_blobStorageService.UploadAsync(...)` |
| `System.IO.File.Exists(filePath)` + `System.IO.File.Delete(filePath)` | `_blobStorageService.DeleteAsync(blobUrl)` (idempotent, handles missing blobs) |
| `"~/Uploads/TeachingMaterials/" + fileName` | Full Azure Blob URL returned by `UploadAsync` |
| `app.UseStaticFiles(...)` for `/Uploads` | Removed — images served directly from Azure Blob Storage |

---

## Test Results

```
Total: 53  |  Passed: 53  |  Failed: 0  |  Skipped: 0
Build: Succeeded — 0 errors, 0 warnings
```

- **20 new tests** added (9 for `AzureBlobStorageService`, 11 for `CoursesController`)
- All 33 pre-existing tests continue to pass
