import OpenAI from 'openai';
import { RunSubmitToolOutputsParams, TextContentBlock } from 'openai/resources/beta/threads/index.mjs';
import { CodeInterpreterToolCall, FunctionToolCall, MessageCreationStepDetails, ToolCallsStepDetails } from 'openai/resources/beta/threads/runs/steps.mjs';

// Helper methods for OpenAI API
//
// Note that the helper methods are added to the OpenAI namespace to make it easier to use them in the main code.

declare module "openai" {
    export namespace OpenAI {
        export namespace Beta {
            export interface Assistants {
                /**
                 * Finds an assistant by name
                 * 
                 * OpenAI does not offer a way to find an assistant by name. This method
                 * iterates over all assistants and returns the first one with the given name.
                 */
                findAssistantByName(name: string): Promise<OpenAI.Beta.Assistants.Assistant | undefined>;

                /**
                 * Creates an assistant if it does not exist, or updates it if it does
                 * 
                 * Note that the assistant is identified by its name.
                 */
                createOrUpdate(assistant: OpenAI.Beta.AssistantCreateParams): Promise<OpenAI.Beta.Assistant>;
            }

            export interface Threads {
                addMessageAndRunToCompletion(assistantId: string, threadId: string, message: string): Promise<OpenAI.Beta.Threads.Run>;
                getLatestMessage(threadId: string): Promise<string>;
            }
        }
    }
}

export function isToolCall(details: MessageCreationStepDetails | ToolCallsStepDetails): details is ToolCallsStepDetails {
    return details.type === 'tool_calls';
}

export function isCodeInterpreterCall(call: any): call is CodeInterpreterToolCall {
    return call.type === 'code_interpreter';
}

OpenAI.Beta.Assistants.prototype.findAssistantByName = async function (name: string): Promise<OpenAI.Beta.Assistants.Assistant | undefined> {
    try {
        // Read more about auto-pagination at https://github.com/openai/openai-node?tab=readme-ov-file#auto-pagination
        for await (const assistant of this.list({ limit: 25 })) {
            if (assistant.name === name) {
                return assistant;
            }

        }
    } catch (error: any) {
        // Read more about error handling at https://github.com/openai/openai-node?tab=readme-ov-file#handling-errors
        if (error instanceof OpenAI.APIError && error.status === 404) {
            return undefined;
        }

        throw error;
    }
}

OpenAI.Beta.Assistants.prototype.createOrUpdate = async function (assistant: OpenAI.Beta.AssistantCreateParams): Promise<OpenAI.Beta.Assistant> {
    let result = await this.findAssistantByName(assistant.name ?? '');
    if (!result) {
        result = await this.create(assistant);
    } else {
        result = await this.update(result.id, assistant);
    }

    return result;
}

OpenAI.Beta.Threads.prototype.addMessageAndRunToCompletion = async function (assistantId: string, threadId: string, message: string): Promise<OpenAI.Beta.Threads.Run> {
    await this.messages.create(threadId, { role: 'user', content: message });
    let run = await this.runs.create(threadId, { assistant_id: assistantId });

    while (run.status !== 'completed') {
        await new Promise(resolve => setTimeout(resolve, 1000)); // Wait for 1 second
        run = await this.runs.retrieve(
            run.thread_id,
            run.id
        );
    }

    return run;
}

OpenAI.Beta.Threads.prototype.getLatestMessage = async function (threadId: string): Promise<string> {
    const messages = await this.messages.list(
        threadId,
        { order: 'desc' }
    );
    const tcb = messages.data[0].content[0] as TextContentBlock;

    return tcb.text.value;
}
