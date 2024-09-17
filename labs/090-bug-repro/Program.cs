using System.ClientModel;
using System.Text.Json;
using dotenv.net;
using OpenAI.Assistants;
#pragma warning disable OPENAI001

var env = DotEnv.Read(options: new DotEnvOptions(probeForEnv: true, probeLevelsToSearch: 7));

var client = new AssistantClient(env["OPENAI_KEY"]);

var assistant = await client.CreateAssistantAsync(env["OPENAI_MODEL"], new AssistantCreationOptions
{
    Name = "Test Assistant",
    Description = "Test Assistant",
    Instructions = "You are a helpful assistant that can answer questions about the secret number.",
    Tools = {
        new CodeInterpreterToolDefinition(),
        new FunctionToolDefinition()
        {
            FunctionName = "getSecretNumber",
            Description = "Gets the secret number",
            Parameters = BinaryData.FromObjectAsJson(
                new
                {
                    Type = "object",
                    Properties = new
                    {
                        Seed = new { Type = "integer", Description = "Optional seed for the secret number." },
                    },
                    Required = Array.Empty<string>()
                }, new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase })
        }
    }
});

var thread = await client.CreateThreadAsync();
Console.WriteLine($"Thread ID: {thread.Value.Id}");

// The following line works (no function call)
//await client.CreateMessageAsync(thread.Value.Id, MessageRole.User, ["Are dolphins fish?"]);

// The following line crashes (function call)
await client.CreateMessageAsync(thread.Value.Id, MessageRole.User, ["Tell me the scret number with seed 1"]);

AsyncCollectionResult<StreamingUpdate> asyncUpdate = client.CreateRunStreamingAsync(thread.Value.Id, assistant.Value.Id);
Console.WriteLine();
ThreadRun? currentRun;
do
{
    List<ToolOutput> outputsToSumit = [];
    currentRun = null;
    await foreach (StreamingUpdate update in asyncUpdate)
    {
        if (update is RunUpdate runUpdate) { currentRun = runUpdate; }
        else if (update is RequiredActionUpdate requiredActionUpdate)
        {
            Console.WriteLine($"Calling function {requiredActionUpdate.FunctionName} {requiredActionUpdate.FunctionArguments}");
            outputsToSumit.Add(new ToolOutput(requiredActionUpdate.ToolCallId, "{ \"SecretNumber\": 42 }"));
        }
        else if (update is MessageContentUpdate contentUpdate)
        {
            Console.Write(contentUpdate.Text);
        }

        if (outputsToSumit.Count != 0)
        {
            asyncUpdate = client.SubmitToolOutputsToRunStreamingAsync(currentRun, outputsToSumit);
        }
    }

    if (outputsToSumit.Count != 0)
    {
        asyncUpdate = client.SubmitToolOutputsToRunStreamingAsync(currentRun, outputsToSumit);
    }
} while (currentRun?.Status.IsTerminal is false);

Console.WriteLine("\nDone.");
