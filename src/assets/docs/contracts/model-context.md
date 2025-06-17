# `ModelContext` Contract

`ModelContext` is a core class in MaIN for managing AI models. This class provides comprehensive functionality for discovering, downloading, caching, and managing LLM models within the system. It handles model lifecycle operations including retrieval from remote sources, local storage management, and memory caching for optimal performance.

This document outlines each method available in `ModelContext` with its purpose and usage.

---

### **Methods:**

## **GetAll()**

**Purpose**:  
Retrieves a complete list of all available models in the system. This method returns all known models that can be used within the MaIN framework.

**Usage**:

```csharp
List<Model> allModels = modelContext.GetAll();
```

**Returns**:  
- A `List<Model>` containing all available models in the system.

---

## **GetModel(string model)**

**Purpose**:  
Retrieves information about a specific model by its name. This method allows you to get detailed information about a particular model, including its configuration and metadata.

**Usage**:

```csharp
Model specificModel = modelContext.GetModel("llama3.2:3b");
```

**Parameters**:  
- `model`: The name of the model to retrieve.

**Returns**:  
- A `Model` object containing the model's information and configuration.

---

## **GetEmbeddingModel()**

**Purpose**:  
Retrieves the designated embedding model used for generating vector representations of text. This is typically used for semantic search, similarity calculations, and other NLP tasks that require text embeddings.

**Usage**:

```csharp
Model embeddingModel = modelContext.GetEmbeddingModel();
```

**Returns**:  
- A `Model` object representing the embedding model.

---

## **Exists(string modelName)**

**Purpose**:  
Checks whether a specific model exists locally on the filesystem. This method verifies if the model file is present and accessible before attempting to use it.

**Usage**:

```csharp
bool modelExists = modelContext.Exists("llama3.2:3b");
```

**Parameters**:  
- `modelName`: The name of the model to check for existence.

**Returns**:  
- A boolean value indicating whether the model file exists locally.

**Throws**:  
- `ArgumentException` if the model name is null or empty.

---

## **DownloadAsync(string modelName, CancellationToken cancellationToken = default)**

**Purpose**:  
Asynchronously downloads a known model from its configured download URL. This method handles the complete download process with progress tracking and error handling.

**Usage**:

```csharp
await modelContext.DownloadAsync("llama3.2:3b");
```

**Parameters**:  
- `modelName`: The name of the model to download.
- `cancellationToken`: Optional cancellation token to abort the download operation.

**Returns**:  
- A `Task<ModelContext>` that completes when the download finishes, returning the same ModelContext instance for method chaining.

**Throws**:  
- `ArgumentException` if the model name is null or empty.

---

## **DownloadAsync(string model, string url)**

**Purpose**:  
Asynchronously downloads a custom model from a specified URL. This method allows downloading models that are not part of the known models collection, adding them to the system after download.

**Usage**:

```csharp
await modelContext.DownloadAsync("custom-model", "https://example.com/model.gguf");
```

**Parameters**:  
- `model`: The name to assign to the downloaded model.
- `url`: The URL from which to download the model.

**Returns**:  
- A `Task<ModelContext>` that completes when the download finishes, returning the same ModelContext instance for method chaining.

**Throws**:  
- `ArgumentException` if the model name or URL is null or empty.

---

## **Download(string modelName)**

**Purpose**:  
Synchronously downloads a known model from its configured download URL. This is the blocking version of the download operation with progress tracking.

**Usage**:

```csharp
modelContext.Download("llama3.2:3b");
```

**Parameters**:  
- `modelName`: The name of the model to download.

**Returns**:  
- The `ModelContext` instance for method chaining.

**Throws**:  
- `ArgumentException` if the model name is null or empty.

---

## **Download(string model, string url)**

**Purpose**:  
Synchronously downloads a custom model from a specified URL. This method provides blocking download functionality for custom models not in the known models collection.

**Usage**:

```csharp
modelContext.Download("custom-model", "https://example.com/model.gguf");
```

**Parameters**:  
- `model`: The name to assign to the downloaded model.
- `url`: The URL from which to download the model.

**Returns**:  
- The `ModelContext` instance for method chaining.

**Throws**:  
- `ArgumentException` if the model name or URL is null or empty.

---

## **LoadToCache(Model model)**

**Purpose**:  
Loads a model into memory cache for faster access during inference operations. This method preloads the model to avoid loading delays when the model is first used in chat sessions.

**Usage**:

```csharp
Model model = modelContext.GetModel("llama3.2:3b");
modelContext.LoadToCache(model);
```

**Parameters**:  
- `model`: The `Model` object to load into cache.

**Returns**:  
- The `ModelContext` instance for method chaining.

**Throws**:  
- `ArgumentNullException` if the model parameter is null.

---

## **LoadToCacheAsync(Model model)**

**Purpose**:  
Asynchronously loads a model into memory cache for faster access during inference operations. This is the non-blocking version of cache loading that allows other operations to continue while the model loads.

**Usage**:

```csharp
Model model = modelContext.GetModel("llama3.2:3b");
await modelContext.LoadToCacheAsync(model);
```

**Parameters**:  
- `model`: The `Model` object to load into cache.

**Returns**:  
- A `Task<ModelContext>` that completes when the model is loaded into cache, returning the same ModelContext instance for method chaining.

**Throws**:  
- `ArgumentNullException` if the model parameter is null.

---

### **Key Features:**

## **Download Progress Tracking**

The `ModelContext` provides comprehensive download progress tracking with the following features:

- **Real-time Progress Updates**: Shows download percentage, current/total bytes, and download speed
- **ETA Calculation**: Provides estimated time to completion based on current download speed
- **Speed Monitoring**: Displays current download speed in human-readable format (KB/s, MB/s, GB/s)
- **Error Recovery**: Automatically cleans up partial downloads on failure

## **File Size Formatting**

All byte values are automatically formatted into human-readable units (Bytes, KB, MB, GB) for better user experience during downloads and progress reporting.

## **HTTP Configuration**

- **Timeout Handling**: Default 30-minute timeout for large model downloads
- **Buffer Optimization**: Configurable buffer sizes for optimal download performance
- **User-Agent Headers**: Proper HTTP headers for compatibility with download servers

## **Path Resolution**

The class automatically resolves model storage paths from:
1. Application settings (`ModelsPath`)
2. Environment variables (`MaIN_ModelsPath`)
3. Throws exception if no valid path is found

---

### **Summary:**

`ModelContext` provides a robust and comprehensive interface for managing AI models in the MaIN framework. It handles the complete model lifecycle from discovery and download to caching and storage management. The class offers both synchronous and asynchronous operations, comprehensive error handling, and detailed progress tracking for download operations.

Key capabilities include:
- **Model Discovery**: Access to all known models and their metadata
- **Download Management**: Robust downloading with progress tracking and error recovery
- **Cache Management**: Memory caching for improved inference performance
- **Custom Model Support**: Ability to download and integrate custom models
- **Path Management**: Flexible model storage path resolution

This class is essential for applications that need to dynamically manage AI models, whether downloading from remote sources or managing local model storage and caching.