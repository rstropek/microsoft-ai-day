# Code Interpreter With Function Calling

## Introduction

This example uses OpenAI's new _Assistant_ API (at the time of writing in preview) to demonstrate how to combine _Code Interpreter_ with _Function Calling_. As the Assistant API is not yet supported in .NET, this sample uses the official TypeScript SDK and Node.js

The sample covers the following topics:

* How to setup a TypeScript/Node.js project for OpenAI.
* How to manage assistants, threads, and runs with the new _Assistant_ API.
* How to enable _Code Interpreter_ and _Function Calling_ in the assistant.

## Exercises

* Make yourself familiar with the code in [_src_](./src).
* Try the prompts offered by the app (part of [_main.ts_](./src/main.ts)). Take a close look at the generated SQL queries and try to understand how ChatGPT uses _Code Interpreter_ to process the function calls' results.
* Think about another series of prompts forcing ChatGPT to combine multiple function calls and processing of the results with _Code Interpreter_.
* Think about an application of function calling that would be relevant to your business or personal life.

## Advanced Exercises

* Add a new function to the application and try to design a prompt that makes ChatGPT call this function.
    * Example: Return details about order header and order details for a given customer and product.
* Quite hard: Use streaming to receive function calls from the OpenAI API step by step.
