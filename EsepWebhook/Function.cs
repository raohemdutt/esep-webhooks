using System.Text;
using Amazon.Lambda.Core;
using Newtonsoft.Json;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace EsepWebhook;

public class Function
{
    /// <summary>
    /// Receives a GitHub "issues" webhook payload (via API Gateway, non-proxy integration)
    /// and posts the issue link to Slack using the webhook URL in the SLACK_URL environment variable.
    /// </summary>
    /// <param name="input">The raw GitHub webhook JSON.</param>
    /// <param name="context">The ILambdaContext that provides methods for logging and describing the Lambda environment.</param>
    /// <returns>Slack's response body ("ok" on success), or a short message when nothing was posted.</returns>
    public string FunctionHandler(object input, ILambdaContext context)
    {
        context.Logger.LogLine($"Received: {input}");

        // GitHub sends a "ping" event (no issue) when the webhook is created; skip anything without an issue link.
        string? issueUrl;
        try
        {
            dynamic? json = JsonConvert.DeserializeObject<dynamic>(input?.ToString() ?? "");
            issueUrl = (string?)json?.issue?.html_url;
        }
        catch (Exception ex) when (ex is JsonException or Microsoft.CSharp.RuntimeBinder.RuntimeBinderException)
        {
            context.Logger.LogLine($"Payload was not a GitHub issue JSON object: {ex.Message}");
            issueUrl = null;
        }
        if (string.IsNullOrEmpty(issueUrl))
        {
            context.Logger.LogLine("No issue.html_url in payload; nothing posted to Slack.");
            return "ignored: no issue in payload";
        }

        string? slackUrl = Environment.GetEnvironmentVariable("SLACK_URL");
        if (string.IsNullOrEmpty(slackUrl))
        {
            context.Logger.LogLine("SLACK_URL environment variable is not set.");
            return "error: SLACK_URL not configured";
        }

        string payload = JsonConvert.SerializeObject(new { text = $"Issue Created: {issueUrl}" });

        var client = new HttpClient();
        var webRequest = new HttpRequestMessage(HttpMethod.Post, slackUrl)
        {
            Content = new StringContent(payload, Encoding.UTF8, "application/json")
        };

        var response = client.Send(webRequest);
        using var reader = new StreamReader(response.Content.ReadAsStream());
        return reader.ReadToEnd();
    }
}
