using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using HtmlAgilityPack;
using Microsoft.Extensions.Configuration;
using JobAutomation.Console.Models;

namespace JobAutomation.Console.Services
{
    public class IndeedScraper : IJobScraper
    {
        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient;

        public IndeedScraper(IConfiguration configuration)
        {
            _configuration = configuration;
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.124 Safari/537.36");
        }

        public string SourceName => "Indeed";

        public async Task<List<JobPost>> ScrapeJobsAsync(string searchTerm, string location)
        {
            var jobs = new List<JobPost>();
            var baseUrl = _configuration["Indeed:BaseUrl"];
            var searchUrl = _configuration["Indeed:SearchUrl"];
            var maxPages = int.Parse(_configuration["Indeed:MaxPages"]);

            for (int page = 0; page < maxPages; page++)
            {
                var url = $"{searchUrl}?q={Uri.EscapeDataString(searchTerm)}&l={Uri.EscapeDataString(location)}&start={page * 10}";
                var html = await _httpClient.GetStringAsync(url);
                var doc = new HtmlDocument();
                doc.LoadHtml(html);

                var jobNodes = doc.DocumentNode.SelectNodes("//div[contains(@class, 'job_seen_beacon')]");
                if (jobNodes == null) break;

                foreach (var jobNode in jobNodes)
                {
                    try
                    {
                        var job = new JobPost
                        {
                            Title = jobNode.SelectSingleNode(".//h2[contains(@class, 'jobTitle')]")?.InnerText.Trim(),
                            Company = jobNode.SelectSingleNode(".//span[contains(@class, 'companyName')]")?.InnerText.Trim(),
                            Location = jobNode.SelectSingleNode(".//div[contains(@class, 'companyLocation')]")?.InnerText.Trim(),
                            Url = baseUrl + jobNode.SelectSingleNode(".//a[contains(@class, 'jcs-JobTitle')]")?.GetAttributeValue("href", ""),
                            PostedDate = DateTime.Now, // Indeed doesn't always show exact posting date
                            Description = string.Empty,
                            SeoOptimizedContent = string.Empty
                        };

                        if (!string.IsNullOrEmpty(job.Url))
                        {
                            var jobHtml = await _httpClient.GetStringAsync(job.Url);
                            var jobDoc = new HtmlDocument();
                            jobDoc.LoadHtml(jobHtml);
                            job.Description = jobDoc.DocumentNode.SelectSingleNode("//div[contains(@class, 'jobsearch-jobDescriptionText')]")?.InnerText.Trim() ?? string.Empty;
                        }

                        if (!string.IsNullOrEmpty(job.Title) && !string.IsNullOrEmpty(job.Company))
                        {
                            jobs.Add(job);
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Console.WriteLine($"Error processing job: {ex.Message}");
                    }
                }

                await Task.Delay(2000); // Be nice to Indeed's servers
            }

            return jobs;
        }
    }
} 