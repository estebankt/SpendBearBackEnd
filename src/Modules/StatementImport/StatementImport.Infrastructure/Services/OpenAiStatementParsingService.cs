using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OpenAI.Chat;
using SpendBear.SharedKernel;
using StatementImport.Application.Abstractions;

namespace StatementImport.Infrastructure.Services;

public class OpenAiStatementParsingService : IStatementParsingService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<OpenAiStatementParsingService> _logger;

    public OpenAiStatementParsingService(
        IConfiguration configuration,
        ILogger<OpenAiStatementParsingService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<Result<List<RawParsedTransaction>>> ParseStatementTextAsync(
        string statementText,
        List<CategoryInfo> availableCategories,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var apiKey = _configuration["OpenAI:ApiKey"];
            if (string.IsNullOrWhiteSpace(apiKey))
                return Result.Failure<List<RawParsedTransaction>>(
                    new Error("OpenAI.NoApiKey", "OpenAI API key is not configured."));

            var model = _configuration["OpenAI:Model"] ?? "gpt-4o-mini";

            var categoriesText = string.Join("\n", availableCategories.Select(c =>
                $"- {c.Name}" + (string.IsNullOrWhiteSpace(c.Description) ? "" : $" ({c.Description})")));

            var systemPrompt = $"""
                You are a financial transaction parser. You will receive the text content of a credit card statement. Extract all individual transactions and categorize each one.

                For each transaction, return:
                - date: the transaction date in ISO 8601 format (YYYY-MM-DD)
                - description: a clean, concise description of the transaction
                - amount: the transaction amount as a positive decimal number
                - currency: the currency code (default USD if not specified)
                - suggestedCategoryName: one of the available categories listed below
                - originalText: the original line(s) from the statement

                Available categories:
                {categoriesText}

                Return your response as a JSON object with a "transactions" array. If you cannot determine a category, use "Miscellaneous".
                Only include purchases/charges. Do NOT include payments, credits, balance transfers, or fees.
                """;

            var userPrompt = $"""
                Here is the credit card statement text:

                ---
                {statementText}
                ---

                Parse all transactions and return them as JSON.
                """;

            var client = new ChatClient(model, apiKey);

            var options = new ChatCompletionOptions
            {
                ResponseFormat = ChatResponseFormat.CreateJsonObjectFormat()
            };

            var messages = new List<ChatMessage>
            {
                new SystemChatMessage(systemPrompt),
                new UserChatMessage(userPrompt)
            };

            var response = await client.CompleteChatAsync(messages, options, cancellationToken);

            var content = response.Value.Content[0].Text;
            _logger.LogDebug("OpenAI response received with {Length} characters", content.Length);

            var parsed = JsonSerializer.Deserialize<OpenAiResponse>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (parsed?.Transactions == null || parsed.Transactions.Count == 0)
                return Result.Failure<List<RawParsedTransaction>>(
                    new Error("OpenAI.NoTransactions", "AI could not extract any transactions from the statement."));

            var result = parsed.Transactions.Select(t => new RawParsedTransaction(
                DateTime.SpecifyKind(t.Date, DateTimeKind.Utc),
                t.Description,
                t.Amount,
                t.Currency ?? "USD",
                t.SuggestedCategoryName ?? "Miscellaneous",
                t.OriginalText
            )).ToList();

            return Result.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse statement with OpenAI");
            return Result.Failure<List<RawParsedTransaction>>(
                new Error("OpenAI.Error", $"Failed to parse statement: {ex.Message}"));
        }
    }

    private sealed class OpenAiResponse
    {
        public List<OpenAiTransaction> Transactions { get; set; } = new();
    }

    private sealed class OpenAiTransaction
    {
        public DateTime Date { get; set; }
        public string Description { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string? Currency { get; set; }
        public string? SuggestedCategoryName { get; set; }
        public string? OriginalText { get; set; }
    }
}
