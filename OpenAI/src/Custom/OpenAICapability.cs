using System;
using System.Collections.Generic;

#nullable enable

namespace OpenAI;

/// <summary>
/// OpenAI 模型的服务平台。同一个模型在不同平台上的能力可能不一样，所以能力表按平台分开。
/// </summary>
public enum OpenAIPlatform
{
    /// <summary>OpenAI 官方 API，对应 models.dev 的 <c>openai</c>。</summary>
    OpenAI,

    /// <summary>Azure OpenAI，对应 models.dev <c>azure</c> 里 OpenAI 自家的模型。</summary>
    Azure,
}

/// <summary>模型收发的内容类型，可组合。</summary>
[Flags]
public enum OpenAIModalities
{
    /// <summary>无。</summary>
    None = 0,

    /// <summary>文本。</summary>
    Text = 1 << 0,

    /// <summary>图片。</summary>
    Image = 1 << 1,

    /// <summary>音频。</summary>
    Audio = 1 << 2,

    /// <summary>视频。</summary>
    Video = 1 << 3,

    /// <summary>PDF 文档。</summary>
    Pdf = 1 << 4,
}

/// <summary>
/// 一个 OpenAI 模型在某个平台上的能力：上下文与输入输出上限、收发哪些内容、支持哪些功能。
/// 数据由 OneApi 仓库的 <c>tools/Capabilities/update_capabilities.py</c> 从 models.dev 生成到
/// <c>OpenAICapability.Data.cs</c>，运行时不联网：能力变了就重新生成、发版。
/// </summary>
public sealed partial class OpenAICapability
{
    /// <summary>由生成的能力表调用。</summary>
    public OpenAICapability(
        int contextWindow,
        int maxInputTokens,
        int maxOutputTokens,
        OpenAIModalities input,
        OpenAIModalities output,
        bool toolCall,
        bool structuredOutput,
        bool temperature,
        bool attachment)
    {
        ContextWindow = contextWindow;
        MaxInputTokens = maxInputTokens;
        MaxOutputTokens = maxOutputTokens;
        Input = input;
        Output = output;
        ToolCall = toolCall;
        StructuredOutput = structuredOutput;
        Temperature = temperature;
        Attachment = attachment;
    }

    /// <summary>上下文窗口（models.dev <c>limit.context</c>）。</summary>
    public int ContextWindow { get; }

    /// <summary>输入上限。models.dev 标了 <c>limit.input</c> 的用它，否则等于上下文窗口。</summary>
    public int MaxInputTokens { get; }

    /// <summary>输出上限，<c>max_completion_tokens</c> / <c>max_output_tokens</c> 能填的最大值。</summary>
    public int MaxOutputTokens { get; }

    /// <summary>能收的内容类型。</summary>
    public OpenAIModalities Input { get; }

    /// <summary>能产出的内容类型。</summary>
    public OpenAIModalities Output { get; }

    /// <summary>支持工具调用。models.dev 没标的当不支持，下同。</summary>
    public bool ToolCall { get; }

    /// <summary>支持结构化输出（按 JSON Schema 约束回复）。</summary>
    public bool StructuredOutput { get; }

    /// <summary>接受 <c>temperature</c> 参数。</summary>
    public bool Temperature { get; }

    /// <summary>接受文件附件。</summary>
    public bool Attachment { get; }

    /// <summary>
    /// 查一个模型在某个平台上的能力。查不到返回 <c>null</c>——表里没有就是不知道，
    /// 不拿别的模型或默认值顶替。id 不分大小写。
    /// </summary>
    public static OpenAICapability? Find(OpenAIPlatform platform, string? modelId)
    {
        if (modelId is null || modelId.Length == 0)
            return null;

        return TableFor(platform) is { } table && table.TryGetValue(modelId, out var capability)
            ? capability
            : null;
    }

    private static Dictionary<string, OpenAICapability>? TableFor(OpenAIPlatform platform) =>
        platform switch
        {
            OpenAIPlatform.OpenAI => OpenAITable,
            OpenAIPlatform.Azure => AzureTable,
            _ => null,
        };
}
