# Embeddings and RAG with Vector Database

## Introduction

This example is similar to [040-embeddings-rag-nodejs](../040-embeddings-rag-nodejs/). The difference is that in this example, we use a vector database to store the embeddings of the product models instead of keeping them in memory.

In this example, we use the vector database [Qdrant](https://qdrant.tech/). There are many options for vector databases (see e.g. [OpenAI Cookbook](https://cookbook.openai.com/examples/vector_databases/readme)). We chose Qdrant because it is easy to run local experiments using Docker.
