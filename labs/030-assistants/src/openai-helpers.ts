import OpenAI from 'openai';
import { TextContentBlock } from 'openai/resources/beta/threads/index.mjs';
import { FunctionToolCall, ToolCallsStepDetails } from 'openai/resources/beta/threads/runs/steps.mjs';
import winston from 'winston';

declare module "openai" {
    export namespace OpenAI {
        export namespace Beta {
            export interface Assistants {
                findAssistantByName(name: string, logger: winston.Logger | undefined): Promise<OpenAI.Beta.Assistants.Assistant | undefined>;
                createOrUpdate(assistant: OpenAI.Beta.AssistantCreateParams, logger: winston.Logger | undefined): Promise<OpenAI.Beta.Assistant>;
            }

            export interface Threads {
                addMessageAndRunToCompletion(assistantId: string, threadId: string, message: string, logger: winston.Logger | undefined,
                    functionCallback: (f: FunctionToolCall.Function) => Promise<any>): Promise<OpenAI.Beta.Threads.Run>;
                getLatestMessage(threadId: string): Promise<string>;
            }
        }
    }
}

OpenAI.Beta.Assistants.prototype.findAssistantByName = async function (name: string, logger: winston.Logger | undefined): Promise<OpenAI.Beta.Assistants.Assistant | undefined> {
    try {
        // Read more about auto-pagination at https://github.com/openai/openai-node?tab=readme-ov-file#auto-pagination
        for await (const assistant of this.list({ limit: 25 })) {
            if (assistant.name === name) {
                logger?.info('Assistant found', { name });
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

    logger?.info('Assistant not found', { name });
}

OpenAI.Beta.Assistants.prototype.createOrUpdate = async function (assistant: OpenAI.Beta.AssistantCreateParams, logger: winston.Logger | undefined): Promise<OpenAI.Beta.Assistant> {
    let result = await this.findAssistantByName(assistant.name ?? '', logger);
    if (!result) {
        result = await this.create(assistant);
        logger?.info('Assistant created', { id: result.id, name: result.name });
    } else {
        result = await this.update(result.id, assistant);
        logger?.info('Assistant updated', { id: result.id, name: result.name });
    }

    return result;
}

OpenAI.Beta.Threads.prototype.addMessageAndRunToCompletion = async function (assistantId: string, threadId: string, message: string, 
    logger: winston.Logger | undefined, functionCallback: (f: FunctionToolCall.Function) => Promise<any>): Promise<OpenAI.Beta.Threads.Run> {
    await this.messages.create(threadId, { role: 'user', content: message });
    let run = await this.runs.create(threadId, { assistant_id: assistantId });
    logger?.info('Run created', { id: run.id });

    while (['queued', 'in_progress', 'cancelling', 'requires_action'].includes(run.status)) {
        logger?.info('Run status', { status: run.status })
        if (run.status === 'requires_action') {
            const steps = await this.runs.steps.list(run.thread_id, run.id, { order: 'desc', limit: 1 });
            const toolCall = (steps.data[0].step_details as ToolCallsStepDetails).tool_calls[0] as FunctionToolCall;
            const functionCall = (toolCall).function
            logger?.info('Calling function', { name: functionCall.name, arguments: functionCall.arguments });
            let functionResponse: string;
            try {
                const result = await functionCallback(functionCall);
                functionResponse = JSON.stringify(result);
            } catch (error: any) {
                logger?.warn('Function call failed, returning error message to ChatGPT', { name: functionCall.name, error: error.message });
                functionResponse = error.message;
            }

            run = await this.runs.submitToolOutputs(run.thread_id, run.id, { tool_outputs: [{ tool_call_id: toolCall.id, output: functionResponse }] });
        }

        await new Promise(resolve => setTimeout(resolve, 1000)); // Wait for 1 second
        run = await this.runs.retrieve(
            run.thread_id,
            run.id
        );
    }

    logger?.info('Final run status', { id: run.status });
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
