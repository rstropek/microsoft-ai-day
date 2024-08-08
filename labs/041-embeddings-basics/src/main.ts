import OpenAI from "openai";
import dotenv from "dotenv";
import { readLine } from "./input.js";
import { dot, norm } from "mathjs";

dotenv.config({ path: "../../.env" });

const openai = new OpenAI({
  apiKey: process.env.OPENAI_KEY,
});

const text1 = await readLine("Enter the first text: ");
const text2 = await readLine("Enter the second text: ");
const text3 = await readLine("Enter the third text: ");

console.log(
  "Now we check whether the first text is more similar to the second text or the third text."
);

const embeddings = await openai.embeddings.create({
  model: process.env.OPENAI_EMBEDDINGS ?? "",
  input: [text1, text2, text3],
});

// For demo purposes, we will print the first ten elements of the embeddings
for (const e of embeddings.data) {
    console.log(e.embedding.slice(0, 10));
}

// For demo purposes, we print the magnitude of the embeddings
for (const e of embeddings.data) {
    console.log(norm(e.embedding));
}

// Let's calculate the dot product of the first two embeddings.
// First, we do not use a method from mathjs but calculate it manually.
let dotProduct = 0;
for (let i = 0; i < embeddings.data[0].embedding.length; i++) {
    dotProduct += embeddings.data[0].embedding[i] * embeddings.data[1].embedding[i];
}
console.log('dot product of t1 and t2', dotProduct);

// Now we use the mathjs method to calculate the dot product
const dotProductMathjs = dot(embeddings.data[0].embedding, embeddings.data[1].embedding);
console.log('dot product of t1 and t2 using mathjs', dotProductMathjs);

// Next, we calculate the dot product of the first and third embeddings.
const dotProductMathjs2 = dot(embeddings.data[0].embedding, embeddings.data[2].embedding);
console.log('dot product of t1 and t3 using mathjs', dotProductMathjs2);

// Now we compare the similarities
if (dotProductMathjs > dotProductMathjs2) {
    console.log('The first text is more similar to the second text.');
} else {
    console.log('The first text is more similar to the third text.');
}