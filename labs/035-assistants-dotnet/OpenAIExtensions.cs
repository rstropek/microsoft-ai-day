using System.ClientModel;
using System.Text;
using System.Text.Json;
using OpenAI;
using OpenAI.Assistants;
#pragma warning disable OPENAI001

namespace AssistantsDotNet;

static class OpenAIExtensions
{
    public static async Task<Assistant?> FindAssistantByName(this AssistantClient client, string name)
    {
        await foreach (var assistant in client.GetAssistantsAsync())
        {
            if (assistant.Name == name) { return assistant; }
        }

        return null;
    }

    public static async Task<Assistant> CreateOrUpdate(this AssistantClient client, string model, AssistantCreationOptions assistant)
    {
        var existing = await client.FindAssistantByName(assistant.Name);
        if (existing != null)
        {
            var updateOptions = new AssistantModificationOptions
            {
                Model = model,
                Name = assistant.Name,
                Description = assistant.Description,
                Instructions = assistant.Instructions,
            };

            updateOptions.DefaultTools.Clear();
            foreach (var tool in assistant.Tools)
            {
                updateOptions.DefaultTools.Add(tool);
            }

            return await client.ModifyAssistantAsync(existing.Id, updateOptions);
        }

        return await client.CreateAssistantAsync(model, assistant);
    }

    public static async IAsyncEnumerable<string> AddMessageAndRunToCompletion(this AssistantClient client, string threadId, string assistantId,
        string message, Func<RequiredActionUpdate, Task<object>>? functionCallback = null)
    {
        await client.CreateMessageAsync(threadId, MessageRole.User, [message]);
        AsyncCollectionResult<StreamingUpdate> asyncUpdate = client.CreateRunStreamingAsync(threadId, assistantId);

        ThreadRun? currentRun;
        var codeInterpreterCode = new StringBuilder();
        do
        {
            currentRun = null;
            List<ToolOutput> outputsToSumit = [];
            await foreach (StreamingUpdate update in asyncUpdate)
            {
                if (update is RequiredActionUpdate requiredActionUpdate && functionCallback != null)
                {
                    Console.WriteLine($"Calling function {requiredActionUpdate.ToolCallId} {requiredActionUpdate.FunctionName} {requiredActionUpdate.FunctionArguments}");

                    string functionResponse;
                    try
                    {
                        var result = await functionCallback(requiredActionUpdate);
                        functionResponse = JsonHelpers.Serialize(result);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Function call failed, returning error message to ChatGPT {requiredActionUpdate.FunctionName} {ex.Message}");
                        functionResponse = ex.Message;
                    }

                    outputsToSumit.Add(new ToolOutput(requiredActionUpdate.ToolCallId, functionResponse));
                }
                else if (update is RunUpdate runUpdate) { currentRun = runUpdate; }
                else if (update is MessageContentUpdate contentUpdate)
                {
                    yield return contentUpdate.Text;
                }
                else if (update is RunStepDetailsUpdate runStepDetailsUpdate
                    && !string.IsNullOrEmpty(runStepDetailsUpdate.CodeInterpreterInput))
                {
                    codeInterpreterCode.Append(runStepDetailsUpdate.CodeInterpreterInput);
                }
            }

            if (outputsToSumit.Count != 0)
            {
                asyncUpdate = client.SubmitToolOutputsToRunStreamingAsync(threadId, currentRun!.Id, outputsToSumit);
            }
        }
        while (currentRun?.Status.IsTerminal is false);

        if (codeInterpreterCode.Length > 0)
        {
            Console.WriteLine("\n\nCODE INTERPRETER:");
            Console.WriteLine(codeInterpreterCode.ToString());
        }
    }

    public static async Task<string?> GetLatestMessage(this AssistantClient client, string threadId)
    {
        await foreach(var msgs in client.GetMessagesAsync(threadId, new MessageCollectionOptions() { Order = MessageCollectionOrder.Descending }))
        {
            return msgs?.Content[0].Text;
        }

        return null;
    }
}
