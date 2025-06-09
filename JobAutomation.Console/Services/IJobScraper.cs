using System.Collections.Generic;
using System.Threading.Tasks;
using JobAutomation.Console.Models;

namespace JobAutomation.Console.Services
{
    public interface IJobScraper
    {
        string SourceName { get; }
        Task<List<JobPost>> ScrapeJobsAsync(string searchTerm, string location);
    }
} 