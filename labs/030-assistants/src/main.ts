import OpenAI from 'openai';
import dotenv from 'dotenv';
import './openai-helpers.js';
import winston from 'winston';
import { TextContentBlock } from 'openai/resources/beta/threads/index.mjs';
import { FunctionToolCall } from 'openai/resources/beta/threads/runs/steps.mjs';
import { createConnectionPool } from './sql.js';
import { getCustomers, getCustomersFunctionDefinition } from './functions.js';
import { readLine } from './input.js';

dotenv.config({ path: '../../.env' });

const logger = winston.createLogger({
    level: process.env.LOG_LEVEL ?? 'info',
    format: winston.format.combine(winston.format.colorize(), winston.format.simple()),
    transports: [new winston.transports.Console()],
});

logger.info('Creating connection pool');
const pool = await createConnectionPool(process.env.ADVENTURE_WORKS ?? '');
logger.info('Connection pool connected');

const openai = new OpenAI({
    // Note that this base URL is different from the one that you would need for 
    // chat completions API. The base URL here is for the new Assistants API.
    baseURL: `${process.env.OPENAI_AZURE_ENDPOINT}openai`,
    apiKey: process.env.OPENAI_AZURE_KEY,
    defaultQuery: { 'api-version': '2024-03-01-preview' },
    defaultHeaders: { 'api-key': process.env.OPENAI_AZURE_KEY }
});

let assistant = await openai.beta.assistants.createOrUpdate({
    model: process.env.OPENAI_AZURE_DEPLOYMENT ?? '',
    name: 'Revenue Analyzer',
    description: 'Retrieves customer and product revenue and analyzes it using code interpreter',
    tools: [
        { type: 'code_interpreter' },
        { type: 'function', function: getCustomersFunctionDefinition }
    ],
    instructions: `You are an assistant supporting business users who need to analyze the revene of
customers and products. Use the provided function tools to access the order database
and answer the user's questions.

Only answer questions related to customer and product revenue. If the user asks
questions not related to this topic, tell her or him that you cannot
answer such questions.

If the user asks a question that cannot be answered with the provided function tools,
tell her or him that you cannot answer the question because of a lack of access
to the required data.`
}, logger);

const thread = await openai.beta.threads.create();
while (true) {
    const userMessage = await readLine('\nYou (just press enter to exit the conversation): ');
    if (!userMessage) { break; }

    const run = await openai.beta.threads.addMessageAndRunToCompletion(assistant.id, thread.id, userMessage, logger, async (functionCall: FunctionToolCall.Function) => {
        if (functionCall.name === 'getCustomers') {
            return await getCustomers(pool, JSON.parse(functionCall.arguments));
        }

        return [];
    });

    if (run.status === 'completed') {
        const messages = await openai.beta.threads.messages.list(
            run.thread_id,
            { order: 'desc' }
        );
        const tcb = messages.data[0].content[0] as TextContentBlock;

        console.log(`\n🤖: ${tcb.text.value}`);
    }

}

pool.close();
