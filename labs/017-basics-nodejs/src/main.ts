import OpenAI from 'openai';
import fs from 'fs';
import dotenv from 'dotenv';
import { ChatCompletionMessageParam } from 'openai/resources/index.mjs';
import { readLine } from './inputHelpers.js';

dotenv.config({ path: '.env' });

const openai = new OpenAI({ apiKey: process.env.OPENAI_API_KEY });

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
  const response = await openai.chat.completions.create({
    messages,
    model: 'gpt-4o-mini',
  });

  // add AI response to messages
  messages.push({
    role: 'assistant',
    content: response.choices[0].message.content,
  });
}
