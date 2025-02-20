
# 🔧 MaIN Command Line Interface (CLI)

The MaIN CLI (`mcli`) is your companion tool for managing AI workflows and services. Designed for simplicity with zero configuration requirements.

## Installation

### (Windows)
Download and unpack content from this link https://1drv.ms/u/s!AnWkWN8pJdeNbV7YSEGZ-NzZB5A?e=C0lSo2
```bash
.\install-mcli.ps1
```

```bash
# Download and run installer (Linux/Mac)

```

> **Requires Administrator privileges**  
> The installer will:
> - Create `%LOCALAPPDATA%\MaIN\CLI` directory
> - Add `mcli` to system PATH
> - Set execution policies
> - Create uninstaller

## Command Reference

### Start Demo Environment

```bash
mcli start-demo [--hard] [--no-api] [--no-models] [--no-image-gen] [--models=MODEL1,MODEL2]
```

| Option          | Description                                      |
|-----------------|--------------------------------------------------|
| `--hard`        | Perform complete system cleanup before starting  |
| `--no-api`      | Skip API service initialization                 |
| `--no-models`   | Skip model download phase                       |
| `--no-image-gen`| Disable image generation service                |
| `--models`      | Specify comma-separated list of models to download |

**Examples:**
```bash
# Fresh start with cleanup
mcli start-demo --hard

# Minimal setup without images
mcli start-demo --no-image-gen

# Custom model selection
mcli start-demo --models=gemma2-2b-maIN,llama2-7b
```

### API Management

```bash
mcli api [--hard]
```

| Option  | Description                              |
|---------|------------------------------------------|
| `--hard`| Force clean restart of API containers    |

**Examples:**
```bash
# Normal API start
mcli api

# Clean restart
mcli api --hard
```

### Image Generation

```bash
mcli image-gen
```

Features:
- Automatic GPU detection
- Model caching
- Thumbnail previews
- Real-time progress monitoring

**Examples:**
```bash
# Start image generation service
mcli image-gen
```

### Model Management

```bash
mcli model <command> [options]
```

| Command       | Description                          | Options                     |
|---------------|--------------------------------------|-----------------------------|
| `download`    | Fetch specific model                 | `<model_name>`              |
| `list`        | Show available models                |                             |
| `present`     | List installed models                |                             |
| `update`      | Update all downloaded models         |                             |

**Examples:**
```bash
# Download specific model
mcli model download gemma2-2b-maIN

# List available models
mcli model list

# Show installed models
mcli model present

# Update all models
mcli model update
```

### Help System

```bash
mcli help [command]
```

Displays detailed help for specific commands.

**Examples:**
```bash
# General help
mcli help

# Command-specific help
mcli help start-demo
```

### Uninstallation

```bash
mcli uninstall
```

This will:
1. Remove CLI from PATH
2. Delete all installed files
3. Clean up containers
4. Remove system integrations

---

## Common Workflows

### Quickstart with Default Model
```bash
mcli start-demo --models=gemma2-2b-maIN
```

### Development Setup
```bash
mcli start-demo --no-image-gen --no-api
mcli model download llama3.2-3b-MaIN
```

---

> **Download Link Placeholder**  
> [INSERT_OFFICIAL_DOWNLOAD_LINK_HERE]
```
