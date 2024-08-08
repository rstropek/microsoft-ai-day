# Introduction to Embeddings with OpenAI

This project demonstrates how to use OpenAI's API to generate embeddings for text inputs and compare their similarities. By following this code, you will learn how to:

* Use OpenAI's API to create embeddings for the input texts.
* Inspect the embeddings and the magnitude of the embeddings.
* Compute the dot product of embeddings manually and using the _mathjs_ library.
* Compare the similarities between different text inputs based on their embeddings.

## Usage

* Follow the prompts to enter three different texts.
* Observe the output, which includes:
  * The first ten elements of each embedding.
   * The magnitude of each embedding.
   * The dot product of the first text with the second and third texts.
   * A comparison of which text is more similar to the first text.

## Example inputs

* Although sentences 1 and 2 use different words (soccer, football), the cosine similarity of 1 and 2 is higher compared to 1 and 3 because the meaning of 1 and 2 is more similar.
    * I enjoy playing soccer on weekends.
    * Football is my favorite sport. Playing it on weekends with friends helps me to relax.
    * In Austria, people often watch soccer on TV on weekends.

* Here we test whether the OpenAI embedding model "understands", that the contextual meaning of "Java" is different in sentences 1 and 2. Therefore, the cosine similarity of 1 and 3 is higher as both are programming-related.
    * He is interested in Java programming.
    * He visited Java last summer.
    * He recently started learning Python programming.

* The next example deals with negation handling. All three sentences are about whether someone likes going to the gym. Sentences 1 and 3 are positive (i.e. like training in the gym), while 2 is not. Therefore, 1 and 3 have a higher cosine similarity.
    * I like going to the gym.
    * I don't like going to the gym.
    * I don't dislike going to the gym.

* Let's take a look at idiomatic expressions. Sentences 1 and 2 have very similar meaning. 3 also contains "cats and dogs", but the meaning is different. As a result, cosine similarity between 1 and 2 is higher.
    * It's raining cats and dogs.
    * The weather is very bad, it's pouring outside.
    * Cats and dogs don't go outside when it rains.

* The next examples demonstrate that embedding models have been pre-trained with data about the real world. They understand certain domain-specific terms like "virus" and "Voron".
    * The computer was infected with a virus.
    * The patient's viral load is detectable.
    * She is updating the antivirus software on her laptop.

    * I need to get better slicing skills to make the most of my Voron.
    * 3D printing is a worth-while hobby.
    * Can I have a slice of bread?

* The last example demonstrates the limits of embeddings. Berry Harris is a well-known teacher in Jazz. Using "the 6th on the 5th" is typical for him. One must know Berry Harris and the musical theory that he has taught to understand the similarity of the sentences 1 and 2. OpenAI embeddings do not understand that.
    * I like how Barry Harris described Jazz theory.
    * Playing the 6th on the 5th is an important concept that you must understand.
    * My friends Barry and Harris often visit me to play computer games.

Come up with your own examples and test the embeddings!
