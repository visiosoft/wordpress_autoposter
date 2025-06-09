using System.Collections.Generic;
using System.Threading.Tasks;
using JobAutomation.Console.Models;

namespace JobAutomation.Console.Services
{
    public class GoogleJobsScraper : IJobScraper
    {
        public string SourceName => "GoogleJobs";

        public async Task<List<JobPost>> ScrapeJobsAsync(string searchTerm, string location)
        {
            // Google Jobs scraping is not officially supported and is technically challenging.
            // This is a stub for demonstration purposes only.
            await Task.CompletedTask;
            return new List<JobPost>();
        }
    }
} 