# API Endpoints

The FeatBit Agent provides several REST API endpoints for managing the agent's data and status. These endpoints are primarily used for manual mode operations and administrative tasks.

## Authentication

All API endpoints require authentication using an API key. The API key must be included in the request headers:

```http
Authorization: <your-api-key>
```

## Base URL

The default base URL for the API is:

```http
http://localhost:6100/api/proxy
```

Replace `localhost:6100` with your agent's actual host and port.

## Endpoints

### Get Agent Status

Retrieves the current operational status of the FeatBit agent.

```http
GET /api/proxy/status
```

#### Example Response

```json
{
  "serves": "p1:dev,p2:dev,p3:dev",
  "dataVersion": 1234567890,
  "syncState": "stable",
  "lastSyncedAt": "2023-10-01T12:00:00Z",
  "reportedAt": "2025-07-07T12:00:00Z"
}
```

#### Response Fields

| Field          | Type   | Description                                                                                                                            |
|----------------|--------|----------------------------------------------------------------------------------------------------------------------------------------|
| `serves`       | string | List of environments now being served                                                                                                  |
| `dataVersion`  | number | Latest data timestamp (in milliseconds) stored in the agent                                                                            |
| `syncState`    | string | Data synchronizer status (`None`, `Starting`, `Interrupted`, `Stable`, `Stopped`). For `manual` mode, this value will always be `None` |
| `lastSyncedAt` | string | Last time the data synchronizer synchronized data. For `manual` mode, this value will always be `null`                                 |
| `reportedAt`   | string | Current time when the agent reported its status                                                                                        |

#### Example

```bash
curl -H "ApiKey: your-api-key" \
  http://localhost:6100/api/proxy/status
```

### Bootstrap Agent Data

Populates the agent with feature flag and segment data. This endpoint is primarily used in manual mode to populate or update the agent's data store.

```http
POST /api/proxy/bootstrap
```

#### Request Body

The request body should contain a complete dataset in JSON format:

```json
{
  "eventType": "rp-full",
  "items": [
    {
      "envId": "env-001",
      "secrets": [ ... ],
      "featureFlags": [ ... ],
      "segments": [ ... ]
    }
  ]
}
```

#### Response

```json
{
  "serves": "p1:dev,p2:dev,p3:dev",
  "dataVersion": 1234567890
}
```

#### Response Fields

| Field         | Type   | Description                                                 |
|---------------|--------|-------------------------------------------------------------|
| `serves`      | string | List of environments now being served                       |
| `dataVersion` | number | Latest data timestamp (in milliseconds) stored in the agent |

#### Example

```bash
curl -X POST \
  -H "Content-Type: application/json" \
  -H "ApiKey: your-api-key" \
  -d @data.json \
  http://localhost:6100/api/proxy/bootstrap
```

#### Notes

- After successful bootstrapping, all connected SDKs will be notified of data changes
- The agent will immediately start serving the new configuration
- This operation replaces all existing data in the agent's store

### Create Data Backup

Exports the complete current state of the agent's data store.

```http
GET /api/proxy/backup
```

#### Response

Returns a complete snapshot of all stored data in the same format as the bootstrap endpoint accepts:

```json
{
  "eventType": "rp-full",
  "items": [
    {
      "envId": "env-001",
      "secrets": [ ... ],
      "featureFlags": [...],
      "segments": [...]
    }
  ]
}
```

#### Example

```bash
curl -H "ApiKey: your-api-key" \
  http://localhost:6100/api/proxy/backup > backup.json
```

#### Use Cases

- **Data Migration**: Move data between agent instances
- **Disaster Recovery**: Create periodic backups for restoration
- **Air-gapped Deployments**: Export data for offline environments
- **Development**: Create test datasets from production data