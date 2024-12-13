1. https://docs.splunk.com/Documentation/Edge/2.1.0/Admin/Containers
2. docker build -f Dockerfile --tag=ladder99/dime:latest .
3. docker save -o dime.tar ladder99/dime:latest
2. tar -cf dime-tar.tar edge.json dime.tar
3. edge.json

```json
{
  "name": "ladder99/dime:latest",
  "containerArchive": "dime.tar",
  "portMap": ["51000:8080"],
  "mappedStorage": "/app/Configs"
}
```