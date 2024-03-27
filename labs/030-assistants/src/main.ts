import OpenAI from 'openai';
import dotenv from 'dotenv';
import './openai-helpers.js';
import winston from 'winston';
import { TextContentBlock } from 'openai/resources/beta/threads/index.mjs';

dotenv.config({ path: '../../.env' });

const logger = winston.createLogger({
    level: process.env.LOG_LEVEL ?? 'info',
    format: winston.format.combine(winston.format.colorize(), winston.format.simple()),
    transports: [new winston.transports.Console()],
});

const openai = new OpenAI({
    baseURL: `${process.env.OPENAI_AZURE_ENDPOINT}openai`,
    apiKey: process.env.OPENAI_AZURE_KEY,
    defaultQuery: { 'api-version': '2024-03-01-preview' },
    defaultHeaders: { 'api-key': process.env.OPENAI_AZURE_KEY }
});
// Azure OpenAI has a slightly different URL schema for chat completions.
// Therefore, we have to manually adjust the path.
const chatCompletionsPath = `${openai.baseURL}/deployments/${process.env.OPENAI_AZURE_DEPLOYMENT}/chat/completions`;

let assistant = await openai.beta.assistants.createOrUpdate({
    model: process.env.OPENAI_AZURE_DEPLOYMENT ?? '',
    name: 'Revenue Analyzer',
    description: 'Retrieves customer and product revenue and analyzes it using code interpreter',
    tools: [
        { type: 'code_interpreter' }
    ],
    instructions: 'You are a helpful assistent calculating results for simple math problems using code interpreter'
}, logger);

let run = await openai.beta.threads.createAndRun({
    assistant_id: assistant.id,
    thread: {
        messages: [{ role: 'user', content: 'Please solve 3x+25=40' }]
    }
});
logger.info('Run created', { id: run.id });

while (['queued', 'in_progress', 'cancelling'].includes(run.status)) {
    logger.info('Run status', { id: run.status });
    await new Promise(resolve => setTimeout(resolve, 1000)); // Wait for 1 second
    run = await openai.beta.threads.runs.retrieve(
        run.thread_id,
        run.id
    );
}

logger.info('Final run status', { id: run.status });
if (run.status === 'completed') {
    // Note that you could get details about the steps performed by
    // the assistent by accessing `openai.beta.threads.runs.steps.list(...)`

    const messages = await openai.beta.threads.messages.list(
        run.thread_id,
        { order: 'desc' }
    );
    const tcb = messages.data[0].content[0] as TextContentBlock;

    console.log(tcb.text.value);
}
