using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using WordPressPCL;
using WordPressPCL.Models;
using JobAutomation.Console.Models;
using System.Collections.Generic;

namespace JobAutomation.Console.Services
{
    public class WordPressService : IDisposable
    {
        private readonly WordPressClient _client;
        private readonly string _baseUrl;

        public WordPressService(IConfiguration configuration)
        {
            _baseUrl = configuration["WordPress:Url"] ?? throw new ArgumentNullException("WordPress:Url");
            var username = configuration["WordPress:Username"] ?? throw new ArgumentNullException("WordPress:Username");
            var applicationPassword = configuration["WordPress:ApplicationPassword"] ?? throw new ArgumentNullException("WordPress:ApplicationPassword");

            // Initialize WordPress client
            _client = new WordPressClient($"{_baseUrl}/wp-json/");
            _client.Auth.UseBasicAuth(username, applicationPassword);
        }

        public async Task<bool> PostJobAsync(JobPost job)
        {
            try
            {
                System.Console.WriteLine("Creating new post...");
                
                var post = new Post
                {
                    Title = new Title(job.Title),
                    Content = new Content(job.SeoOptimizedContent),
                    Status = Status.Publish,
                    Meta = new Dictionary<string, object>
                    {
                        { "mathrank_focus_keyword", job.FocusKeyword ?? job.Title },
                        { "mathrank_meta_description", job.Description.Length > 160 ? job.Description.Substring(0, 157) + "..." : job.Description },
                        { "mathrank_meta_title", job.Title },
                        { "mathrank_robots_index", "index" },
                        { "mathrank_robots_follow", "follow" },
                        // Alternative field names
                        { "_mathrank_focus_keyword", job.FocusKeyword ?? job.Title },
                        { "_mathrank_meta_description", job.Description.Length > 160 ? job.Description.Substring(0, 157) + "..." : job.Description }
                    }
                };

                var result = await _client.Posts.CreateAsync(post);
                
                if (result != null)
                {
                    System.Console.WriteLine($"Post published successfully! Post ID: {result.Id}");
                    return true;
                }
                
                System.Console.WriteLine("Failed to create post.");
                return false;
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"Error posting to WordPress: {ex.Message}");
                System.Console.WriteLine($"Stack trace: {ex.StackTrace}");
                return false;
            }
        }

        public void Dispose()
        {
            // No cleanup needed for WordPressPCL client
        }
    }
} 