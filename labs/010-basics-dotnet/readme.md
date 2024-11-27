# OpenAI API Basics

## Introduction

This example introduces you to the fundamentals of using the Azure OpenAI API. We will cover the following topics:

* How to setup your connection to Azure OpenAI
* Configuring the system prompt
* Build a conversation loop
* Send messages to OpenAI and receive the agent's response

## Exercises

* Make yourself familiar with the code in [_ApiBasicsDotNet_](./ApiBasicsDotNet).
* Think about an assistant that would be relevant to your business or personal life. Adjust the system prompt to match your assistant's purpose. Test the program and see how well it performs.
* Try to break out of the limits of the system prompt. What happens if you ask the assistant to do something that is not related to the system prompt?

## Advanced Exercises

* Setup an GPT-4o-mini model and compare the results with the GPT-4o model.
* Use streaming to receive messages from the OpenAI API step by step (solution see [here](../012-basics-dotnet-streaming/))
* Convert the console application to an ASP.NET Core web application.
