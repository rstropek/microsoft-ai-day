using AssistantsDotNet;
using dotenv.net;
using Microsoft.Data.SqlClient;
using OpenAI.Assistants;
#pragma warning disable OPENAI001

// Get environment variables from .env file. We have to go up 7 levels to get to the root of the
// git repository (because of bin/Debug/net8.0 folder).
var env = DotEnv.Read(options: new DotEnvOptions(probeForEnv: true, probeLevelsToSearch: 7));

// Open connection to Adventure Works
using var sqlConnection = new SqlConnection(env["ADVENTURE_WORKS"]);
await sqlConnection.OpenAsync();

// In this sample, we use key-based authentication. This is only done because this sample
// will be done by a larger group in a hackathon event. In real world, AVOID key-based
// authentication. ALWAYS prefer Microsoft Entra-based authentication (Managed Identity)!
var client = new AssistantClient(env["OPENAI_KEY"]);

var assistant = await client.CreateOrUpdate(env["OPENAI_MODEL"], new AssistantCreationOptions
{
    Name = "Revenue Analyzer",
    Description = "Retrieves customer and product revenue and analyzes it using code interpreter",
    Instructions = """
        You are an assistant supporting business users who need to analyze the revene of
        customers and products. Use the provided function tools to access the order database
        and answer the user's questions.

        Only answer questions related to customer and product revenue. If the user asks
        questions not related to this topic, tell her or him that you cannot
        answer such questions.

        If the user asks a question that cannot be answered with the provided function tools,
        tell her or him that you cannot answer the question because of a lack of access
        to the required data.
        """,
    Tools = {
        new CodeInterpreterToolDefinition(),
        Functions.GetCustomersFunctionDefinition,
        Functions.GetProductsFunctionDefinition,
        Functions.GetCustomerProductsRevenueFunctionDefinition,
    }
});

var thread = await client.CreateThreadAsync();
while (true)
{
    string[] options =
    [
        "I will visit Orlando Gee tomorrow. Give me a revenue breakdown of his revenue per product (absolute revenue and percentages). Also show me his total revenue.",
        "Now show me a table with his revenue per year and month.",
        "The table is missing some months. Probably because they did not buy anything in those months. Complete the table by adding 0 revenue for all missing months.",
        "Show me the data in a table. Include not just percentage values, but also absolute revenue"
    ];
    Console.WriteLine("\n");
    for (int i = 0; i < options.Length; i++)
    {
        Console.WriteLine($"{i + 1}: {options[i]}");
    }
    Console.Write("You (just press enter to exit the conversation): ");
    var userMessage = Console.ReadLine();
    if (string.IsNullOrEmpty(userMessage)) { break; }
    if (int.TryParse(userMessage, out int selection) && selection >= 1 && selection <= options.Length)
    {
        userMessage = options[selection - 1];
    }

    var first = true;
    await foreach (var message in client.AddMessageAndRunToCompletion(thread.Value.Id, assistant.Id, userMessage, async functionCall =>
    {
        switch (functionCall.FunctionName)
        {
            case "getCustomers":
                return await Functions.GetCustomers(sqlConnection, JsonHelpers.Deserialize<Functions.GetCustomersParameters>(functionCall.FunctionArguments)!);
            case "getProducts":
                return await Functions.GetProducts(sqlConnection, JsonHelpers.Deserialize<Functions.GetProductsParameters>(functionCall.FunctionArguments)!);
            case "getCustomerProductsRevenue":
                return await Functions.GetCustomerProductsRevenue(sqlConnection, JsonHelpers.Deserialize<Functions.GetCustomerProductsRevenueParameters>(functionCall.FunctionArguments)!);
            default:
                throw new Exception($"Function {functionCall.FunctionName} is not supported");
        }
    }))
    {
        if (first) { Console.Write($"\n🤖: "); first = false; }
        Console.Write(message);
    }

    Console.WriteLine();
    //var lastMessage = await client.GetLatestMessage(thread.Value.Id);
}
