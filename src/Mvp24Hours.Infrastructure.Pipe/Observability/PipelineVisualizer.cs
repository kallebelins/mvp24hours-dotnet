//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================

using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Mvp24Hours.Core.Contract.Infrastructure.Pipe;
using Mvp24Hours.Core.Enums.Infrastructure;

namespace Mvp24Hours.Infrastructure.Pipe.Observability;

/// <summary>
/// Default implementation of <see cref="IPipelineVisualizer"/> providing
/// multiple output formats for pipeline visualization.
/// </summary>
public class PipelineVisualizer : IPipelineVisualizer
{
    #region [ Mermaid ]

    /// <inheritdoc />
    public string ToMermaid(IPipelineAsync pipeline, PipelineVisualizationOptions? options = null)
    {
        PipelineStructure structure = GetStructure(pipeline);
        return GenerateMermaid(structure, options ?? new PipelineVisualizationOptions());
    }

    /// <inheritdoc />
    public string ToMermaid(IPipeline pipeline, PipelineVisualizationOptions? options = null)
    {
        PipelineStructure structure = GetStructure(pipeline);
        return GenerateMermaid(structure, options ?? new PipelineVisualizationOptions());
    }

    private static string GenerateMermaid(PipelineStructure structure, PipelineVisualizationOptions options)
    {
        var sb = new StringBuilder();
        string direction = options.Direction switch
        {
            DiagramDirection.TopToBottom => "TB",
            DiagramDirection.BottomToTop => "BT",
            DiagramDirection.LeftToRight => "LR",
            DiagramDirection.RightToLeft => "RL",
            _ => "TB"
        };

        sb.AppendLine($"flowchart {direction}");
        sb.AppendLine($"    %% {options.Title}");
        sb.AppendLine();

        // Add start node
        sb.AppendLine("    Start([Start])");

        // Add operation nodes
        for (int i = 0; i < structure.Operations.Count; i++)
        {
            OperationNode op = structure.Operations[i];
            (string, string) nodeShape = GetMermaidNodeShape(op, options);
            string nodeStyle = options.HighlightRequiredOperations && op.IsRequired ? ":::required" : "";

            string label = options.UseShortNames ? op.Name : op.TypeName;
            if (options.ShowOperationTypes && op.Category != OperationCategory.Standard)
            {
                label += $"<br/><small>{op.Category}</small>";
            }

            if (options.IncludeMetrics && op.Metrics != null)
            {
                label += $"<br/><small>Avg: {op.Metrics.AverageDurationMs:F1}ms</small>";
            }

            sb.AppendLine($"    {op.Id}{nodeShape.Item1}\"{label}\"{nodeShape.Item2}{nodeStyle}");
        }

        // Add end node
        sb.AppendLine("    End([End])");
        sb.AppendLine();

        // Add connections
        if (structure.Operations.Count > 0)
        {
            sb.AppendLine($"    Start --> {structure.Operations[0].Id}");

            for (int i = 0; i < structure.Operations.Count - 1; i++)
            {
                sb.AppendLine($"    {structure.Operations[i].Id} --> {structure.Operations[i + 1].Id}");
            }

            sb.AppendLine($"    {structure.Operations[^1].Id} --> End");
        }
        else
        {
            sb.AppendLine("    Start --> End");
        }

        // Add interceptors if requested
        if (options.IncludeInterceptors && structure.Interceptors.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("    %% Interceptors");

            foreach (InterceptorGroup group in structure.Interceptors)
            {
                sb.AppendLine($"    subgraph {group.InterceptorType}Interceptors[\"{group.InterceptorType} Interceptors\"]");
                foreach (OperationNode op in group.Operations)
                {
                    sb.AppendLine($"        {op.Id}[[\"{op.Name}\"]]");
                }
                sb.AppendLine("    end");
            }
        }

        // Add styles
        sb.AppendLine();
        sb.AppendLine("    %% Styles");
        sb.AppendLine("    classDef required fill:#ff9999,stroke:#cc0000,stroke-width:2px");
        sb.AppendLine("    classDef validation fill:#99ff99,stroke:#00cc00");
        sb.AppendLine("    classDef conditional fill:#ffff99,stroke:#cccc00");
        sb.AppendLine("    classDef parallel fill:#99ccff,stroke:#0066cc");

        return sb.ToString();
    }

    private static (string, string) GetMermaidNodeShape(OperationNode op, PipelineVisualizationOptions options)
    {
        return op.Category switch
        {
            OperationCategory.Validation => ("{", "}"),
            OperationCategory.Conditional => ("{{", "}}"),
            OperationCategory.Parallel => ("[[", "]]"),
            OperationCategory.SubPipeline => ("[[", "]]"),
            _ => ("[", "]")
        };
    }

    #endregion

    #region [ DOT/Graphviz ]

    /// <inheritdoc />
    public string ToDot(IPipelineAsync pipeline, PipelineVisualizationOptions? options = null)
    {
        PipelineStructure structure = GetStructure(pipeline);
        return GenerateDot(structure, options ?? new PipelineVisualizationOptions());
    }

    /// <inheritdoc />
    public string ToDot(IPipeline pipeline, PipelineVisualizationOptions? options = null)
    {
        PipelineStructure structure = GetStructure(pipeline);
        return GenerateDot(structure, options ?? new PipelineVisualizationOptions());
    }

    private static string GenerateDot(PipelineStructure structure, PipelineVisualizationOptions options)
    {
        var sb = new StringBuilder();
        string rankdir = options.Direction switch
        {
            DiagramDirection.TopToBottom => "TB",
            DiagramDirection.BottomToTop => "BT",
            DiagramDirection.LeftToRight => "LR",
            DiagramDirection.RightToLeft => "RL",
            _ => "TB"
        };

        sb.AppendLine($"digraph \"{options.Title}\" {{");
        sb.AppendLine($"    rankdir={rankdir};");
        sb.AppendLine("    node [fontname=\"Helvetica\"];");
        sb.AppendLine("    edge [fontname=\"Helvetica\"];");
        sb.AppendLine();

        // Start node
        sb.AppendLine("    start [shape=circle, label=\"\", style=filled, fillcolor=black, width=0.3];");

        // Operation nodes
        foreach (OperationNode op in structure.Operations)
        {
            string shape = GetDotNodeShape(op);
            string color = GetDotNodeColor(op, options);
            string label = options.UseShortNames ? op.Name : op.TypeName;

            sb.AppendLine($"    {op.Id} [label=\"{EscapeDotString(label)}\", shape={shape}, style=filled, fillcolor=\"{color}\"];");
        }

        // End node
        sb.AppendLine("    end [shape=doublecircle, label=\"\", style=filled, fillcolor=black, width=0.3];");
        sb.AppendLine();

        // Edges
        if (structure.Operations.Count > 0)
        {
            sb.AppendLine($"    start -> {structure.Operations[0].Id};");

            for (int i = 0; i < structure.Operations.Count - 1; i++)
            {
                sb.AppendLine($"    {structure.Operations[i].Id} -> {structure.Operations[i + 1].Id};");
            }

            sb.AppendLine($"    {structure.Operations[^1].Id} -> end;");
        }
        else
        {
            sb.AppendLine("    start -> end;");
        }

        // Interceptors subgraph
        if (options.IncludeInterceptors && structure.Interceptors.Count > 0)
        {
            sb.AppendLine();
            foreach (InterceptorGroup group in structure.Interceptors)
            {
                sb.AppendLine($"    subgraph cluster_{group.InterceptorType} {{");
                sb.AppendLine($"        label=\"{group.InterceptorType} Interceptors\";");
                sb.AppendLine("        style=dashed;");
                foreach (OperationNode op in group.Operations)
                {
                    sb.AppendLine($"        {op.Id} [label=\"{EscapeDotString(op.Name)}\", shape=box];");
                }
                sb.AppendLine("    }");
            }
        }

        sb.AppendLine("}");

        return sb.ToString();
    }

    private static string GetDotNodeShape(OperationNode op)
    {
        return op.Category switch
        {
            OperationCategory.Validation => "hexagon",
            OperationCategory.Conditional => "diamond",
            OperationCategory.Parallel => "parallelogram",
            OperationCategory.SubPipeline => "box3d",
            _ => "box"
        };
    }

    private static string GetDotNodeColor(OperationNode op, PipelineVisualizationOptions options)
    {
        if (options.HighlightRequiredOperations && op.IsRequired)
        {
            return "#ff9999";
        }

        return op.Category switch
        {
            OperationCategory.Validation => "#99ff99",
            OperationCategory.Conditional => "#ffff99",
            OperationCategory.Parallel => "#99ccff",
            _ => "#ffffff"
        };
    }

    private static string EscapeDotString(string input)
    {
        return input.Replace("\"", "\\\"").Replace("\n", "\\n");
    }

    #endregion

    #region [ ASCII ]

    /// <inheritdoc />
    public string ToAscii(IPipelineAsync pipeline, PipelineVisualizationOptions? options = null)
    {
        PipelineStructure structure = GetStructure(pipeline);
        return GenerateAscii(structure, options ?? new PipelineVisualizationOptions());
    }

    /// <inheritdoc />
    public string ToAscii(IPipeline pipeline, PipelineVisualizationOptions? options = null)
    {
        PipelineStructure structure = GetStructure(pipeline);
        return GenerateAscii(structure, options ?? new PipelineVisualizationOptions());
    }

    private static string GenerateAscii(PipelineStructure structure, PipelineVisualizationOptions options)
    {
        var sb = new StringBuilder();
        int maxNameLength = structure.Operations.Count > 0
            ? structure.Operations.Max(o => (options.UseShortNames ? o.Name : o.TypeName).Length)
            : 10;

        maxNameLength = Math.Max(maxNameLength, 10);

        sb.AppendLine($"╔{"═".PadLeft(maxNameLength + 4, '═')}╗");
        sb.AppendLine($"║  {options.Title.PadRight(maxNameLength)}  ║");
        sb.AppendLine($"╠{"═".PadLeft(maxNameLength + 4, '═')}╣");

        // Start
        sb.AppendLine($"║  {"● START".PadRight(maxNameLength)}  ║");
        sb.AppendLine($"║  {"│".PadRight(maxNameLength)}  ║");

        // Operations
        for (int i = 0; i < structure.Operations.Count; i++)
        {
            OperationNode op = structure.Operations[i];
            string name = options.UseShortNames ? op.Name : op.TypeName;
            string prefix = op.IsRequired && options.HighlightRequiredOperations ? "★ " : "  ";

            sb.AppendLine($"║  ▼{new string(' ', maxNameLength - 1)}  ║");

            (string, string, string, string) border = op.Category switch
            {
                OperationCategory.Validation => ("╭", "╮", "╰", "╯"),
                OperationCategory.Conditional => ("◇", "◇", "◇", "◇"),
                OperationCategory.Parallel => ("╔", "╗", "╚", "╝"),
                _ => ("┌", "┐", "└", "┘")
            };

            sb.AppendLine($"║  {border.Item1}{new string('─', maxNameLength - 2)}{border.Item2}  ║");
            sb.AppendLine($"║  │{prefix}{name.PadRight(maxNameLength - 4)}│  ║");

            if (options.ShowOperationTypes && op.Category != OperationCategory.Standard)
            {
                string categoryStr = $"({op.Category})";
                sb.AppendLine($"║  │{categoryStr.PadRight(maxNameLength - 2)}│  ║");
            }

            if (options.IncludeMetrics && op.Metrics != null)
            {
                string metricsStr = $"Avg: {op.Metrics.AverageDurationMs:F1}ms";
                sb.AppendLine($"║  │{metricsStr.PadRight(maxNameLength - 2)}│  ║");
            }

            sb.AppendLine($"║  {border.Item3}{new string('─', maxNameLength - 2)}{border.Item4}  ║");
            sb.AppendLine($"║  {"│".PadRight(maxNameLength)}  ║");
        }

        // End
        sb.AppendLine($"║  ▼{new string(' ', maxNameLength - 1)}  ║");
        sb.AppendLine($"║  {"◉ END".PadRight(maxNameLength)}  ║");
        sb.AppendLine($"╚{"═".PadLeft(maxNameLength + 4, '═')}╝");

        // Interceptors
        if (options.IncludeInterceptors && structure.Interceptors.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("Interceptors:");
            foreach (InterceptorGroup group in structure.Interceptors)
            {
                sb.AppendLine($"  [{group.InterceptorType}]");
                foreach (OperationNode op in group.Operations)
                {
                    sb.AppendLine($"    - {op.Name}");
                }
            }
        }

        return sb.ToString();
    }

    #endregion

    #region [ JSON ]

    /// <inheritdoc />
    public string ToJson(IPipelineAsync pipeline, PipelineVisualizationOptions? options = null)
    {
        PipelineStructure structure = GetStructure(pipeline);

        if (options?.IncludeMetrics == true && options.MetricsProvider != null)
        {
            EnrichWithMetrics(structure, options.MetricsProvider);
        }

        return JsonSerializer.Serialize(structure, new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Converters = { new JsonStringEnumConverter() }
        });
    }

    /// <inheritdoc />
    public string ToJson(IPipeline pipeline, PipelineVisualizationOptions? options = null)
    {
        PipelineStructure structure = GetStructure(pipeline);

        if (options?.IncludeMetrics == true && options.MetricsProvider != null)
        {
            EnrichWithMetrics(structure, options.MetricsProvider);
        }

        return JsonSerializer.Serialize(structure, new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Converters = { new JsonStringEnumConverter() }
        });
    }

    #endregion

    #region [ Structure Extraction ]

    /// <inheritdoc />
    public PipelineStructure GetStructure(IPipelineAsync pipeline)
    {
        Type pipelineType = pipeline.GetType();
        var structure = new PipelineStructure
        {
            Name = pipelineType.Name,
            Type = pipelineType.FullName ?? pipelineType.Name
        };

        // Try to cast to concrete implementation to access operations
        if (pipeline is PipelineAsync concreteAsync)
        {
            // Get operations
            List<IOperationAsync> operations = concreteAsync.GetOperations();
            for (int i = 0; i < operations.Count; i++)
            {
                IOperationAsync op = operations[i];
                structure.Operations.Add(CreateOperationNode(op, i));
            }

            // Get interceptors
            Dictionary<PipelineInterceptorType, List<IOperationAsync>> interceptors = concreteAsync.GetInterceptors();
            foreach (KeyValuePair<PipelineInterceptorType, List<IOperationAsync>> kvp in interceptors)
            {
                var group = new InterceptorGroup
                {
                    InterceptorType = kvp.Key.ToString()
                };

                for (int i = 0; i < kvp.Value.Count; i++)
                {
                    group.Operations.Add(CreateOperationNode(kvp.Value[i], i, $"int_{kvp.Key}_"));
                }

                structure.Interceptors.Add(group);
            }
        }

        return structure;
    }

    /// <inheritdoc />
    public PipelineStructure GetStructure(IPipeline pipeline)
    {
        Type pipelineType = pipeline.GetType();
        var structure = new PipelineStructure
        {
            Name = pipelineType.Name,
            Type = pipelineType.FullName ?? pipelineType.Name
        };

        // Try to cast to concrete implementation to access operations
        if (pipeline is Pipeline concrete)
        {
            // Get operations
            List<IOperation> operations = concrete.GetOperations();
            for (int i = 0; i < operations.Count; i++)
            {
                IOperation op = operations[i];
                structure.Operations.Add(CreateOperationNode(op, i));
            }

            // Get interceptors
            Dictionary<PipelineInterceptorType, List<IOperation>> interceptors = concrete.GetInterceptors();
            foreach (KeyValuePair<PipelineInterceptorType, List<IOperation>> kvp in interceptors)
            {
                var group = new InterceptorGroup
                {
                    InterceptorType = kvp.Key.ToString()
                };

                for (int i = 0; i < kvp.Value.Count; i++)
                {
                    group.Operations.Add(CreateOperationNode(kvp.Value[i], i, $"int_{kvp.Key}_"));
                }

                structure.Interceptors.Add(group);
            }
        }

        return structure;
    }

    private static OperationNode CreateOperationNode(IOperationAsync op, int index, string idPrefix = "op_")
    {
        Type type = op.GetType();
        return new OperationNode
        {
            Id = $"{idPrefix}{index}",
            Name = GetShortName(type),
            TypeName = type.FullName ?? type.Name,
            IsRequired = op.IsRequired,
            Index = index,
            Category = DetermineCategory(type)
        };
    }

    private static OperationNode CreateOperationNode(IOperation op, int index, string idPrefix = "op_")
    {
        Type type = op.GetType();
        return new OperationNode
        {
            Id = $"{idPrefix}{index}",
            Name = GetShortName(type),
            TypeName = type.FullName ?? type.Name,
            IsRequired = op.IsRequired,
            Index = index,
            Category = DetermineCategory(type)
        };
    }

    private static string GetShortName(Type type)
    {
        string name = type.Name;

        // Remove common suffixes
        string[] suffixes = ["Operation", "Async", "Handler", "Action"];
        foreach (string? suffix in suffixes)
        {
            if (name.EndsWith(suffix) && name.Length > suffix.Length)
            {
                name = name[..^suffix.Length];
            }
        }

        return name;
    }

    private static OperationCategory DetermineCategory(Type type)
    {
        string name = type.Name.ToLowerInvariant();

        if (name.Contains("valid"))
        {
            return OperationCategory.Validation;
        }

        if (name.Contains("condition") || name.Contains("branch"))
        {
            return OperationCategory.Conditional;
        }

        if (name.Contains("parallel"))
        {
            return OperationCategory.Parallel;
        }

        if (name.Contains("subpipeline") || name.Contains("sub_pipeline"))
        {
            return OperationCategory.SubPipeline;
        }

        if (type.Name.Contains("ActionAsync") || type.Name.Contains("OperationAction"))
        {
            return OperationCategory.Action;
        }

        return OperationCategory.Standard;
    }

    private static void EnrichWithMetrics(PipelineStructure structure, IPipelineMetrics metrics)
    {
        foreach (OperationNode op in structure.Operations)
        {
            OperationMetrics? opMetrics = metrics.GetOperationMetrics(op.TypeName);
            opMetrics ??= metrics.GetOperationMetrics(op.Name);
            op.Metrics = opMetrics;
        }
    }

    #endregion
}

