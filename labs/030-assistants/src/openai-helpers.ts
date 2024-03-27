import OpenAI from 'openai';
import winston from 'winston';

declare module "openai" {
    export namespace OpenAI {
        export namespace Beta {
            export interface Assistants {
                findAssistantByName(name: string, logger: winston.Logger | undefined): Promise<OpenAI.Beta.Assistants.Assistant | undefined>;
                createOrUpdate(assistant: OpenAI.Beta.AssistantCreateParams, logger: winston.Logger | undefined): Promise<OpenAI.Beta.Assistant>;
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
