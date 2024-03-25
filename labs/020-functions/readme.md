# OpenAI API Function Calling

## Introduction

This example introduces you to the concept of function calling:

* How to make your application's functions available to the OpenAI API.
* Processing function calls
    * Handling arguments
    * Returning results
* Error handling

## Exercises

* Make yourself familiar with the code in [_FunctionCallingDotNet_](./FunctionCallingDotNet).
* Try the following prompts in a coversation and try to understand what OpenAI does and why:

    ```txt
    I am going to visit Carolyn Farino tomorrow. Tell me something about her and the products that she usually buys.
    Did she ever buy a headset?
    Give me a table by year and month of her revenues.
    ```

* Think about an application of function calling that would be relevant to your business or personal life.
* Try to ask a question that the OpenAI API cannot answer with the given functions. What happens?

## Advanced Exercises

* Add a new function to the application and try to design a prompt that makes ChatGPT call this function.
    * Example: Return details about order header and order details for a given customer and product.
* Quite hard: Use streaming to receive messages from the OpenAI API step by step.
