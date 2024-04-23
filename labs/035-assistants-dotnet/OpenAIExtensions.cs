using Azure.AI.OpenAI.Assistants;

namespace AssistantsDotNet;

static class OpenAIExtensions
{
    public static async Task<Assistant?> FindAssistantByName(this AssistantsClient client, string name)
    {
        PageableList<Assistant> assistants;
        string? after = null;
        do
        {
            assistants = await client.GetAssistantsAsync(after: after);
            foreach (var assistant in assistants)
            {
                if (assistant.Name == name) { return assistant; }
            }

            after = assistants.LastId;
        }
        while (assistants.HasMore);

        return null;
    }

    public static async Task<Assistant> CreateOrUpdate(this AssistantsClient client, AssistantCreationOptions assistant)
    {
        var existing = await client.FindAssistantByName(assistant.Name);
        if (existing != null)
        {
            var updateOptions = new UpdateAssistantOptions()
            {
                Model = assistant.Model,
                Name = assistant.Name,
                Description = assistant.Description,
                Instructions = assistant.Instructions,
                Metadata = assistant.Metadata
            };
            foreach (var tool in assistant.Tools) { updateOptions.Tools.Add(tool); }
            foreach (var fileId in assistant.FileIds) { updateOptions.FileIds.Add(fileId); }

            return await client.UpdateAssistantAsync(existing.Id, updateOptions);
        }

        return await client.CreateAssistantAsync(assistant);
    }

    public static async Task<ThreadRun> AddMessageAndRunToCompletion(this AssistantsClient client, string threadId, string assistantId,
        string message, Func<RunStepFunctionToolCall, Task<object>>? functionCallback = null)
    {
        await client.CreateMessageAsync(threadId, MessageRole.User, message);
        var run = await client.CreateRunAsync(threadId, new CreateRunOptions(assistantId));
        Console.WriteLine($"Run created { run.Value.Id }");

        while (run.Value.Status == RunStatus.Queued || run.Value.Status == RunStatus.InProgress || run.Value.Status == RunStatus.Cancelling || run.Value.Status == RunStatus.RequiresAction)
        {
            Console.WriteLine($"Run status { run.Value.Status }");

            var steps = await client.GetRunStepsAsync(run, 1, ListSortOrder.Descending);

            // If last step is a code interpreter call, log it (including generated Python code)
            if (steps.Value.Any() && steps.Value.First().StepDetails is RunStepToolCallDetails toolCallDetails)
            {
                foreach(var call in toolCallDetails.ToolCalls)
                {
                    if (call is RunStepCodeInterpreterToolCall codeInterpreterToolCall && !string.IsNullOrEmpty(codeInterpreterToolCall.Input))
                    {
                        Console.WriteLine($"Code Interpreter Tool Call: {codeInterpreterToolCall.Input}");
                    }
                }
            }

            // Check if the run requires us to execute a function
            if (run.Value.Status == RunStatus.RequiresAction && functionCallback != null)
            {
                var toolOutput = new List<ToolOutput>();
                if (steps.Value.First().StepDetails is RunStepToolCallDetails stepDetails)
                {
                    foreach(var call in stepDetails.ToolCalls.OfType<RunStepFunctionToolCall>())
                    {
                        Console.WriteLine($"Calling function { call.Id } { call.Name } { call.Arguments }");

                        string functionResponse;
                        try
                        {
                            var result = await functionCallback(call);
                            functionResponse = JsonHelpers.Serialize(result);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Function call failed, returning error message to ChatGPT { call.Name } { ex.Message }");
                            functionResponse = ex.Message;
                        }

                        toolOutput.Add(new()
                        {
                            ToolCallId = call.Id,
                            Output = functionResponse
                        });
                    }
                }

                if (toolOutput.Count != 0)
                {
                    run = await client.SubmitToolOutputsToRunAsync(threadId, run.Value.Id, toolOutput);
                }
            }


            await Task.Delay(1000);
            run = await client.GetRunAsync(threadId, run.Value.Id);
        }

        Console.WriteLine($"Final run status { run.Value.Status }");
        return run;
    }

    public static async Task<string?> GetLatestMessage(this AssistantsClient client, string threadId)
    {
        var messages = await client.GetMessagesAsync(threadId, 1, ListSortOrder.Descending);
        if (messages.Value.FirstOrDefault()?.ContentItems[0] is MessageTextContent tc)
        {
            return tc.Text;
        }

        return null;
    }
}
