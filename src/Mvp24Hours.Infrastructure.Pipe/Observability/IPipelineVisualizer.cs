//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================

using Mvp24Hours.Core.Contract.Infrastructure.Pipe;
using System.Collections.Generic;

namespace Mvp24Hours.Infrastructure.Pipe.Observability
{
    /// <summary>
    /// Provides functionality to visualize and export pipeline flow diagrams.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This interface supports multiple output formats:
    /// <list type="bullet">
    /// <item>Mermaid - Markdown-compatible flowchart syntax</item>
    /// <item>DOT/Graphviz - Standard graph description language</item>
    /// <item>ASCII - Plain text visualization</item>
    /// <item>JSON - Structured data for custom rendering</item>
    /// </list>
    /// </para>
    /// </remarks>
    public interface IPipelineVisualizer
    {
        /// <summary>
        /// Generates a Mermaid flowchart diagram from a pipeline.
        /// </summary>
        /// <param name="pipeline">The pipeline to visualize.</param>
        /// <param name="options">Optional visualization options.</param>
        /// <returns>A Mermaid-compatible diagram string.</returns>
        string ToMermaid(IPipelineAsync pipeline, PipelineVisualizationOptions? options = null);

        /// <summary>
        /// Generates a Mermaid flowchart diagram from a sync pipeline.
        /// </summary>
        /// <param name="pipeline">The pipeline to visualize.</param>
        /// <param name="options">Optional visualization options.</param>
        /// <returns>A Mermaid-compatible diagram string.</returns>
        string ToMermaid(IPipeline pipeline, PipelineVisualizationOptions? options = null);

        /// <summary>
        /// Generates a DOT/Graphviz diagram from a pipeline.
        /// </summary>
        /// <param name="pipeline">The pipeline to visualize.</param>
        /// <param name="options">Optional visualization options.</param>
        /// <returns>A DOT-compatible diagram string.</returns>
        string ToDot(IPipelineAsync pipeline, PipelineVisualizationOptions? options = null);

        /// <summary>
        /// Generates a DOT/Graphviz diagram from a sync pipeline.
        /// </summary>
        /// <param name="pipeline">The pipeline to visualize.</param>
        /// <param name="options">Optional visualization options.</param>
        /// <returns>A DOT-compatible diagram string.</returns>
        string ToDot(IPipeline pipeline, PipelineVisualizationOptions? options = null);

        /// <summary>
        /// Generates an ASCII representation of the pipeline flow.
        /// </summary>
        /// <param name="pipeline">The pipeline to visualize.</param>
        /// <param name="options">Optional visualization options.</param>
        /// <returns>An ASCII diagram string.</returns>
        string ToAscii(IPipelineAsync pipeline, PipelineVisualizationOptions? options = null);

        /// <summary>
        /// Generates an ASCII representation of the sync pipeline flow.
        /// </summary>
        /// <param name="pipeline">The pipeline to visualize.</param>
        /// <param name="options">Optional visualization options.</param>
        /// <returns>An ASCII diagram string.</returns>
        string ToAscii(IPipeline pipeline, PipelineVisualizationOptions? options = null);

        /// <summary>
        /// Generates a JSON representation of the pipeline structure.
        /// </summary>
        /// <param name="pipeline">The pipeline to visualize.</param>
        /// <param name="options">Optional visualization options.</param>
        /// <returns>A JSON string representing the pipeline structure.</returns>
        string ToJson(IPipelineAsync pipeline, PipelineVisualizationOptions? options = null);

        /// <summary>
        /// Generates a JSON representation of the sync pipeline structure.
        /// </summary>
        /// <param name="pipeline">The pipeline to visualize.</param>
        /// <param name="options">Optional visualization options.</param>
        /// <returns>A JSON string representing the pipeline structure.</returns>
        string ToJson(IPipeline pipeline, PipelineVisualizationOptions? options = null);

        /// <summary>
        /// Gets the pipeline structure as a data object.
        /// </summary>
        /// <param name="pipeline">The pipeline to analyze.</param>
        /// <returns>A structured representation of the pipeline.</returns>
        PipelineStructure GetStructure(IPipelineAsync pipeline);

        /// <summary>
        /// Gets the sync pipeline structure as a data object.
        /// </summary>
        /// <param name="pipeline">The pipeline to analyze.</param>
        /// <returns>A structured representation of the pipeline.</returns>
        PipelineStructure GetStructure(IPipeline pipeline);
    }

    /// <summary>
    /// Options for pipeline visualization.
    /// </summary>
    public class PipelineVisualizationOptions
    {
        /// <summary>
        /// Gets or sets the diagram title (default: "Pipeline Flow").
        /// </summary>
        public string Title { get; set; } = "Pipeline Flow";

        /// <summary>
        /// Gets or sets whether to include interceptors in the visualization (default: true).
        /// </summary>
        public bool IncludeInterceptors { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to show operation types (default: true).
        /// </summary>
        public bool ShowOperationTypes { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to use short class names instead of full names (default: true).
        /// </summary>
        public bool UseShortNames { get; set; } = true;

        /// <summary>
        /// Gets or sets the diagram direction for Mermaid (default: TB - top to bottom).
        /// </summary>
        public DiagramDirection Direction { get; set; } = DiagramDirection.TopToBottom;

        /// <summary>
        /// Gets or sets whether to highlight required operations (default: true).
        /// </summary>
        public bool HighlightRequiredOperations { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to include metrics in the visualization (default: false).
        /// </summary>
        public bool IncludeMetrics { get; set; } = false;

        /// <summary>
        /// Gets or sets the metrics provider for including metrics data.
        /// </summary>
        public IPipelineMetrics? MetricsProvider { get; set; }
    }

    /// <summary>
    /// Diagram flow direction.
    /// </summary>
    public enum DiagramDirection
    {
        /// <summary>
        /// Top to bottom.
        /// </summary>
        TopToBottom,

        /// <summary>
        /// Bottom to top.
        /// </summary>
        BottomToTop,

        /// <summary>
        /// Left to right.
        /// </summary>
        LeftToRight,

        /// <summary>
        /// Right to left.
        /// </summary>
        RightToLeft
    }

    /// <summary>
    /// Represents the structure of a pipeline for visualization.
    /// </summary>
    public class PipelineStructure
    {
        /// <summary>
        /// Gets or sets the pipeline name.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the pipeline type.
        /// </summary>
        public string Type { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets whether the pipeline breaks on fail.
        /// </summary>
        public bool IsBreakOnFail { get; set; }

        /// <summary>
        /// Gets or sets whether the pipeline forces rollback on failure.
        /// </summary>
        public bool ForceRollbackOnFailure { get; set; }

        /// <summary>
        /// Gets or sets the operations in the pipeline.
        /// </summary>
        public List<OperationNode> Operations { get; set; } = new();

        /// <summary>
        /// Gets or sets the interceptors.
        /// </summary>
        public List<InterceptorGroup> Interceptors { get; set; } = new();
    }

    /// <summary>
    /// Represents an operation node in the pipeline structure.
    /// </summary>
    public class OperationNode
    {
        /// <summary>
        /// Gets or sets the operation ID.
        /// </summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the operation name.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the operation type name.
        /// </summary>
        public string TypeName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets whether the operation is required.
        /// </summary>
        public bool IsRequired { get; set; }

        /// <summary>
        /// Gets or sets the operation index in the pipeline.
        /// </summary>
        public int Index { get; set; }

        /// <summary>
        /// Gets or sets the operation category.
        /// </summary>
        public OperationCategory Category { get; set; }

        /// <summary>
        /// Gets or sets the operation metrics (if available).
        /// </summary>
        public OperationMetrics? Metrics { get; set; }
    }

    /// <summary>
    /// Operation categories for visualization styling.
    /// </summary>
    public enum OperationCategory
    {
        /// <summary>
        /// Standard operation.
        /// </summary>
        Standard,

        /// <summary>
        /// Validation operation.
        /// </summary>
        Validation,

        /// <summary>
        /// Conditional/branch operation.
        /// </summary>
        Conditional,

        /// <summary>
        /// Parallel operation group.
        /// </summary>
        Parallel,

        /// <summary>
        /// Sub-pipeline operation.
        /// </summary>
        SubPipeline,

        /// <summary>
        /// Action-based operation.
        /// </summary>
        Action
    }

    /// <summary>
    /// Represents a group of interceptors.
    /// </summary>
    public class InterceptorGroup
    {
        /// <summary>
        /// Gets or sets the interceptor type.
        /// </summary>
        public string InterceptorType { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the operations in this interceptor group.
        /// </summary>
        public List<OperationNode> Operations { get; set; } = new();
    }
}

