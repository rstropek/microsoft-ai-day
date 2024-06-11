using System.Text.Json;
using dotenv.net;
using FunctionCallingDotNet;
using Microsoft.EntityFrameworkCore;
using OpenAI.Chat;

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
var client = new ChatClient("gpt-4o", env["OPENAI_KEY"]);

List<ChatMessage> messages =
  [
    // System prompt
    new SystemChatMessage("""
      You are an assistant supporting business users who need to analyze the revene of
      customers and products. Use the provided function tools to access the order database
      and answer the user's questions.

      Only answer questions related to customer and product revenue. If the user asks
      questions not related to this topic, tell her or him that you cannot
      answer such questions.

      If the user asks a question that cannot be answered with the provided function tools,
      tell her or him that you cannot answer the question because of a lack of access
      to the required data.
      """),
    // Initial assistant message to get the conversation started
    new AssistantChatMessage("""
      Hi! I can help you with questions about customer and product revenue. What would you like to know?
      """),
  ];

ChatCompletionOptions options = new()
{
    // Define the tool functions that can be called from the assistant
    Tools =
    {
        ChatTool.CreateFunctionTool(
                functionName: "getCustomers",
                functionDescription: """
                    Gets a filtered list of customers. At least one filter MUST be provided in
                    the parameters. The result list is limited to 25 customer.
                    """,
                functionParameters: BinaryData.FromObjectAsJson(
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
            ),
        ChatTool.CreateFunctionTool(
                functionName: "getProducts",
                functionDescription: """
                    Gets a filtered list of products. At least one filter MUST be
                    provided in the parameters. The result list is limited to 25 customer.
                    """,
                functionParameters: BinaryData.FromObjectAsJson(
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
            ),
        ChatTool.CreateFunctionTool(
                functionName: "getTopCustomers",
                functionDescription: """
                    Gets the customers with their revenue sorted by revenue in descending order.
                    """,
                functionParameters: BinaryData.FromObjectAsJson(
                new
                {
                    Type = "object",
                    Properties = new
                    {
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
            ),
        ChatTool.CreateFunctionTool(
                functionName: "getCustomerRevenueTrend",
                functionDescription: """
                    Gets the total revenue for a given customer per year, and month.
                    Use this function to analyze the revenue trend of a specific customer.
                    """,
                functionParameters: BinaryData.FromObjectAsJson(
                new
                {
                    Type = "object",
                    Properties = new
                    {
                        CustomerID = new
                        {
                            Type = "integer",
                            Description = "ID of the customer to get the revenue trend for."
                        }
                    },
                    Required = new[] { "customerID" }
                }, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase })
            ),
        ChatTool.CreateFunctionTool(
                functionName: "getCustomerProductBreakdown",
                functionDescription: """
                    Gets the total revenue for a given customer per product. Use this function
                    to analyze the revenue breakdown of a specific customer.
                    """,
                functionParameters: BinaryData.FromObjectAsJson(
                new
                {
                    Type = "object",
                    Properties = new
                    {
                        CustomerID = new
                        {
                            Type = "integer",
                            Description = "ID of the customer to get the revenue trend for."
                        }
                    },
                    Required = new[] { "customerID" }
                }, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase })
            )
    },
};


while (true)
{
    Console.WriteLine($"🤖: {messages.Last().Content[0].Text}");
    
    // Ask the user for a message. Exit program in case of empty message.
    string[] prompts =
    [
        "I am going to visit Carolyn Farino tomorrow. Tell me something about her and the products that she usually buys.",
        "Did she ever buy a headset?",
        "Give me a table by year and month of her revenues."
    ];
    Console.WriteLine("\n");
    for (int i = 0; i < prompts.Length; i++)
    {
        Console.WriteLine($"{i + 1}: {prompts[i]}");
    }
    Console.Write("\nYou (just press enter to exit the conversation): ");
    var userMessage = Console.ReadLine();
    if (string.IsNullOrEmpty(userMessage)) { break; }
    if (int.TryParse(userMessage, out int selection) && selection >= 1 && selection <= prompts.Length)
    {
        userMessage = prompts[selection - 1];
    }

    // Add the user message to the list of messages to send to the API
    messages.Add(new UserChatMessage(userMessage));

    bool requiresAction;
    do
    {
        requiresAction = false;

        ChatCompletion chatCompletion = await client.CompleteChatAsync(messages, options);

        switch (chatCompletion.FinishReason)
        {
            case ChatFinishReason.Stop:
                messages.Add(new AssistantChatMessage(chatCompletion.Content[0].Text));
                break;
            case ChatFinishReason.ToolCalls:
                {
                    messages.Add(new AssistantChatMessage(chatCompletion));
                    foreach (var toolCall in chatCompletion.ToolCalls)
                    {
                        Console.WriteLine($"\tExecuting tool {toolCall.FunctionName} with arguments {toolCall.FunctionArguments}.");
                        ToolChatMessage result;
                        switch (toolCall.FunctionName)
                        {
                            case "getCustomers":
                                result = await ExecuteQuery<CustomerFilter, Customer>(context, toolCall, context.GetCustomers);
                                break;

                            case "getProducts":
                                result = await ExecuteQuery<ProductFilter, Product>(context, toolCall, context.GetProducts);
                                break;

                            case "getTopCustomers":
                                result = await ExecuteQuery<TopCustomerFilter, TopCustomerResult>(context, toolCall, context.GetTopCustomers);
                                break;

                            case "getCustomerRevenueTrend":
                                result = await ExecuteQuery<CustomerDetailStatsFilter, CustomerRevenueTrendResult>(context, toolCall, context.GetCustomerRevenueTrend);
                                break;

                            case "getCustomerProductBreakdown":
                                result = await ExecuteQuery<CustomerDetailStatsFilter, CustomerProductBreakdownResult>(context, toolCall, context.GetCustomerProductBreakdown);
                                break;

                            default:
                                throw new InvalidOperationException($"Tool {toolCall.FunctionName} does not exist.");
                        }

                        messages.Add(result);
                    }

                    requiresAction = true;
                    break;
                }
            default:
                throw new NotImplementedException();
        }

    } while (requiresAction);
}

static async Task<ToolChatMessage> ExecuteQuery<TFilter, TResult>(ApplicationDataContext context, ChatToolCall toolCall, Func<TFilter, Task<TResult[]>> body)
{
    ToolChatMessage result;
    var jsonOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
    try
    {
        // Deserialize arguments
        var filter = JsonSerializer.Deserialize<TFilter>(toolCall.FunctionArguments, jsonOptions)!;

        // Get result from the database
        var customers = await body(filter);
        result = new ToolChatMessage(toolCall.Id, JsonSerializer.Serialize(customers, jsonOptions));
    }
    catch (Exception ex)
    {
        result = new ToolChatMessage(toolCall.Id, JsonSerializer.Serialize(new { Error = ex.Message }, jsonOptions));
    }

    return result;
}
