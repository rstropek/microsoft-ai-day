import OpenAI from 'openai';
import fs from 'fs';
import dotenv from 'dotenv';
import { ChatCompletionMessageParam } from 'openai/resources/index.mjs';
import { readLine } from './inputHelpers.js';

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
    baseURL: process.env.AZURE_OPENAI_BASE_URL,
    defaultQuery: { 'api-version': '2024-05-01-preview' },
    defaultHeaders: { 'api-key': process.env.AZURE_OPENAI_API_KEY },
  });
}

const systemPrompt = await fs.promises.readFile('system-prompt.md', {
  encoding: 'utf-8',
});
const messages: ChatCompletionMessageParam[] = [
  {
    role: 'system',
    content: systemPrompt,
  },
];

while (true) {
  // print last message in messages
  console.log(`\n🤖: ${messages[messages.length - 1].content}`);

  // get user input
  const userMessage = await readLine('\nYou (empty to quit): ');
  if (!userMessage) {
    break;
  }

  // add user message to messages
  messages.push({
    role: 'user',
    content: userMessage,
  });

  // get AI response
  const options: OpenAI.RequestOptions = {};
  if (openAIType === '2') {
    options.path = `openai/deployments/${process.env.MODEL}/chat/completions`;
  }
  const response = await openai.chat.completions.create(
    {
      messages,
      model: process.env.MODEL!,
    },
    options
  );

  // add AI response to messages
  messages.push({
    role: 'assistant',
    content: response.choices[0].message.content,
  });
}
