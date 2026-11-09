# esep-webhooks

ESEP Assignment 8: Event-Driven Architecture.

When an issue is opened on this repo, GitHub sends a webhook to an AWS API Gateway endpoint (REST API, `POST /`, stage `default`, us-east-2). API Gateway invokes the `EsepWebhook` Lambda function (C#, .NET 10, non-proxy integration). The function reads `issue.html_url` from GitHub's JSON and posts `Issue Created: <link>` to the class Slack channel.

```
GitHub issue opened -> GitHub webhook (JSON POST) -> API Gateway /default -> Lambda EsepWebhook -> Slack #eventing-webhook
```

The Slack incoming-webhook URL is stored only in the Lambda's `SLACK_URL` environment variable and is never committed to this repo.

## Deploy

```bash
cd EsepWebhook
dotnet lambda deploy-function EsepWebhook
```
