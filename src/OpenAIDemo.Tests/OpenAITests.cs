using Azure;
using System;
using System.Diagnostics;
using Xunit.Abstractions;

namespace OpenAIDemo.Tests
{
    public class OpenAITests
    {
        //  AOAI endpoint * key
        private readonly string _azureOpenAIKey = SecretManager.OpenAIKey;
        private readonly string _azureOpenAIEndpoint = SecretManager.OpenAIEndpoint;

        // AOAI client
        private readonly OpenAIClient _openAIClient;

        private readonly ITestOutputHelper _output;

        // ctor
        public OpenAITests(ITestOutputHelper output)
        {
            _openAIClient = new(new Uri(_azureOpenAIEndpoint), new AzureKeyCredential(_azureOpenAIKey));
            _output = output;
        }

        [Theory]
        [InlineData("This is a testing input")]
        [InlineData("Justin L")]
        public async Task Test_Embeddings(string input)
        {
            EmbeddingsOptions embeddingOptions = new()
            {
                DeploymentName = "TextEmbedding",
                Input = { input },
            };

            Response<Embeddings> response = await _openAIClient.GetEmbeddingsAsync(embeddingOptions);


            _output.WriteLine($"Promt tokens: {response.Value.Usage.PromptTokens}");
            _output.WriteLine($"Total tokens: {response.Value.Usage.TotalTokens}");

            //foreach (float item in returnValue.Value.Data[0].Embedding.ToArray())
            //{
            //    _output.WriteLine(item.ToString());
            //}

            // The response includes the generated embedding.
            EmbeddingItem item = response.Value.Data[0];
            ReadOnlyMemory<float> embedding = item.Embedding;
            _output.WriteLine($"Embedding: {string.Join(", ", embedding.ToArray())}");
        }

        [Theory]
        [InlineData("https://staoaicsazl.blob.core.windows.net/public-images/1.png")]
        //[InlineData("https://staoaicsazl.blob.core.windows.net/public-images/2.jpg")]
        //[InlineData("https://staoaicsazl.blob.core.windows.net/public-images/3.png")]
        public async Task Test_GPT_Vision_Sync(string uri)
        {
            // Create a new Stopwatch instance
            Stopwatch stopwatch = Stopwatch.StartNew();

            ChatCompletionsOptions chatCompletionsOptions = new()
            {
                DeploymentName = "GPT4-Vision",
                MaxTokens = 1000,
                Messages =
                {
                    new ChatRequestSystemMessage("You are a helpful assistant that describes images."),
                    new ChatRequestUserMessage(
                        new ChatMessageTextContentItem("Hi! Please describe this image"),
                        new ChatMessageImageContentItem(new Uri(uri))),
                },
            };

            Response<ChatCompletions> chatResponse = await _openAIClient.GetChatCompletionsAsync(chatCompletionsOptions);
            ChatChoice choice = chatResponse.Value.Choices[0];

            
            if (choice.FinishDetails is StopFinishDetails stopDetails || choice.FinishReason == CompletionsFinishReason.Stopped)
            {
                // Stop the stopwatch
                stopwatch.Stop();

                // Print out the elapsed time
                _output.WriteLine($"Execution Time: {stopwatch.ElapsedMilliseconds} ms");

                _output.WriteLine($"{choice.Message.Role}: {choice.Message.Content}");
                _output.WriteLine($"Total tokens: {chatResponse.Value.Usage.TotalTokens}");
            }

        }

        [Theory]
        [InlineData("https://staoaicsazl.blob.core.windows.net/public-images/1.png")]
        public async Task Test_GPT_Vision_StreamMode(string uri)
        {
            // Create a new Stopwatch instance
            Stopwatch stopwatch = Stopwatch.StartNew();

            ChatCompletionsOptions chatCompletionsOptions = new()
            {
                DeploymentName = "GPT4-Vision",
                Messages =
                {
                    new ChatRequestSystemMessage("You are a helpful assistant that describes images."),
                    new ChatRequestUserMessage(
                        new ChatMessageTextContentItem("Hi! Please describe this image"),
                        new ChatMessageImageContentItem(new Uri(uri))),
                },
                MaxTokens = 1000
            };

            string role = string.Empty;
            string content = string.Empty;

            await foreach (StreamingChatCompletionsUpdate chatUpdate in _openAIClient.GetChatCompletionsStreaming(chatCompletionsOptions))
            {
                // Choice-specific information like Role and ContentUpdate will also provide a ChoiceIndex that allows
                // StreamingChatCompletionsUpdate data for independent choices to be appropriately separated.
                if (chatUpdate.ChoiceIndex.HasValue)
                {
                    int choiceIndex = chatUpdate.ChoiceIndex.Value;
                    if (chatUpdate.Role.HasValue)
                    {
                        role += $"{chatUpdate.Role.Value.ToString().ToUpperInvariant()}: ";
                    }
                    if (!string.IsNullOrEmpty(chatUpdate.ContentUpdate))
                    {
                        content += chatUpdate.ContentUpdate;
                    }
                }

                if (chatUpdate.FinishReason == CompletionsFinishReason.Stopped)
                {
                    // Stop the stopwatch
                    stopwatch.Stop();

                    // Print out the elapsed time
                    _output.WriteLine($"Execution Time: {stopwatch.ElapsedMilliseconds} ms");

                    _output.WriteLine($"role: {role}");
                    _output.WriteLine($"content: {content}");
                }
              
            }

         
        }


        [Theory]
        [InlineData("https://staoaicsazl.blob.core.windows.net/public-images/1.png")]
        public async Task Test_GPT_Vision_StreamMode_ChunkResponse(string uri)
        {
            ChatCompletionsOptions chatCompletionsOptions = new()
            {
                DeploymentName = "GPT4-Vision",
                Messages =
                {
                    new ChatRequestSystemMessage("You are a helpful assistant that describes images."),
                    new ChatRequestUserMessage(
                        new ChatMessageTextContentItem("Hi! Please describe this image"),
                        new ChatMessageImageContentItem(new Uri(uri))),
                },
                MaxTokens = 1000
            };

            string role = string.Empty;
            string content = string.Empty;

            await foreach (StreamingChatCompletionsUpdate chatUpdate in _openAIClient.GetChatCompletionsStreaming(chatCompletionsOptions))
            {
                // Choice-specific information like Role and ContentUpdate will also provide a ChoiceIndex that allows
                // StreamingChatCompletionsUpdate data for independent choices to be appropriately separated.
                if (chatUpdate.ChoiceIndex.HasValue)
                {
                    _output.WriteLine($"{chatUpdate.ContentUpdate}");
                    //int choiceIndex = chatUpdate.ChoiceIndex.Value;
                    //if (chatUpdate.Role.HasValue)
                    //{
                    //    role += $"{chatUpdate.Role.Value.ToString().ToUpperInvariant()}: ";
                    //}
                    if (!string.IsNullOrEmpty(chatUpdate.ContentUpdate))
                    {
                        content += chatUpdate.ContentUpdate;
                    }
                }

                // ! it seems like the last FinishReason is not being set, not sure if it's a bug or not
                // one of my customers is experiencing the same issue. a support ticket has been raised
                // _output.WriteLine($"Finish reason: {chatUpdate?.FinishReason?.ToString()}");

                if (chatUpdate.FinishReason == CompletionsFinishReason.Stopped)
                {
                    _output.WriteLine($"role: {role}");
                    _output.WriteLine($"content: {content}");
                    return;
                }

            }


        }


        [Theory]
        [InlineData("What are you?")]
        [InlineData("Can you show me the list of price tier for Azure API Managenment?")]
        [InlineData("Can you show me the list of price tier for Azure App Service?")]
        [InlineData("Can you show me the list of price tier for Azure Loigc Apps?")]
        [InlineData("Can you list the differences between Azure SQL database and Managed Instance?")]
        public async Task Test_GPT_Chat(string prompt)
        {
            ChatCompletionsOptions options = new ChatCompletionsOptions
            {
                Temperature = 0f,
                DeploymentName = "GPT4o", // Deployment name NOT model name
            };

            // system message
            options.Messages.Add(new ChatRequestSystemMessage("Suppose you are Azure expert."));
            // load user message
            options.Messages.Add(new ChatRequestUserMessage(prompt));

            // show progress bar
            bool isCompleted = false;

            var apiTask = _openAIClient.GetChatCompletionsAsync(options);

            while (!isCompleted)
            {
                _output.WriteLine("Loading......");
                await Task.Delay(300);
                if (apiTask.IsCompleted)
                {
                    isCompleted = true;
                }
            }

            // Ensure the API call is completed
            var response = await apiTask;

            ChatChoice choice = response.Value.Choices[0];
            _output.WriteLine(choice.Message.Content);

        }
    }
}