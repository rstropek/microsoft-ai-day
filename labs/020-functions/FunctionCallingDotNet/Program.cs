using System.Text.Json;
using Azure;
using Azure.AI.OpenAI;
using dotenv.net;
using FunctionCallingDotNet;
using Microsoft.EntityFrameworkCore;

// Get environment variables from .env file. We have to go up 7 levels to get to the root of the
// git repository (because of bin/Debug/net8.0 folder).
var env = DotEnv.Read(options: new DotEnvOptions(probeForEnv: true, probeLevelsToSearch: 7));

// Create Entity Framework Core context
var builder = new DbContextOptionsBuilder();
builder.UseSqlServer(env["ADVENTURE_WORKS"]);
var context = new ApplicationDataContext(builder.Options);

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
      You are an assistant supporting business users who need to analyze the revene of
      customers and products. Use the provided function tools to access the order database
      and answer the user's questions.

      Only answer questions related to customer and product revenue. If the user asks
      questions not related to this topic, tell her or him that you cannot
      answer such questions.
      """),
    // Initial assistant message to get the conversation started
    new ChatRequestAssistantMessage("""
      Hi! I can help you with questions about customer and product revenue. What would you like to know?
      """),
  ]
)
{
    // Define the tool functions that can be called from the assistant
    Tools =
    {
        new ChatCompletionsFunctionToolDefinition(
            new FunctionDefinition()
            {
                Name = "getCustomers",
                Description = """
                Gets a filtered list of customers. The result contains the following colums:
                * CustomerID
                * FirstName
                * MiddleName
                * LastName
                * CompanyName
                At least one filter MUST be provided in the parameters.
                Note that our customer database is a little messie. Some customers are
                multiple times in the database with different IDs. If you find customer
                entries with identical names, you can assume that they are the same customer
                and add their revenues.
                """,
                Parameters = BinaryData.FromObjectAsJson(
                new
                {
                    Type = "object",
                    Properties = new
                    {
                        CustomerID = new
                        {
                            Type = "integer",
                            Description = "Optional filter for the customer ID."
                        },
                        FirstName = new
                        {
                            Type = "string",
                            Description = "Optional filter for the first name."
                        },
                        MiddleName = new
                        {
                            Type = "string",
                            Description = "Optional filter for the middle name."
                        },
                        LastName = new
                        {
                            Type = "string",
                            Description = "Optional filter for the last name."
                        },
                        CompanyName = new
                        {
                            Type = "string",
                            Description = "Optional filter for the company name."
                        }
                    },
                    Required = Array.Empty<string>()
                }, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase })
            }),
        new ChatCompletionsFunctionToolDefinition(
            new FunctionDefinition()
            {
                Name = "getProducts",
                Description = """
                Gets a filtered list of products. The result contains the following colums:
                * ProductID
                * Name
                * ProductNumber
                * Color
                * ListPrice
                * ProductCategoryID
                At least one filter MUST be provided in the parameters.
                """,
                Parameters = BinaryData.FromObjectAsJson(
                new
                {
                    Type = "object",
                    Properties = new
                    {
                        ProductID = new
                        {
                            Type = "integer",
                            Description = "Optional filter for the customer ID."
                        },
                        Name = new
                        {
                            Type = "string",
                            Description = "Optional filter for the product name."
                        },
                        ProductNumber = new
                        {
                            Type = "string",
                            Description = "Optional filter for the product number."
                        },
                        ProductCategoryID = new
                        {
                            Type = "integer",
                            Description = "Optional filter for the product category ID."
                        }
                    },
                    Required = Array.Empty<string>()
                }, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase })
            }),
        new ChatCompletionsFunctionToolDefinition(
            new FunctionDefinition()
            {
                Name = "getCustomerRevenueStatistics",
                Description = """
                Get revenue statistics for a customer. The result contains the following colums:
                * CustomerID
                * ProductID
                * Year
                * Month
                * TotalRevenue
                """,
                Parameters = BinaryData.FromObjectAsJson(
                new
                {
                    Type = "object",
                    Properties = new
                    {
                        CustomerID = new
                        {
                            Type = "integer",
                            Description = """
                                Optional customer filter for which the revenue should be queried.
                                If not provided, the revenue for all customers is queried.
                                """
                        },
                        ProductID = new
                        {
                            Type = "integer",
                            Description = """
                                Optional product for which the revenue should be queried.
                                If not provided, the revenue for all products is queried.
                                """
                        },
                        Year = new
                        {
                            Type = "integer",
                            Description = "Optional filter for the year of the orders."
                        },
                        Month = new
                        {
                            Type = "integer",
                            Description = "Optional filter for the month of the orders."
                        }
                    },
                    Required = Array.Empty<string>()
                }, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase })
            })
    },
};

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

    bool repeat;
    do
    {
        // If the last message from the assistant was a tool call, we need to
        // add the tool call result to the list of messages to send to the API.
        // We also need to repeat the call to the API to get the next message
        // from the assistant. The next message could be another tool call.
        // We have to repeat that process until the assistant sends a message
        // that is not a tool call.
        repeat = false;

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

        if (response.Value.Choices[0].Message.ToolCalls.Any())
        {
            // We have a tool call

            foreach (var toolCall in response.Value.Choices[0].Message.ToolCalls.OfType<ChatCompletionsFunctionToolCall>())
            {
                Console.WriteLine($"\tExecuting tool {toolCall.Name} with arguments {toolCall.Arguments}.");
                ChatRequestToolMessage result;
                switch (toolCall.Name)
                {
                    case "getCustomers":
                        result = await ExecuteQuery<CustomerFilter, Customer>(context, toolCall, context.GetCustomers);
                        break;

                    case "getProducts":
                        result = await ExecuteQuery<ProductFilter, Product>(context, toolCall, context.GetProducts);
                        break;

                    case "getCustomerRevenueStatistics":
                        result = await ExecuteQuery<CustomerRevenueFilter, CustomerRevenueStats>(context, toolCall, context.GetCustomerRevenueStats);
                        break;

                    default:
                        throw new InvalidOperationException($"Tool {toolCall.Name} does not exist.");
                }

                // Add the result of the tool call to the list of messages to send to the API
                chatCompletionOptions.Messages.Add(result);
                repeat = true;
            }
        }
        else
        {
            // We don't have a tool call. Add the response from the API to the list of messages to send to the API
            chatCompletionOptions.Messages.Add(new ChatRequestAssistantMessage(response.Value.Choices[0].Message));
        }
    } while (repeat);
}

static async Task<ChatRequestToolMessage> ExecuteQuery<TFilter, TResult>(ApplicationDataContext context, ChatCompletionsFunctionToolCall toolCall, Func<TFilter, Task<TResult[]>> body)
{
    ChatRequestToolMessage result;
    try
    {
        // Deserialize arguments
        var filter = JsonSerializer.Deserialize<TFilter>(toolCall.Arguments,
            new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase })!;

        // Get result from the database
        var customers = await body(filter);
        result = new ChatRequestToolMessage(JsonSerializer.Serialize(customers), toolCall.Id);
    }
    catch (Exception ex)
    {
        result = new ChatRequestToolMessage(JsonSerializer.Serialize(new { Error = ex.Message }), toolCall.Id);
    }

    return result;
}
