# Microsoft AI Day

## Introduction

This repository contains demos and samples for the [_Microsoft Build: AI Day (Austria)_](https://msevents.microsoft.com/event?id=3431470856). It contains the following labs:

* OpenAI Chat Completions Basics ([C# and .NET](./labs/010-basics-dotnet/) or [Python](./labs/015-basics-python/))
* [Function Calling with Chat Completions](./labs/020-functions-dotnet/) (C# and .NET)
* [Using Tools with the new _Assistant_ API](./labs/030-assistants-nodejs/) (TypeScript and Node.js)
* [Embeddings and the RAG model](./labs/040-embeddings-rag-nodejs/) (TypeScript and Node.js)

Attendees can decide the complexity level on their own:

* L200: People who are less familiar with coding can take the existing code, try to run it (description will be provided), play with prompts, and think about possible use cases in their professional or personal life.
* L300: People who are a bit familiar with coding can extend the existing code by copying and modify parts of the samples (ideas will be provided).
* L400: People who are very familiar with coding can take the samples as a starting point or inspiration and implement their own use case.

## Getting Started

We recommend that you use [_Development Containers_](https://containers.dev/) to work with this repository. Here is how you can get started:

* Make sure that you have _Docker_ installed locally.
* Install [Visual Studio Code](https://code.visualstudio.com)
* Install the [_Remote Development_ extension pack](https://marketplace.visualstudio.com/items?itemName=ms-vscode-remote.vscode-remote-extensionpack)
* Clone this repository
* Open the repository in Visual Studio Code
* When asked whether you want to reopen the folder using _Dev Containers_, say yes.
* You are all set!

If you do not want to use _Dev Containers_, install the following software on your machine:

* If you want to work with C# and .NET:
    * [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet)
    * Visual Studio 2023 (v17.9)
    * Latest version of [Visual Studio Code](https://code.visualstudio.com) with the extensions listed in [_extensions.json_](./.vscode/extensions.json)
* If you want to work with TypeScript and Node.js:
    * Latest LTS version of [Node.js](https://nodejs.org)
    * Latest version of Visual Studio Code

## Environment variables

Within this workshop we are working with a shared Azure OpenAI tenant and specific deployments. As part of the workshop you will receive an `.env` file from your instructors, so you can access the shared Azure OpenAI tenant from your project folder. After adding the `.env` file to the root of this repository, or copying the `.env.template` file and rename it, you will be able to execute the various `labs` in this repostiory.

## Sample Data

The hands-on-labs in this repo use the [_Adventure Works LT_ sample database](https://learn.microsoft.com/en-us/sql/samples/adventureworks-install-configure). If you participate in the official hackathon, your trainers will provide a connection string to an instance of _Adventure Works LT_ in Azure. If you work on the samples at home, [install _Adventure Works LT_ in Azure](https://learn.microsoft.com/en-us/sql/samples/adventureworks-install-configure?view=sql-server-ver16&tabs=ssms#deploy-to-azure-sql-database) or on a [local SQL Server](https://learn.microsoft.com/en-us/sql/samples/adventureworks-install-configure?view=sql-server-ver16&tabs=ssms#restore-to-sql-server).

We recommend to install [_Azure Data Studio_](https://azure.microsoft.com/en-us/products/data-studio) to interactively explore the sample database. Alternatively, you can use [SQL Server Management Studio](https://docs.microsoft.com/en-us/sql/ssms/download-sql-server-management-studio-ssms).
