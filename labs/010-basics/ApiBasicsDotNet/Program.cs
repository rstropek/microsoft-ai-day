using System.Text;
using Azure;
using Azure.AI.OpenAI;
using dotenv.net;

// Get environment variables from .env file. We have to go up 7 levels to get to the root of the
// git repository (because of bin/Debug/net8.0 folder).
var env = DotEnv.Read(options: new DotEnvOptions(probeForEnv: true, probeLevelsToSearch: 7));

// In this sample, we use key-based authentication. This is only done because this sample
// will be done by a larger group in a hackathon event. In real world, AVOID key-based
// authentication. ALWAYS prefer Microsoft Entra-based authentication (Managed Identity)!
var client = new OpenAIClient(
    new Uri(env["OPENAI_AZURE_ENDPOINT"]),
    new AzureKeyCredential(env["OPENAI_AZURE_KEY"]));

var chatCompletionOptions = new ChatCompletionsOptions(
  env["OPENAI_AZURE_DEPLOYMENT"],
  [
    // System prompt
    new ChatRequestSystemMessage("""
      You are an assistant that helps customer to find the right bike. Options are:

      * Light, single-speed bike for urban commuting.
      * Gravel bike designed to ride on many different surfaces.
      * Cargo bike for transporting kids or goods.
      * Racing bike for sports.
      * Moutainbike designed for off-road cycling.
      * All bike types above a also available with electric motors.

      Ask the user about how she or he is going to use the bike. Make a suggestion
      based on the intended use.

      If transporting goods or kids seems to be important for the customer,
      mention the option of using a bike trailer as an alternative for cargo bikes.
      Point out that bike trailers should not be used with carbon bike frames.

      Only answer questions related to bike type selection. If the user asks
      questions not related to this topic, tell her or him that you cannot
      answer such questions-
      """),
    // Initial assistant message to get the conversation started
    new ChatRequestAssistantMessage("""
      Hi! Can I help you find the right bike?
      """),
  ]
);

Console.OutputEncoding = Encoding.UTF8; // This should help displaying emojis in the console

while (true)
{
    // Display the last message from the assistant
    if (chatCompletionOptions.Messages.Last() is ChatRequestAssistantMessage am)
    {
        Console.WriteLine($"🤖: {am.Content}");
    }

    // Ask the user for a message. Exit program in case of empty message.
    Console.Write("\nYou (just press enter to exit the conversation): ");
    var userMessage = Console.ReadLine();
    if (string.IsNullOrEmpty(userMessage)) { break; }

    // Add the user message to the list of messages to send to the API
    chatCompletionOptions.Messages.Add(new ChatRequestUserMessage(userMessage));

    // Send the messages to the API and wait for the response. Display a
    // waiting indicator while waiting for the response.
    Console.Write("\nThinking...");
    var chatTask = client.GetChatCompletionsAsync(chatCompletionOptions);
    while (!chatTask.IsCompleted)
    {
        Console.Write(".");
        await Task.Delay(1000);
    }

    Console.WriteLine("\n");
    var response = await chatTask;
    if (response.GetRawResponse().IsError)
    {
        Console.WriteLine($"Error: {response.GetRawResponse().ReasonPhrase}");
        break;
    }

    // Add the response from the API to the list of messages to send to the API
    chatCompletionOptions.Messages.Add(new ChatRequestAssistantMessage(response.Value.Choices[0].Message));
}
