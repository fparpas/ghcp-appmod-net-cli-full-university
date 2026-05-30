# Modernization Summary — 004-transform-local-io-to-blob-storage

## Objective

Migrate all local disk file upload and management operations in `Controllers/CoursesController.cs` to Azure Blob Storage using the `Azure.Storage.Blobs` SDK with Managed Identity (`DefaultAzureCredential`).

---

## Changes Made

### 1. `ContosoUniversity.csproj`
- Added `Azure.Storage.Blobs` v12.28.0 NuGet package.

### 2. `appsettings.json`
- Added `Storage` configuration section:
  ```json
  "Storage": {
    "ServiceUri": "https://<YOUR_STORAGE_ACCOUNT_NAME>.blob.core.windows.net",
    "ContainerName": "teaching-materials"
  }
  ```
- `ServiceUri` must be set to the real Azure Storage account endpoint before deployment.
- `ContainerName` defaults to `"teaching-materials"` if omitted.

### 3. `Services/IBlobStorageService.cs` *(new)*
- New interface defining two operations:
  - `UploadFileAsync(Stream, blobName, contentType?)` — uploads a file stream and returns the full blob URL.
  - `DeleteFileAsync(blobUrl)` — deletes the blob identified by its URL (no-op if not found).

### 4. `Services/AzureBlobStorageService.cs` *(new)*
- Singleton service implementing `IBlobStorageService` using `BlobServiceClient`.
- Authenticates via `DefaultAzureCredential` (Managed Identity in Azure; developer credential locally).
- Creates the container on first upload (idempotent via `CreateIfNotExistsAsync`).
- Upload uses `BlobUploadOptions` with `Conditions = null` (unconditional overwrite) to preserve the original `File.Create` / `SaveAs` semantics. **MIGRATION NOTE** comment left in code.
- Delete uses `BlobUriBuilder` to extract the blob name from the stored URL, then calls `DeleteIfExistsAsync`.
- Internal test constructor accepts a pre-built `BlobContainerClient` for unit testing.

### 5. `Program.cs`
- Registered `BlobServiceClient` as a **Singleton** with `DefaultAzureCredential` and the URI from `Storage:ServiceUri`.
- Registered `IBlobStorageService` → `AzureBlobStorageService` as a **Singleton** (required because `BlobServiceClient` is a long-lived, thread-safe connection pool).
- Throws `InvalidOperationException` at startup if `Storage:ServiceUri` is not configured.

### 6. `Controllers/CoursesController.cs`
| Before | After |
|--------|-------|
| Injected `IWebHostEnvironment _env` | Injected `IBlobStorageService _blobStorageService` |
| `Directory.CreateDirectory(...)` | Container created lazily by `AzureBlobStorageService` |
| `System.IO.File.Create(filePath)` + `CopyTo(stream)` | `await _blobStorageService.UploadFileAsync(...)` |
| `System.IO.File.Exists(...)` + `File.Delete(...)` | `await _blobStorageService.DeleteFileAsync(...)` |
| `TeachingMaterialImagePath = "/Uploads/TeachingMaterials/{fileName}"` | `TeachingMaterialImagePath = <full blob URL>` |
| `Create`, `Edit (POST)`, `DeleteConfirmed` — synchronous | All three actions are now **async `Task<IActionResult>`** |

Old technology references fully removed: `System.IO.Directory`, `System.IO.File`, `Microsoft.AspNetCore.Hosting.IWebHostEnvironment`, local path strings starting with `/Uploads/`.

---

## New Unit Tests

**`ContosoUniversity.Tests/Controllers/CoursesControllerTests.cs`** (9 tests)

| Test | Validates |
|------|-----------|
| `Create_Post_WithValidFile_UploadsToBlobAndSavesCourse` | Happy path: file is uploaded, course saved with blob URL |
| `Create_Post_WithInvalidExtension_ReturnsViewWithModelError` | Extension whitelist enforcement |
| `Create_Post_WithFileTooLarge_ReturnsViewWithModelError` | 5 MB size limit enforcement |
| `Create_Post_WithNoFile_CreatesCourseWithoutBlobPath` | No file → no blob call, course saved without path |
| `Create_Post_WhenUploadThrows_ReturnsViewWithModelError` | Upload exception → model error, course not saved |
| `Edit_Post_WithNewFile_DeletesOldBlobAndUploadsNewBlob` | Old blob deleted, new blob uploaded, path updated |
| `Edit_Post_WithNoFile_UpdatesCourseWithoutTouchingBlob` | No new file → blob service not called |
| `Edit_Post_WithInvalidExtension_ReturnsViewWithModelError` | Extension validation in Edit |
| `DeleteConfirmed_WithBlobPath_DeletesBlobAndRemovesCourse` | Blob deleted, course removed from DB |
| `DeleteConfirmed_WithNoBlobPath_RemovesCourseWithoutCallingBlobService` | No blob URL → blob service not called |
| `Create_Post_BlobUrlStoredInTeachingMaterialImagePath_NotLocalPath` | Path is a blob URL (`https://`), not a local path |

---

## Build & Test Results

- **Build**: ✅ Succeeded (0 errors, 0 warnings)
- **Tests**: ✅ 48 / 48 passed (11 pre-existing + 9 new controller tests + remaining suite)
- **Consistency check**: ✅ No Critical or Major issues

---

## Notes

- `TeachingMaterialImagePath` stores full blob URLs (e.g. `https://account.blob.core.windows.net/teaching-materials/course_1_abc.jpg`). Any Views that previously referenced this path as a relative URL for `<img src="...">` should now work correctly as absolute URLs.
- The managed identity used at runtime must have the **Storage Blob Data Contributor** RBAC role on the storage account or container.
