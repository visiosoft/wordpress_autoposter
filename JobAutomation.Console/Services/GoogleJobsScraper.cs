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
                    driver.Navigate().GoToUrl("https://www.google.com/search?q=jobs&jbr=sep:0");
                    await Task.Delay(2000); // Wait for the page to load
                    
                    // Wait for the jobs to load with increased timeout
                    var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(30));

                    // Handle the popup if it appears
                    try {
                        var notNowButton = wait.Until(d => d.FindElement(By.CssSelector("div.sjVJQd.pt054b")));
                        if (notNowButton != null && notNowButton.Displayed) {
                            System.Console.WriteLine("Found popup, clicking 'Not now'...");
                            notNowButton.Click();
                            await Task.Delay(2000); // Wait for popup to disappear
                        }
                    } catch {
                        // Ignore if no popup found
                        System.Console.WriteLine("No popup found, continuing...");
                    }

                    // Click on "100+ more jobs" span first
                    try {
                        var moreJobsSpan = wait.Until(d => d.FindElement(By.CssSelector("span.LGwnxb")));
                        if (moreJobsSpan != null && moreJobsSpan.Displayed) {
                            System.Console.WriteLine("Found '100+ more jobs' span, clicking...");
                            // Scroll into view
                            ((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].scrollIntoView(true);", moreJobsSpan);
                            await Task.Delay(1000);
                            
                            // Try to click
                            try {
                                moreJobsSpan.Click();
                            } catch {
                                // If regular click fails, try JavaScript click
                                ((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].click();", moreJobsSpan);
                            }
                            System.Console.WriteLine("Clicked '100+ more jobs' successfully");
                            await Task.Delay(2000); // Wait for more jobs to load
                        }
                    } catch (Exception ex) {
                        System.Console.WriteLine($"Error clicking '100+ more jobs': {ex.Message}");
                    }

                    // Find all elements with class="EimVGf"
                    try {
                        var elements = wait.Until(d => d.FindElements(By.CssSelector("div.EimVGf")));
                        System.Console.WriteLine($"Found {elements.Count} elements with class='EimVGf'");
                        
                        foreach (var element in elements)
                        {
                            try {
                                System.Console.WriteLine("Processing element...");
                                // Scroll element into view
                                ((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].scrollIntoView(true);", element);
                                await Task.Delay(1000);
                                
                                // Try to click the element
                                try {
                                    element.Click();
                                } catch {
                                    // If regular click fails, try JavaScript click
                                    ((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].click();", element);
                                }
                                
                                System.Console.WriteLine("Clicked element successfully");
                                await Task.Delay(2000); // Wait between clicks
                            } catch (Exception ex) {
                                System.Console.WriteLine($"Error clicking element: {ex.Message}");
                            }
                        }
                    } catch (Exception ex) {
                        System.Console.WriteLine($"Error finding elements: {ex.Message}");
                    }
                    return jobs;
                    // Wait for the job cards to be visible
                    wait.Until(d => 
                    {
                        try
                        {
                            var jobCards = d.FindElements(By.CssSelector("div.tNxQIb"));
                            return jobCards.Count > 0;
                        }
                        catch
                        {
                            return false;
                        }
                    });

                    // Click on "100+ more jobs" span
                    try {
                        var moreJobsSpan = wait.Until(d => d.FindElement(By.CssSelector("span.LGwnxb")));
                        if (moreJobsSpan != null && moreJobsSpan.Displayed) {
                            System.Console.WriteLine("Found '100+ more jobs' span, clicking...");
                            // Scroll into view
                            ((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].scrollIntoView(true);", moreJobsSpan);
                            await Task.Delay(1000);
                            
                            // Try to click
                            try {
                                moreJobsSpan.Click();
                            } catch {
                                // If regular click fails, try JavaScript click
                                ((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].click();", moreJobsSpan);
                            }
                            System.Console.WriteLine("Clicked '100+ more jobs' successfully");
                            await Task.Delay(2000); // Wait for more jobs to load
                        }
                    } catch (Exception ex) {
                        System.Console.WriteLine($"Error clicking '100+ more jobs': {ex.Message}");
                    }

                    // Get all job cards
                    var jobCards = driver.FindElements(By.CssSelector("div.tNxQIb")).ToList();
                    System.Console.WriteLine($"Found {jobCards.Count} job listings.");

                    if (jobCards.Count == 0)
                    {
                        System.Console.WriteLine("No job listings found.");
                        return jobs;
                    }

                    bool hasMoreJobs = true;
                    int processedJobs = 0;

                    while (hasMoreJobs)
                    {
                        try
                        {
                            // Get current job cards
                            jobCards = driver.FindElements(By.CssSelector("div.tNxQIb")).ToList();
                            if (jobCards.Count == 0)
                            {
                                System.Console.WriteLine("No more job listings found.");
                                break;
                            }

                            // Click on the current job
                            var currentJob = jobCards.First();
                            System.Console.WriteLine($"\nProcessing job {processedJobs + 1} of {jobCards.Count}");
                            System.Console.WriteLine($"Job title: {currentJob.Text}");
                            
                            // Scroll the current job into view and wait for any overlays to disappear
                            ((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].scrollIntoView(true);", currentJob);
                            await Task.Delay(1000);

                            // Try to remove any overlays that might be intercepting the click
                            try {
                                var overlays = driver.FindElements(By.CssSelector("div.sjVJQd"));
                                foreach (var overlay in overlays) {
                                    ((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].remove();", overlay);
                                }
                            } catch {
                                // Ignore if no overlays found
                            }

                            // Wait for the job to be clickable
                            wait.Until(d => 
                            {
                                try
                                {
                                    return currentJob.Displayed && currentJob.Enabled;
                                }
                                catch
                                {
                                    return false;
                                }
                            });

                            // Try clicking with JavaScript if regular click fails
                            try {
                                currentJob.Click();
                            } catch {
                                ((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].click();", currentJob);
                            }
                            
                            System.Console.WriteLine("Waiting for job details to load...");
                            await Task.Delay(2000);

                            // Extract job information
                            System.Console.WriteLine("Extracting job information...");
                            
                            var title = GetElementTextWithRetry(driver, wait, By.CssSelector("div.tNxQIb"));
                            var company = GetElementTextWithRetry(driver, wait, By.CssSelector("div.wHYlTd.MKCbgd.a3jPc"));
                            var jobLocation = GetElementTextWithRetry(driver, wait, By.CssSelector("div.waQ7qe.cS4Vcb-pGL6qe-ysgGef"));
                            var description = GetElementTextWithRetry(driver, wait, By.CssSelector("span.hkXmid"));
                            
                            string url = string.Empty;
                            try {
                                var applyButton = wait.Until(d => d.FindElement(By.CssSelector("div.nNzjpf-cS4Vcb-PvZLI-Ueh9jd-MJoBVe-bF1uUb")));
                                if (applyButton != null)
                                {
                                    var parentAnchor = applyButton.FindElement(By.XPath("./ancestor::a"));
                                    if (parentAnchor != null)
                                    {
                                        url = parentAnchor.GetAttribute("href");
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                System.Console.WriteLine($"Error getting apply URL: {ex.Message}");
                            }

                            if (!string.IsNullOrEmpty(title) && !string.IsNullOrEmpty(company))
                            {
                                var jobPost = new JobPost
                                {
                                    Title = title,
                                    Company = company,
                                    Location = jobLocation,
                                    Description = description,
                                    Url = url,
                                    PostedDate = DateTime.Now,
                                    SeoOptimizedContent = $"{title} - {company} - {jobLocation}. {description}"
                                };

                                jobs.Add(jobPost);

                                System.Console.WriteLine("\n=== JOB DETAILS ===");
                                System.Console.WriteLine("------------------");
                                System.Console.WriteLine($"Title: {title}");
                                System.Console.WriteLine($"Company: {company}");
                                System.Console.WriteLine($"Location: {jobLocation}");
                                System.Console.WriteLine($"URL: {url}");
                                System.Console.WriteLine("\nDescription:");
                                System.Console.WriteLine("------------------");
                                System.Console.WriteLine(description);
                                System.Console.WriteLine("------------------");
                                System.Console.WriteLine("=== END OF JOB DETAILS ===\n");

                                // Comment out WordPress posting for testing
                                /*
                                // Post to WordPress
                                System.Console.WriteLine("Posting to WordPress...");
                                try
                                {
                                    var success = await _wordPressService.PostJobAsync(jobPost);
                                    if (success)
                                    {
                                        System.Console.WriteLine("Successfully posted to WordPress!");
                                        processedJobs++;
                                    }
                                    else
                                    {
                                        System.Console.WriteLine("Failed to post to WordPress.");
                                    }
                                }
                                catch (Exception ex)
                                {
                                    System.Console.WriteLine($"Error posting to WordPress: {ex.Message}");
                                }
                                */
                                processedJobs++; // Still increment counter for testing
                            }

                            // Try to find and click the next job
                            System.Console.WriteLine("Looking for next job...");
                            try
                            {
                                // Get all job cards in the list
                                var allJobCards = driver.FindElements(By.CssSelector("div.tNxQIb")).ToList();
                                if (allJobCards.Count > 0)
                                {
                                    // Find the index of the current job
                                    var currentJobIndex = allJobCards.IndexOf(currentJob);
                                    if (currentJobIndex >= 0 && currentJobIndex < allJobCards.Count - 1)
                                    {
                                        // Get the next job in the list
                                        var nextJob = allJobCards[currentJobIndex + 1];
                                        System.Console.WriteLine($"Found next job at index {currentJobIndex + 1}, clicking...");
                                        
                                        // Scroll the next job into view
                                        ((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].scrollIntoView(true);", nextJob);
                                        await Task.Delay(1000);

                                        // Try to remove any overlays that might be intercepting the click
                                        try {
                                            var overlays = driver.FindElements(By.CssSelector("div.sjVJQd"));
                                            foreach (var overlay in overlays) {
                                                ((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].remove();", overlay);
                                            }
                                        } catch {
                                            // Ignore if no overlays found
                                        }

                                        // Wait for the job to be clickable
                                        wait.Until(d => 
                                        {
                                            try
                                            {
                                                return nextJob.Displayed && nextJob.Enabled;
                                            }
                                            catch
                                            {
                                                return false;
                                            }
                                        });

                                        // Try clicking with JavaScript if regular click fails
                                        try {
                                            nextJob.Click();
                                        } catch {
                                            ((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].click();", nextJob);
                                        }
                                        
                                        await Task.Delay(2000);
                                        System.Console.WriteLine("Clicked next job, continuing with new job...");
                                    }
                                    else
                                    {
                                        System.Console.WriteLine("No more jobs in the list.");
                                        hasMoreJobs = false;
                                    }
                                }
                                else
                                {
                                    System.Console.WriteLine("No job cards found.");
                                    hasMoreJobs = false;
                                }
                            }
                            catch (Exception ex)
                            {
                                System.Console.WriteLine($"Error finding next job: {ex.Message}");
                                hasMoreJobs = false;
                            }
                        }
                        catch (Exception ex)
                        {
                            System.Console.WriteLine($"Error processing job: {ex.Message}");
                            hasMoreJobs = false;
                        }
                    }

                    System.Console.WriteLine($"\nProcessing complete. Total jobs processed: {processedJobs}");
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