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
                    .AddSingleton<IJobScraper, GoogleJobsScraper>()
                    .BuildServiceProvider();

                using (var wordPressService = services.GetRequiredService<WordPressService>())
                {
                    var googleJobsScraper = services.GetRequiredService<IJobScraper>();
                    
                    // Define search parameters
                    var searchTerm = "software developer jobs Pakistan since yesterday";
                    var location = "Pakistan";
                    
                    System.Console.WriteLine($"Fetching {searchTerm} jobs in {location} from Google Jobs...");
                    var jobs = await googleJobsScraper.ScrapeJobsAsync(searchTerm, location);
                    
                    System.Console.WriteLine($"\nFound {jobs.Count} jobs. Here are the details:\n");
                    System.Console.WriteLine("===============================================");
                    
                    foreach (var job in jobs)
                    {
                        System.Console.WriteLine($"Title: {job.Title}");
                        System.Console.WriteLine($"Company: {job.Company}");
                        System.Console.WriteLine($"Location: {job.Location}");
                        System.Console.WriteLine($"URL: {job.Url}");
                        System.Console.WriteLine($"Description: {job.Description}");
                        System.Console.WriteLine("===============================================\n");
                    }

                    System.Console.WriteLine("Would you like to post these jobs to WordPress? (y/n)");
                    var response = System.Console.ReadLine()?.ToLower();
                    
                    if (response == "y")
                    {
                        System.Console.WriteLine("\nStarting to post jobs to WordPress...");
                        
                        foreach (var job in jobs)
                        {
                            System.Console.WriteLine($"\nPosting job: {job.Title} at {job.Company}...");
                            var success = await wordPressService.PostJobAsync(job);
                            
                            if (success)
                            {
                                System.Console.WriteLine($"Successfully posted job: {job.Title}");
                            }
                            else
                            {
                                System.Console.WriteLine($"Failed to post job: {job.Title}");
                            }
                            
                            // Add a small delay between posts to avoid overwhelming the server
                            await Task.Delay(2000);
                        }
                        
                        System.Console.WriteLine("\nFinished processing all jobs.");
                    }
                    else
                    {
                        System.Console.WriteLine("\nSkipping WordPress posting.");
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
