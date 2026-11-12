using System;
using System.IO;
using System.Net.Http;
using System.Text;
using Amazon.Lambda.Core;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

// Lets Lambda turn the incoming JSON into the input object.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace EsepWebhook;

public class Function
{
    // One shared client for all invocations (avoids running out of sockets).
    private static readonly HttpClient client = new HttpClient();

    public string FunctionHandler(object input, ILambdaContext context)
    {
        string raw = input?.ToString() ?? "";
        context.Logger.LogLine($"Received: {raw}");

        // Pull issue.html_url out of GitHub's JSON.
        // Pings and plain test strings have no issue, so we skip them instead of crashing.
        string issueUrl = null;
        try
        {
            issueUrl = JToken.Parse(raw).SelectToken("issue.html_url")?.ToString();
        }
        catch (Exception ex)
        {
            context.Logger.LogLine($"Could not parse input: {ex.Message}");
        }

        if (string.IsNullOrEmpty(issueUrl))
        {
            return "ignored: no issue in payload";
        }

        // The Slack URL lives only in the Lambda environment variable. Never in code.
        string slackUrl = Environment.GetEnvironmentVariable("SLACK_URL");
        if (string.IsNullOrEmpty(slackUrl))
        {
            context.Logger.LogLine("SLACK_URL is not set");
            return "error: SLACK_URL is not set";
        }

        string payload = JsonConvert.SerializeObject(new { text = $"Issue Created: {issueUrl}" });

        var webRequest = new HttpRequestMessage(HttpMethod.Post, slackUrl)
        {
            Content = new StringContent(payload, Encoding.UTF8, "application/json")
        };

        var response = client.Send(webRequest);
        using var reader = new StreamReader(response.Content.ReadAsStream());
        string result = reader.ReadToEnd();

        context.Logger.LogLine($"Slack replied {(int)response.StatusCode}: {result}");
        return result;
    }
}
