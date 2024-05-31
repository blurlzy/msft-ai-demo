using Azure;
using Microsoft.SemanticKernel;
using Xunit.Abstractions;
// plugins namespace
using Plugins;

namespace OpenAIDemo.Tests
{
    public class SemanticKernelTests
    {
        //  AOAI endpoint * key
        private readonly string _azureOpenAIKey = SecretManager.OpenAIKey;
        private readonly string _azureOpenAIEndpoint = SecretManager.OpenAIEndpoint;
        private readonly string _deployment = "GPT4o";

        private readonly IKernelBuilder builder;
        private readonly Kernel _semanticKernel;
        private readonly ITestOutputHelper _output;

        // ctor
        public SemanticKernelTests(ITestOutputHelper output)
        {
            // init Ikernal Builder
            builder = Kernel.CreateBuilder()
                    .AddAzureOpenAIChatCompletion(_deployment, _azureOpenAIEndpoint, _azureOpenAIKey);

            // optional - register plugins
            builder.Plugins.AddFromType<MathPlugin>();

            // create kernel
            _semanticKernel = builder.Build();

            // output
            _output = output;
        }



        [Theory]
        [InlineData("Can you write an function which can compare two date time objects?")]
        public async Task Test_Chat1(string input)
        {
            //            var prompt = @"
            //You are a C# programming assistant, dedicated to helping users learn C# and build end-to-end projects using C# and its related libraries. 
            //Provide clear explanations of C# concepts, syntax, and best practices. 
            //Offer tailored support and resources, ensuring users gain in-depth knowledge and practical experience in working with C# and its ecosystem.
            //{{$input}}";

            var prompt = $"""
         <message role="system">Instructions: You are a C# programming assistant, dedicated to helping users learn C# and build end-to-end projects using C# and its related libraries. 
         Provide clear explanations of C# concepts, syntax, and best practices. 
         Offer tailored support and resources, ensuring users gain in-depth knowledge and practical experience in working with C# and its ecosystem.</message>
         
         <message role="user">{input}</message>
         """;


            var response = await _semanticKernel.InvokePromptAsync(prompt);

            _output.WriteLine(response.ToString());
        }


        [Theory]
        [InlineData("MathPlugin", "Sqrt", "number1", 4)]
        public async Task Test_Run_Plugin(string plugin, string function, string inputParam1Name, object inputParam1Value)
        {
            // call agent (function) MathPlugin / Sqrt method which takes 1 input param
            double answer = await _semanticKernel.InvokeAsync<double>(plugin, function, 
                new()
                {
                    { inputParam1Name, inputParam1Value } // input param
                });

            _output.WriteLine($"The square root of {inputParam1Value} is {answer}.");
        }
    }

}
