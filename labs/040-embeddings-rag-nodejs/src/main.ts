import OpenAI from 'openai';
import dotenv from 'dotenv';
import winston from 'winston';
import { createConnectionPool } from './sql.js';
import { getProductModels } from './products.js';
import { Embedding } from 'openai/resources/embeddings.mjs';
import fs from 'fs';
import { readLine } from './input.js';
import { dot } from 'mathjs';

dotenv.config({ path: '../../.env' });

const logger = winston.createLogger({
    level: process.env.LOG_LEVEL ?? 'info',
    format: winston.format.combine(winston.format.colorize(), winston.format.simple()),
    transports: [new winston.transports.Console()],
});

logger.info('Creating connection pool');
const pool = await createConnectionPool(process.env.ADVENTURE_WORKS ?? '');
logger.info('Connection pool connected');

logger.info('Fetching product models');
const products = await getProductModels(pool);
logger.info('Product models complete', { count: products.length });

const openai = new OpenAI({
    baseURL: `${process.env.OPENAI_AZURE_ENDPOINT}/openai/deployments/${process.env.OPENAI_AZURE_EMBEDDINGS}/`,
    apiKey: process.env.OPENAI_AZURE_KEY,
    defaultQuery: { 'api-version': '2024-03-01-preview' },
    defaultHeaders: { 'api-key': process.env.OPENAI_AZURE_KEY }
});

const productEmbeddings = new Map<number, Embedding>();

// Calculate embeddings if embeddings.json does not exist. Otherwise, load embeddings from file.
if (fs.existsSync('embeddings.json')) {
    logger.info('Loading embeddings from embeddings.json');
    const embeddings = JSON.parse(fs.readFileSync('embeddings.json', 'utf-8'));
    for (const [productModelID, embedding] of embeddings) {
        productEmbeddings.set(productModelID, embedding);
    }
} else {
    logger.info('Calculating embeddings');
    for (const product of products) {
        const description = `# ${product.productGroupDescription2}
        
        ## ${product.productGroupDescription1}
        
        ### ${product.name}
        
        ${product.description}
        `
        const embedding = await openai.embeddings.create({ model: process.env.OPENAI_AZURE_EMBEDDINGS ?? '', input: description });
        productEmbeddings.set(product.productModelID, embedding.data[0]);
    }

    fs.writeFileSync('embeddings.json', JSON.stringify([...productEmbeddings.entries()]));
}

while (true) {
    const options = [
        'Do you have padles for my road bike?',
        'I am looking for padels that I can ride with my regular shoes.',
        'I got a voucher from your store and I want to buy new clothes for mountain biking. What can you recommend?',
        'I got a voucher from your store and I want to buy new clothes for mountain biking. What can you recommend?'
    ];
    console.log('\n');
    for (let i = 0; i < options.length; i++) {
        console.log(`${i + 1}: ${options[i]}`);
    }
    let query = await readLine('\nYou (just press enter to exit the conversation): ');
    if (!query) { break; }
    const selection = parseInt(query);
    if (!isNaN(selection) && selection >= 1 || selection <= options.length) {
        query = options[selection - 1];
    }

    const queryEmbedding = await openai.embeddings.create({ model: process.env.OPENAI_AZURE_EMBEDDINGS ?? '', input: query });

    const similarities = new Map<number, number>();
    for (const [productModelID, productEmbedding] of productEmbeddings) {
        const similarity = dot(productEmbedding.embedding, queryEmbedding.data[0].embedding);
        similarities.set(productModelID, similarity);
    }

    // Print three most similar products
    const mostSimilar = [...similarities.entries()].sort((a, b) => b[1] - a[1]).slice(0, 10);

    const augmentedPrompt = `You are a helpful assistant in a bike shop. People are looking for bikes and bike parts.
Below you find relevant product models that you can recommend. ONLY use those product models. DO NOT suggest
anything else. If no product model fits, appologize that we do not have the right product for them.
If the customer asks anything not related to bikes or bike parts, tell them that you can only help with bikes and bike parts.

=== PRODUCT MODELS

${(
            mostSimilar.map(([productModelID, _]) => {
                const product = products.find(p => p.productModelID === productModelID);
                if (product) {
                    return `${product.productModelID}: ${product.name} - ${product.description} (${product.productGroupDescription2} ${product.productGroupDescription1})`;
                }

                return '';
            }).join('\n\n')
        )}`;

    logger.info('Calling ChatGPT completions', { prompt: augmentedPrompt, query });
    const response = await openai.chat.completions.create({
        messages: [
            { role: 'system', content: augmentedPrompt },
            { role: 'user', content: query }
        ],
        model: process.env.OPENAI_AZURE_DEPLOYMENT ?? ''
    },
        {
            path: `${process.env.OPENAI_AZURE_ENDPOINT}/openai/deployments/${process.env.OPENAI_AZURE_DEPLOYMENT}/chat/completions`
        });

    console.log();
    console.log(response.choices[0].message.content);
}
pool.close();
