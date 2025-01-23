using System.Text;
using Azure.AI.OpenAI;
using Azure.Identity;
using dotenv.net;
using OpenAI.Chat;

// Get environment variables from .env file. We have to go up 7 levels to get to the root of the
// git repository (because of bin/Debug/net8.0 folder).
var env = DotEnv.Read(options: new DotEnvOptions(probeForEnv: true, probeLevelsToSearch: 7));

ChatClient client;

// OPENAI
// In this sample, we use key-based authentication. This is only done because this sample
// will be done by a larger group in a hackathon event. In real world, AVOID key-based
// authentication. ALWAYS prefer Microsoft Entra-based authentication (Managed Identity)!
//client = new ChatClient(env["OPENAI_MODEL"], env["OPENAI_KEY"]);

// AZURE OPENAI (with managed identity)
var options = new DefaultAzureCredentialOptions { TenantId = "022e4faf-c745-475a-be06-06b1e1c9e39d" };
AzureOpenAIClient azureClient = new(new Uri("https://oai-rstropek-sweden.openai.azure.com/"), new DefaultAzureCredential(options));
client = azureClient.GetChatClient("gpt-4o");

List<ChatMessage> messages =
  [
    // System prompt
    new SystemChatMessage("""
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
      answer such questions.
      """),
    // Initial assistant message to get the conversation started
    new AssistantChatMessage("""
      Hi! Can I help you find the right bike?
      """),
  ];

Console.OutputEncoding = Encoding.UTF8; // This should help displaying emojis in the console

while (true)
{
  // Display the last message from the assistant
  if (messages.Last() is AssistantChatMessage am)
  {
    Console.WriteLine($"🤖: {am.Content[0].Text}");
  }

  // Ask the user for a message. Exit program in case of empty message.
  Console.Write("\nYou (just press enter to exit the conversation): ");
  var userMessage = Console.ReadLine();
  if (string.IsNullOrEmpty(userMessage)) { break; }

  // Add the user message to the list of messages to send to the API
  messages.Add(new UserChatMessage(userMessage));

  // Send the messages to the API and wait for the response. Display a
  // waiting indicator while waiting for the response.
  Console.Write("\nThinking...");
  var chatTask = client.CompleteChatAsync(messages);
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
  messages.Add(new AssistantChatMessage(response.Value.Content[0].Text));
}
