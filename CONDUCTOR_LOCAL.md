# Local Conductor Setup

Use the GHCR images (public) instead of unavailable Docker Hub tags.

## Start
```powershell
docker compose -f docker-compose.conductor.ghcr.yml up -d
```

Wait until health:
```powershell
docker inspect --format='{{json .State.Health.Status}}' conductor-server
```
Should return "healthy".

UI: http://localhost:5001  (configure server URL there if prompted: http://localhost:8080)
API base: http://localhost:8080/api/

## Stop & clean
```powershell
docker compose -f docker-compose.conductor.ghcr.yml down
docker volume rm ork esdemo_conductor_pg_data  # optional wipe
```

## Troubleshooting
- If pull fails: ensure Docker Desktop is running and you're logged in (not required for public GHCR, but rate limits may apply).
- Health stuck: check logs
```powershell
docker logs conductor-server --tail 200
```
- Port conflicts: change the left side of `8080:8080` or `5001:80`.

## Integrating with the API
Set in `appsettings.Development.json`:
```json
"Conductor": {
  "BaseUrl": "http://localhost:8080/api/",
  "Enabled": true,
  "ApiKey": "",
  "ApiSecret": ""
}
```
Then restart the API and register metadata:
```powershell
Invoke-RestMethod -Method Post http://localhost:5221/api/registration/register-tasks
Invoke-RestMethod -Method Post http://localhost:5221/api/registration/register-workflow
```

Start a workflow via creating a loan request.
