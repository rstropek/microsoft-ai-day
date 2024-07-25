import OpenAI from 'openai';
import fs from 'fs';
import dotenv from 'dotenv';
import { readLine } from './inputHelpers.js';
import './openaiHelpers.js';

dotenv.config({ path: '.env' });

let openAIType: string;
do {
  openAIType = await readLine('Do you want to use OpenAI (1) or Azure OpenAI (2)? ');
} while (openAIType !== '1' && openAIType !== '2');

let openai: OpenAI;
if (openAIType === '1') {
  openai = new OpenAI({ apiKey: process.env.OPENAI_API_KEY });
} else {
  openai = new OpenAI({
    baseURL: `${process.env.AZURE_OPENAI_BASE_URL}openai`,
    defaultQuery: { 'api-version': '2024-05-01-preview' },
    defaultHeaders: { 'api-key': process.env.AZURE_OPENAI_API_KEY },
  });
}

const systemPrompt = await fs.promises.readFile('system-prompt.md', {
  encoding: 'utf-8',
});

let assistant = await openai.beta.assistants.createOrUpdate({
  model: process.env.MODEL!,
  name: 'Bike Advisor',
  description: 'Helps customers to choose the best bike for their needs',
  instructions: systemPrompt
});

const thread = await openai.beta.threads.create();
await openai.beta.threads.messages.create(thread.id, {
  role: 'assistant',
  content: 'How can I help you today?',
});

while (true) {
  // print last message in messages
  console.log(`\n🤖: ${await openai.beta.threads.getLatestMessage(thread.id)}`);

  // get user input
  const userMessage = await readLine('\nYou (empty to quit): ');
  if (!userMessage) {
    break;
  }

  // add user message to messages
  await openai.beta.threads.addMessageAndRunToCompletion(assistant.id, thread.id, userMessage);
}
