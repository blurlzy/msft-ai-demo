
Console.WriteLine("<<<<<<<<<<<<<<<<<<<<<<<<<< Azure OpenAI Demo >>>>>>>>>>>>>>>>>>>>>>>>>>>");
Console.WriteLine("Connecting to Azure key vault.........");

var azureOpenAIKey = SecretManager.OpenAIKey;
var azureOpenAIEndpoint = SecretManager.OpenAIEndpoint;

Console.WriteLine("Initializing Azure OpenAI client.....");

OpenAIClient client = new(new Uri(azureOpenAIEndpoint), new AzureKeyCredential(azureOpenAIKey));
ChatCompletionsOptions options = new ChatCompletionsOptions
{
    Temperature = 0f,
    DeploymentName = "GPT4-1106", // Deployment name NOT model name
};

while (true)
{
    // menu options
    Console.WriteLine("------------------------ Topic  -------------------------");
    Console.WriteLine("     1. Azure");
    Console.WriteLine("     2. .Net / C#");
    Console.WriteLine("     2. Email Writing");
    Console.WriteLine("     #. Exit");

    var input = Console.ReadLine();
    // exit
    if(input == "#")
    {
        return;
    }
    
    if (input == "1")
    {
        options.Messages.Add(new ChatRequestSystemMessage("Suppose you are Azure expert."));
    }
    else if (input == "2")
    {
        options.Messages.Add(new ChatRequestSystemMessage("Suppose you are .Net / C# expert."));       
    }
    else if(input == "3")
    {
        options.Messages.Add(new ChatRequestSystemMessage("Suppose you are email writing expert."));
    }

    // chat complete
    CompleteChat(client, options).GetAwaiter().GetResult();
}


static async Task CompleteChat(OpenAIClient client, ChatCompletionsOptions options)
{
    while (true)
    {
        Console.WriteLine("Prompts or Enter # to exit");
        var input = Console.ReadLine();

        // exit
        if (input == "#")
        {
            return;
        }

        // load user message
        options.Messages.Add(new ChatRequestUserMessage(input));

        // show progress bar
        bool isCompleted = false;

        var apiTask = client.GetChatCompletionsAsync(options);
        while (!isCompleted)
        {
            Console.Write(".");
            await Task.Delay(300);
            if (apiTask.IsCompleted)
            {
                isCompleted = true;
            }
        }

        // Ensure the API call is completed
        var response = await apiTask;

        ChatChoice choice = response.Value.Choices[0];

        if (choice.FinishDetails is StopFinishDetails stopDetails || choice.FinishReason == CompletionsFinishReason.Stopped)
        {

        }

        // display response
        DisplayChatResponse(response);

        // save assistant message
        options.Messages.Add(new ChatRequestAssistantMessage(response.Value.Choices[0].Message.Content));
    }
}

static void DisplayChatResponse(Response<ChatCompletions>? res)
{
    Console.ForegroundColor = ConsoleColor.Green;

    // new empty line
    Console.WriteLine();

    if (res is null)
    {
        Console.WriteLine("Response is null");
        return;
    }

    Console.WriteLine(res.Value.Choices[0].Message.Content);
    Console.ForegroundColor = ConsoleColor.White;

    // new empty line
    Console.WriteLine();
}