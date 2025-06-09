using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using JobAutomation.Console.Models;
using JobAutomation.Console.Services;

namespace JobAutomation.Console.Services
{
    public class GoogleJobsScraper : IJobScraper
    {
        private readonly WordPressService _wordPressService;

        public GoogleJobsScraper(WordPressService wordPressService)
        {
            _wordPressService = wordPressService;
        }

        public string SourceName => "GoogleJobs";

        public async Task<List<JobPost>> ScrapeJobsAsync(string searchTerm, string location)
        {
            var jobs = new List<JobPost>();
            var options = new ChromeOptions();
            // Removed headless mode to make browser visible
            options.AddArgument("--disable-gpu");
            options.AddArgument("--no-sandbox");
            options.AddArgument("--disable-dev-shm-usage");
            options.AddArgument("--disable-blink-features=AutomationControlled");
            options.AddArgument("--user-agent=Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");

            using (var driver = new ChromeDriver(options))
            {
                try
                {
                    // Navigate to Google Jobs directly with software engineer search
                    driver.Navigate().GoToUrl("https://www.google.com/search?q=jobs&jbr=sep:0&udm=8&ved=2ahUKEwj4obOohOWNAxVKnf0HHb8lAeYQ3L8LegQIIxAN");
                    await Task.Delay(2000); // Increased initial wait time for page load
                    
                    // Wait for the jobs to load with increased timeout
                    var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(2));

                    // Handle the popup if it appears
                    try {
                        var notNowButton = wait.Until(d => d.FindElement(By.CssSelector("div.sjVJQd.pt054b")));
                        if (notNowButton != null && notNowButton.Displayed) {
                            System.Console.WriteLine("Found popup, clicking 'Not now'...");
                            notNowButton.Click();
                            await Task.Delay(3000); // Increased wait time after popup
                        }
                    } catch {
                        // No popup found, continue
                        System.Console.WriteLine("No popup found, continuing...");
                    }

                   
                    System.Console.WriteLine("Proceeding with job scraping...");

                    // Get all job links using class gmxZue
                    var jobLinks = driver.FindElements(By.CssSelector("span.gmxZue")).ToList();
                    System.Console.WriteLine($"Found {jobLinks.Count} job listings.");

                    if (jobLinks.Count == 0)
                    {
                        System.Console.WriteLine("No job listings found.");
                        return jobs;
                    }

                    for (int i = 0; i < jobLinks.Count; i++)
                    {
                        try
                        {
                            // Debug point 1: Start of job processing
                            System.Diagnostics.Debug.WriteLine($"\n=== Starting to process job {i + 1} ===");
                            
                            // Refresh job links list to get fresh elements
                            jobLinks = driver.FindElements(By.CssSelector("span.gmxZue")).ToList();
                            if (i >= jobLinks.Count)
                            {
                                System.Console.WriteLine("Job list changed, stopping...");
                                break;
                            }

                            var jobLink = jobLinks[i];
                            System.Console.WriteLine($"\nProcessing job {i + 1} of {jobLinks.Count}...");
                            
                            // Get the job URL from the parent anchor
                            var parentAnchor = jobLink.FindElement(By.XPath("./ancestor::a"));
                            string jobUrl = parentAnchor.GetAttribute("href");
                            System.Console.WriteLine($"Job URL: {jobUrl}");

                            // Debug point 2: Before clicking
                            System.Diagnostics.Debug.WriteLine($"About to click job link: {jobUrl}");

                            // Click the job link to open side panel using JavaScript
                            System.Console.WriteLine("Clicking job link...");
                            try {
                                // First try to click using JavaScript
                                ((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].click();", parentAnchor);
                                System.Diagnostics.Debug.WriteLine("JavaScript click successful");
                            } catch (Exception ex) {
                                System.Diagnostics.Debug.WriteLine($"JavaScript click failed: {ex.Message}");
                                // If JavaScript click fails, try regular click
                                try {
                                    parentAnchor.Click();
                                    System.Diagnostics.Debug.WriteLine("Regular click successful");
                                } catch (Exception ex2) {
                                    System.Diagnostics.Debug.WriteLine($"Regular click failed: {ex2.Message}");
                                    System.Console.WriteLine("Both click methods failed, trying to navigate directly...");
                                    driver.Navigate().GoToUrl(jobUrl);
                                }
                            }
                            
                            // Wait for the side panel to load
                            await Task.Delay(3000);

                            // Debug point 3: After clicking
                            System.Diagnostics.Debug.WriteLine("Checking for side panel...");

                            // Wait for the side panel to be visible and loaded
                            var sidePanel = wait.Until(d => d.FindElement(By.CssSelector("div.tNxQIb.PUpOsf")));
                            if (sidePanel == null || !sidePanel.Displayed)
                            {
                                System.Console.WriteLine("Side panel not visible, skipping this job...");
                                continue;
                            }

                            // Debug point 4: Side panel found
                            System.Diagnostics.Debug.WriteLine("Side panel found and visible");

                            // Wait a bit more to ensure all content is loaded
                            await Task.Delay(2000);

                            // Force refresh the side panel content
                            try {
                                // Scroll the side panel into view
                                ((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].scrollIntoView(true);", sidePanel);
                                await Task.Delay(1000);

                                // Try to force a refresh of the side panel
                                ((IJavaScriptExecutor)driver).ExecuteScript(@"
                                    var event = new Event('scroll');
                                    window.dispatchEvent(event);
                                ");
                                await Task.Delay(1000);
                                System.Diagnostics.Debug.WriteLine("Side panel refreshed");
                            } catch (Exception ex) {
                                System.Diagnostics.Debug.WriteLine($"Failed to refresh side panel: {ex.Message}");
                                System.Console.WriteLine("Could not refresh side panel");
                            }
                            
                            // Extract job information
                            System.Console.WriteLine("Extracting job information...");
                            
                            // Clear previous values
                            string title = string.Empty;
                            string company = string.Empty;
                            string jobLocation = string.Empty;
                            string description = string.Empty;

                            // Debug point 5: Before extracting title
                            System.Diagnostics.Debug.WriteLine("Attempting to extract title...");

                            // Try to get title first to confirm we're on the right job
                            try {
                                // Force a fresh find of the title element
                                var titleElement = driver.FindElement(By.CssSelector("div.tNxQIb.PUpOsf"));
                                title = titleElement.Text;
                                System.Diagnostics.Debug.WriteLine($"Title element found: {title}");
                                System.Console.WriteLine($"Raw title element text: {title}");
                                
                                if (string.IsNullOrEmpty(title))
                                {
                                    System.Console.WriteLine("Title is empty, might be wrong job panel, skipping...");
                                    continue;
                                }
                                System.Console.WriteLine($"Found title: {title}");
                            } catch (Exception ex) {
                                System.Diagnostics.Debug.WriteLine($"Failed to get title: {ex.Message}");
                                System.Console.WriteLine($"Could not find job title element: {ex.Message}");
                                continue;
                            }

                            // Debug point 6: Before extracting company
                            System.Diagnostics.Debug.WriteLine("Attempting to extract company...");

                            // Now get other details
                            try {
                                // Force a fresh find of the company element
                                var companyElement = driver.FindElement(By.CssSelector("div.wHYlTd.MKCbgd.a3jPc"));
                                company = companyElement.Text;
                                System.Diagnostics.Debug.WriteLine($"Company element found: {company}");
                                System.Console.WriteLine($"Raw company element text: {company}");
                                System.Console.WriteLine($"Found company: {company}");
                            } catch (Exception ex) {
                                System.Diagnostics.Debug.WriteLine($"Failed to get company: {ex.Message}");
                                System.Console.WriteLine($"Could not find company name element: {ex.Message}");
                            }

                            // Debug point 7: Before extracting location
                            System.Diagnostics.Debug.WriteLine("Attempting to extract location...");

                            try {
                                // Force a fresh find of the location element
                                var locationElement = driver.FindElement(By.CssSelector("div.wHYlTd.FqK3wc.MKCbgd"));
                                jobLocation = locationElement.Text;
                                System.Diagnostics.Debug.WriteLine($"Location element found: {jobLocation}");
                                System.Console.WriteLine($"Raw location element text: {jobLocation}");
                                System.Console.WriteLine($"Found location: {jobLocation}");
                            } catch (Exception ex) {
                                System.Diagnostics.Debug.WriteLine($"Failed to get location: {ex.Message}");
                                System.Console.WriteLine($"Could not find location element: {ex.Message}");
                            }

                            // Debug point 8: Before checking description button
                            System.Diagnostics.Debug.WriteLine("Checking for description button...");

                            // Check for and click "Show full description" button
                            try {
                                var showFullDescButton = driver.FindElement(By.CssSelector("span.nNzjpf-cS4Vcb-PvZLI-H2GLj"));
                                if (showFullDescButton != null && showFullDescButton.Displayed)
                                {
                                    System.Console.WriteLine("Found 'Show full description' button, clicking...");
                                    ((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].click();", showFullDescButton);
                                    await Task.Delay(2000); // Wait for description to expand
                                    System.Diagnostics.Debug.WriteLine("Description expanded");
                                }
                            } catch (Exception ex) {
                                System.Diagnostics.Debug.WriteLine($"No description button or error: {ex.Message}");
                                System.Console.WriteLine("No 'Show full description' button found or already expanded");
                            }

                            // Debug point 9: Before extracting description
                            System.Diagnostics.Debug.WriteLine("Attempting to extract description...");

                            try {
                                // Force a fresh find of the description element
                                var descElement = driver.FindElement(By.CssSelector("span.hkXmid[jsname='QAWWu']"));
                                description = descElement.Text;
                                System.Diagnostics.Debug.WriteLine($"Description element found: {description?.Substring(0, Math.Min(100, description.Length))}...");
                                System.Console.WriteLine($"Raw description element text: {description?.Substring(0, Math.Min(100, description.Length))}...");
                                System.Console.WriteLine($"Found description: {description?.Substring(0, Math.Min(100, description.Length))}...");
                            } catch (Exception ex) {
                                System.Diagnostics.Debug.WriteLine($"Failed to get description: {ex.Message}");
                                System.Console.WriteLine($"Could not find description element: {ex.Message}");
                            }

                            // Debug point 10: Before creating job post
                            System.Diagnostics.Debug.WriteLine("Creating job post object...");

                            if (!string.IsNullOrEmpty(title) && !string.IsNullOrEmpty(company))
                            {
                                var jobPost = new JobPost
                                {
                                    Title = title,
                                    Company = company,
                                    Location = jobLocation,
                                    Description = description,
                                    Url = jobUrl,
                                    PostedDate = DateTime.Now,
                                    SeoOptimizedContent = $"{title} - {company} - {jobLocation}. {description}"
                                };

                                System.Console.WriteLine("\n=== JOB DETAILS ===");
                                System.Console.WriteLine("------------------");
                                System.Console.WriteLine($"Title: {title}");
                                System.Console.WriteLine($"Company: {company}");
                                System.Console.WriteLine($"Location: {jobLocation}");
                                System.Console.WriteLine($"URL: {jobUrl}");
                                System.Console.WriteLine("\nDescription:");
                                System.Console.WriteLine("------------------");
                                System.Console.WriteLine(description);
                                System.Console.WriteLine("------------------");
                                System.Console.WriteLine("=== END OF JOB DETAILS ===\n");

                                // Debug point 11: Before WordPress posting
                                System.Diagnostics.Debug.WriteLine("Attempting to post to WordPress...");

                                // Post to WordPress
                                System.Console.WriteLine("Posting to WordPress...");
                                try
                                {
                                    /*  var success = await _wordPressService.PostJobAsync(jobPost);
                                    if (success)
                                    {
                                        System.Console.WriteLine("Successfully posted to WordPress!");
                                    }
                                    else
                                    {
                                        System.Console.WriteLine("Failed to post to WordPress.");
                                    }*/
                                }
                                catch (Exception ex)
                                {
                                    System.Diagnostics.Debug.WriteLine($"WordPress posting failed: {ex.Message}");
                                    System.Console.WriteLine($"Error posting to WordPress: {ex.Message}");
                                }
                            }
                            else
                            {
                                System.Diagnostics.Debug.WriteLine("Skipping job due to missing title or company");
                                System.Console.WriteLine("Skipping job due to missing title or company...");
                            }

                            // Wait before processing next job
                            await Task.Delay(2000);
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"Error in job processing: {ex.Message}");
                            System.Console.WriteLine($"Error processing job link: {ex.Message}");
                        }
                    }

                    System.Console.WriteLine($"\nProcessing complete. Total jobs processed: {jobs.Count}");
                    System.Console.WriteLine("Press Enter to close the browser and continue...");
                    System.Console.ReadLine();
                }
                catch (Exception ex)
                {
                    System.Console.WriteLine($"Error in Google Jobs scraping: {ex.Message}");
                }
                finally
                {
                    driver.Quit();
                }
            }

            return jobs;
        }

        private string GetElementTextWithRetry(IWebDriver driver, WebDriverWait wait, By by)
        {
            var maxRetries = 2; // Reduced from 3 to 2
            var retryCount = 0;
            
            while (retryCount < maxRetries)
            {
                try
                {
                    var element = wait.Until(d => d.FindElement(by));
                    return element.Text;
                }
                catch (Exception ex)
                {
                    retryCount++;
                    System.Console.WriteLine($"Retry {retryCount}/{maxRetries} for {by}: {ex.Message}");
                    Task.Delay(500).Wait(); // Reduced from 1000 to 500
                }
            }
            
            return string.Empty;
        }

        private string GetElementAttributeWithRetry(IWebDriver driver, WebDriverWait wait, By by, string attribute)
        {
            var maxRetries = 2; // Reduced from 3 to 2
            var retryCount = 0;
            
            while (retryCount < maxRetries)
            {
                try
                {
                    var element = wait.Until(d => d.FindElement(by));
                    return element.GetAttribute(attribute);
                }
                catch (Exception ex)
                {
                    retryCount++;
                    System.Console.WriteLine($"Retry {retryCount}/{maxRetries} for {by} attribute {attribute}: {ex.Message}");
                    Task.Delay(500).Wait(); // Reduced from 1000 to 500
                }
            }
            
            return string.Empty;
        }
    }
} 