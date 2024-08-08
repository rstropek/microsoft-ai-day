import OpenAI from "openai";
import dotenv from "dotenv";
import winston from "winston";
import { createConnectionPool } from "./sql.js";
import { getProductModels } from "./products.js";
import { readLine } from "./input.js";
import { QdrantClient } from "@qdrant/js-client-rest";

dotenv.config({ path: "../../.env" });

const logger = winston.createLogger({
  level: process.env.LOG_LEVEL ?? "info",
  format: winston.format.combine(
    winston.format.colorize(),
    winston.format.simple()
  ),
  transports: [new winston.transports.Console()],
});

logger.info("Creating connection pool");
const pool = await createConnectionPool(process.env.ADVENTURE_WORKS ?? "");
logger.info("Connection pool connected");

logger.info("Fetching product models");
const products = await getProductModels(pool);
logger.info("Product models complete", { count: products.length });

const openai = new OpenAI({
  apiKey: process.env.OPENAI_KEY,
});

const client = new QdrantClient({ host: "localhost", port: 6333 });

// Uncomment this line to delete the collection and start from scratch
// await client.deleteCollection('products');

const collection = await client.collectionExists("products");
if (!collection.exists) {
  logger.info("Creating collection");
  await client.createCollection("products", {
    vectors: {
      size: 3072,
      distance: "Dot",
    },
  });

  logger.info("Calculating embeddings");
  for (const product of products) {
    const description = `# ${product.productGroupDescription2}
        
        ## ${product.productGroupDescription1}
        
        ### ${product.name}
        
        ${product.description}
        `;
    const embedding = await openai.embeddings.create({
      model: process.env.OPENAI_EMBEDDINGS ?? "",
      input: description,
    });

    await client.upsert("products", {
      wait: true,
      points: [
        {
          id: product.productModelID,
          vector: embedding.data[0].embedding,
          payload: {
            name: product.name,
          },
        },
      ],
    });
  }
}

while (true) {
  const options = [
    "Do you have padles for my road bike?",
    "I am looking for padels that I can ride with my regular shoes.",
    "I got a voucher from your store and I want to buy new clothes for mountain biking. What can you recommend?",
  ];
  console.log("\n");
  for (let i = 0; i < options.length; i++) {
    console.log(`${i + 1}: ${options[i]}`);
  }
  let query = await readLine(
    "\nYou (just press enter to exit the conversation): "
  );
  if (!query) {
    break;
  }
  const selection = parseInt(query);
  if ((!isNaN(selection) && selection >= 1) || selection <= options.length) {
    query = options[selection - 1];
  }

  const queryEmbedding = await openai.embeddings.create({
    model: process.env.OPENAI_EMBEDDINGS ?? "",
    input: query,
  });

  let searchResult = await client.search("products", {
    vector: queryEmbedding.data[0].embedding,
    limit: 3,
  });

  const augmentedPrompt = `You are a helpful assistant in a bike shop. People are looking for bikes and bike parts.
Below you find relevant product models that you can recommend. ONLY use those product models. DO NOT suggest
anything else. If no product model fits, appologize that we do not have the right product for them.
If the customer asks anything not related to bikes or bike parts, tell them that you can only help with bikes and bike parts.

=== PRODUCT MODELS

${searchResult
  .map((r) => {
    const product = products.find((p) => p.productModelID === r.id);
    if (product) {
      return `${product.productModelID}: ${product.name} - ${product.description} (${product.productGroupDescription2} ${product.productGroupDescription1})`;
    }

    return "";
  })
  .join("\n\n")}`;

  logger.info("Calling ChatGPT completions", {
    prompt: augmentedPrompt,
    query,
  });
  const response = await openai.chat.completions.create({
    messages: [
      { role: "system", content: augmentedPrompt },
      { role: "user", content: query },
    ],
    model: process.env.OPENAI_MODEL ?? "",
  });

  console.log();
  console.log(response.choices[0].message.content);
}
pool.close();
