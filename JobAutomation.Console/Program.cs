using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using JobAutomation.Console.Services;
using JobAutomation.Console.Models;

namespace JobAutomation.Console
{
    class Program
    {
        static async Task Main(string[] args)
        {
            try
            {
                // Set up configuration
                var configuration = new ConfigurationBuilder()
                    .SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile("appsettings.json", optional: false)
                    .Build();

                // Set up dependency injection
                var services = new ServiceCollection()
                    .AddSingleton<IConfiguration>(configuration)
                    .AddSingleton<WordPressService>()
                    .BuildServiceProvider();

                using (var wordPressService = services.GetRequiredService<WordPressService>())
                {
                    // Create a test job post
                    var testJob = new JobPost
                    {
                        Title = "Test REST API Post with Focus Keyword",
                        Company = "Test Company",
                        Location = "Test Location",
                        Description = "This is a test description for a job posting that includes a focus keyword for SEO optimization.",
                        Url = "https://example.com/test-job",
                        PostedDate = DateTime.Now,
                        FocusKeyword = "test job posting",
                        SeoOptimizedContent = @"<h2>Test Job Post</h2>
<p>This is a test post using the WordPress REST API with application password authentication and focus keyword support.</p>
<h3>Requirements:</h3>
<ul>
    <li>Test requirement 1</li>
    <li>Test requirement 2</li>
</ul>
<h3>Benefits:</h3>
<ul>
    <li>Test benefit 1</li>
    <li>Test benefit 2</li>
</ul>"
                    };

                    // Post to WordPress
                    System.Console.WriteLine("Posting test content to WordPress using REST API...");
                    var success = await wordPressService.PostJobAsync(testJob);
                    
                    if (success)
                    {
                        System.Console.WriteLine("Successfully posted test content to WordPress.");
                    }
                    else
                    {
                        System.Console.WriteLine("Failed to post test content to WordPress. Please check the application password and try again.");
                        return; // Exit the program if posting failed
                    }
                }
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"An error occurred: {ex.Message}");
                System.Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }
    }
}
