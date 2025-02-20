
# 🔧 Configuration Guide

MaIN offers flexible configuration through `appsettings.json` or environment variables. Here's how to customize your setup:

## Basic Structure

```json
{
  "MaIN": {
    "ImageGenUrl": "http://localhost:5003",
    "ModelsPath": "/app/Models",
    "MongoDbSettings": {
      "ConnectionString": "mongodb://localhost:27017",
      "DatabaseName": "MaInDB",
      "ChatsCollection": "Chats",
      "AgentsCollection": "Agents",
      "FlowsCollection": "Flows"
    }
  }
}
```

## Core Configuration

| Setting            | Description                                      | Default Value                     |
|--------------------|--------------------------------------------------|-----------------------------------|
| `ImageGenUrl`      | URL for image generation service                 | `http://localhost:5003`           |
| `ModelsPath`       | Directory for model storage                      | Platform-specific default path    |
| `<storage options>`| Choose storage details                           | _Not set (optional)_              |

## Storage Configuration Options

### MongoDB

```json
{
  "MaIN": {
    "MongoDbSettings": {
      "ConnectionString": "mongodb://localhost:27017",
      "DatabaseName": "MaInDB",
      "ChatsCollection": "Chats",
      "AgentsCollection": "Agents",
      "FlowsCollection": "Flows"
    }
  }
}
```

### SQLite

```json
{
  "MaIN": {
    "SqliteSettings": {
      "ConnectionString": "Data Source=MainDB.db"
    }
  }
}
```

### SQL Server

```json
{
  "MaIN": {
    "SqlSettings": {
      "ConnectionString": "Server=localhost;Database=MaInDB;User Id=sa;Password=your_password;"
    }
  }
}
```

### File System

```json
{
  "MaIN": {
    "FileSystemSettings": {
      "Path": "/data/storage"
    }
  }
}
```

## Environment Variables

All settings can be configured via environment variables:

```bash
# Core settings
export MAIN_IMAGEGENURL="http://localhost:5003"
export MAIN_MODELSPATH="/app/Models"

# MongoDB example
export MAIN_MONGODBSETTINGS_CONNECTIONSTRING="mongodb://localhost:27017"
export MAIN_MONGODBSETTINGS_DATABASENAME="MaInDB"

# SQLite example
export MAIN_SQLITESETTINGS_CONNECTIONSTRING="Data Source=MainDB.db"

# SQL Server example
export MAIN_SQLSETTINGS_CONNECTIONSTRING="Server=localhost;Database=MaInDB;User Id=sa;Password=your_password;"

# File System example
export MAIN_FILESYSTEMSETTINGS_PATH="/data/storage"
```

## Default Behavior

- If no storage provider is configured, MaIN uses in-memory storage.
- Models are automatically cached in the specified `ModelsPath`.
- Image generation service is optional (`ImageGenUrl` can be empty).

## Example Configurations

### Minimal Setup (In-Memory)

```json
{
  "MaIN": {
    "ModelsPath": "./models"
  }
}
```

### Production Setup (MongoDB)

```json
{
  "MaIN": {
    "ImageGenUrl": "http://image-gen:5003",
    "ModelsPath": "/var/lib/main/models",
    "MongoDbSettings": {
      "ConnectionString": "mongodb://mongo:27017",
      "DatabaseName": "ProductionDB",
      "ChatsCollection": "ProductionChats"
    }
  }
}
```

### Development Setup (SQLite)

```json
{
  "MaIN": {
    "ImageGenUrl": "http://localhost:5003",
    "ModelsPath": "C:\\Dev\\Models",
    "SqliteSettings": {
      "ConnectionString": "Data Source=DevelopmentDB.db"
    }
  }
}
```

## Configuration Precedence

1. Environment variables
2. `appsettings.json`
3. Default values
```

