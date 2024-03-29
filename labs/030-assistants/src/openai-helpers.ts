import OpenAI from 'openai';
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
                createAndRunToCompletion(params: ThreadCreateAndRunParamsNonStreaming, logger: winston.Logger | undefined,
                    functionCallback: (f: FunctionToolCall.Function) => Promise<any>): Promise<OpenAI.Beta.Threads.Run>;
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

OpenAI.Beta.Threads.prototype.createAndRunToCompletion = async function (params: OpenAI.Beta.ThreadCreateAndRunParamsNonStreaming, 
    logger: winston.Logger | undefined, functionCallback: (f: FunctionToolCall.Function) => Promise<any>): Promise<OpenAI.Beta.Threads.Run> {
    let run = await this.createAndRun(params);
    logger?.info('Run created', { id: run.id });

    while (['queued', 'in_progress', 'cancelling', 'requires_action'].includes(run.status)) {
        logger?.info('Run status', { status: run.status })
        if (run.status === 'requires_action') {
            const steps = await this.runs.steps.list(run.thread_id, run.id, { order: 'desc', limit: 1 });
            const toolCall = (steps.data[0].step_details as ToolCallsStepDetails).tool_calls[0] as FunctionToolCall;
            const functionCall = (toolCall).function
            logger?.info('Calling function', { name: functionCall.name, arguments: functionCall.arguments });
            const result = await functionCallback(functionCall);
            run = await this.runs.submitToolOutputs(run.thread_id, run.id, { tool_outputs: [{ tool_call_id: toolCall.id, output: JSON.stringify(result) }] });
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