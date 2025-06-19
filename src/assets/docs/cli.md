
# 🔧 MaIN Command Line Interface (CLI)

The MaIN CLI (`mcli`) is your companion tool for managing AI workflows and services. Designed for simplicity with zero configuration requirements.

## Installation

### (Windows)
> **Requires Administrator privileges**  
> You might need to run `Set-ExecutionPolicy -Scope CurrentUser -ExecutionPolicy Unrestricted`
> The installer will:
> - Create `%LOCALAPPDATA%\MaIN\CLI` directory
> - Add `mcli` to system PATH
> - Set execution policies
> - Create uninstaller

Download and unpack content from this link [Download](https://1drv.ms/u/c/8dd72529df58a475/EXWkWN8pJdcggI1tAAAAAAABD0eIFVX7HhjwDubuEr1T9w?e=QplVP8)

```bash
.\install-mcli.ps1
```

### (Linux/Mac)
Download and unpack content from this link [Download](https://1drv.ms/u/c/8dd72529df58a475/EXWkWN8pJdcggI1zAAAAAAABMMmdRp0OgzMEwBFB4Gftvg?e=V3slNN)
> - You might need some sudo permissions to set default model path or run api scripts

```bash
.\install-mcli.sh
```

## Command Reference

### Infer

```bash
mcli infer chat [--model] [--path] [--no-models] [--no-image-gen] [--models=MODEL1,MODEL2]
```

| Option          | Description                                      |
|-----------------|--------------------------------------------------|
| `--model`       | Name of model that will be used                  |
| `--path`      | Path to model file (if not one of supported models)|
| `--backend`   | If you want to use one of integration you need to specify desired backend|

**Examples:**
```bash
# Use one of supported models (you can check what is supported by calling mcli model list)
mcli infer chat --model gemma2:2b

# Use cuseom model (name will be displayed in UI)
mcli infer chat --path /directory/my_models/some_model.gguf --model MyCustomModel

# Use openAi integration
mcli infer chat --model o1-mini --backend openai
mcli infer chat --model dall-e-3 --backend openai

#You can also use local image generation the same way so just --model FLUX.1_Shnell 
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

### Configuration variables
```bash
mcli config set
```
Used to request globally environment variables that package can use. At the moment it supports OPEN_AI_KEY and ModelsPath
Command will ask you to fill desired variable, you can just run mcli config set and follow on screen instruction

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

### Quickstart with supported model

```bash
mcli infer chat --model llama3.2:3b
```
