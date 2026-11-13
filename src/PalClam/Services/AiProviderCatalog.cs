// SPDX-FileCopyrightText: 2026 Oscar de la Cuesta Campillo
// SPDX-License-Identifier: GPL-3.0-or-later
// This file is part of PalClam; see LICENSE-GPL-3.0.txt.
namespace PalClam.Services;

/// <summary>Catálogo declarativo: no implica ningún acceso a la red al abrir la aplicación.</summary>
internal sealed record AiProvider(
    string Id, string DisplayName, string BaseUrl, string Protocol,
    bool IsLocal, string Documentation);

internal static class AiProviderCatalog
{
    public static readonly IReadOnlyList<AiProvider> All = new AiProvider[]
    {
        new("ollama", "Ollama · local", "http://127.0.0.1:11434/api", "ollama", true, "https://docs.ollama.com/api"),
        new("lmstudio", "LM Studio · local", "http://127.0.0.1:1234/v1", "chat", true, "https://lmstudio.ai/docs/developer/openai-compat"),
        new("openai", "OpenAI API", "https://api.openai.com/v1", "chat", false, "https://platform.openai.com/docs"),
        new("openrouter", "OpenRouter", "https://openrouter.ai/api/v1", "chat", false, "https://openrouter.ai/docs"),
        new("gemini", "Google Gemini", "https://generativelanguage.googleapis.com/v1beta/openai", "chat", false, "https://ai.google.dev/gemini-api/docs/openai"),
        new("xai", "xAI · Grok", "https://api.x.ai/v1", "responses", false, "https://docs.x.ai/developers"),
        new("anthropic", "Anthropic · Claude", "https://api.anthropic.com/v1", "anthropic", false, "https://docs.anthropic.com"),
        new("groq", "Groq Cloud", "https://api.groq.com/openai/v1", "chat", false, "https://console.groq.com/docs"),
        new("deepseek", "DeepSeek", "https://api.deepseek.com", "chat", false, "https://api-docs.deepseek.com"),
        new("mistral", "Mistral AI", "https://api.mistral.ai/v1", "chat", false, "https://docs.mistral.ai"),
        new("perplexity", "Perplexity · Sonar", "https://api.perplexity.ai", "chat", false, "https://docs.perplexity.ai"),
        new("together", "Together AI", "https://api.together.xyz/v1", "chat", false, "https://docs.together.ai"),
        new("compatible", "API compatible · personalizada", "", "chat", false, "https://platform.openai.com/docs/api-reference")
    };

    public static AiProvider Find(string? id) =>
        All.FirstOrDefault(p => p.Id == id) ?? All[0];

    public static bool RequiresKey(AiProvider p) => !p.IsLocal;
}
