# Job Automation Tool

This .NET Core application automates the process of scraping job listings from Indeed Pakistan, optimizing them for SEO using ChatGPT, and posting them to a WordPress site.

## Features

- Scrapes job listings from Indeed Pakistan
- Optimizes job descriptions for SEO using ChatGPT
- Posts optimized content to WordPress
- Configurable search terms and locations
- Rate limiting to be respectful to APIs

## Prerequisites

- .NET 8.0 SDK or later
- OpenAI API key
- WordPress site with REST API enabled
- WordPress application password

## Setup

1. Clone the repository
2. Update the `appsettings.json` file with your credentials:
   ```json
   {
     "OpenAI": {
       "ApiKey": "your-openai-api-key",
       "Model": "gpt-3.5-turbo"
     },
     "WordPress": {
       "Url": "your-wordpress-site-url",
       "Username": "your-wordpress-username",
       "Password": "your-wordpress-application-password"
     },
     "Indeed": {
       "BaseUrl": "https://pk.indeed.com",
       "SearchUrl": "https://pk.indeed.com/jobs",
       "SearchQuery": "?q={0}&l={1}",
       "MaxPages": 5
     }
   }
   ```

3. Build and run the application:
   ```bash
   dotnet build
   dotnet run
   ```

## Configuration

- `MaxPages`: Number of pages to scrape from Indeed (default: 5)
- `SearchQuery`: Customize the search query format
- WordPress category ID: Update the default category ID in `WordPressService.cs`

## Usage

The application will:
1. Scrape jobs from Indeed Pakistan
2. Process each job through ChatGPT for SEO optimization
3. Post the optimized content to WordPress
4. Wait between requests to respect API rate limits

## Error Handling

The application includes error handling for:
- Network issues
- API rate limits
- Invalid responses
- WordPress posting failures

## Contributing

Feel free to submit issues and enhancement requests! # wordpress_autoposter
