// Temporarily commented out for WordPress testing
/*
using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using OpenAI;
using OpenAI.Managers;
using OpenAI.ObjectModels.RequestModels;
using OpenAI.ObjectModels;

namespace JobAutomation.Console.Services
{
    public class ChatGptService
    {
        private readonly OpenAIService _openAiService;

        public ChatGptService(IConfiguration configuration)
        {
            var apiKey = configuration["OpenAI:ApiKey"];
            _openAiService = new OpenAIService(new OpenAiOptions()
            {
                ApiKey = apiKey
            });
        }

        public async Task<string> OptimizeForSeoAsync(string jobTitle, string company, string location, string description)
        {
            var prompt = $@"Please optimize the following job posting for SEO while maintaining its professional tone and accuracy. 
            Include relevant keywords naturally, improve readability, and make it more engaging for potential candidates.
            Keep the essential information but enhance it for better search engine visibility.

            Job Title: {jobTitle}
            Company: {company}
            Location: {location}
            Description: {description}

            Please provide the optimized content in a well-structured format.";

            var completionResult = await _openAiService.ChatCompletion.CreateCompletion(new ChatCompletionCreateRequest
            {
                Messages = new System.Collections.Generic.List<ChatMessage>
                {
                    ChatMessage.FromSystem("You are an expert SEO content writer specializing in job postings."),
                    ChatMessage.FromUser(prompt)
                },
                Model = Models.ChatGpt3_5Turbo,
                MaxTokens = 1000
            });

            if (completionResult.Successful)
            {
                return completionResult.Choices[0].Message.Content;
            }
            else
            {
                throw new Exception($"Failed to optimize content: {completionResult.Error?.Message}");
            }
        }
    }
}
*/ 