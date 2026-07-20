using Mvp24Hours.Infrastructure.Pipe.AdvancedFlow.DependencyGraph;
using Mvp24Hours.Infrastructure.Pipe.Typed;

namespace Mvp24Hours.Application.Pipe.Test.AdvancedFlow.DependencyGraph;

[Trait("Category", "Unit")]
public class DependencyGraphTest
{
    [Fact]
    public async Task DependencyGraphExecutor_Should_ExecuteNodesInDependencyOrder()
    {
        var graph = new DependencyGraph<List<string>>();
        graph.AddNode(new LambdaDependencyGraphNode<List<string>>("validate", (ctx, _) =>
        {
            ctx.Add("validate");
            return OperationResult<object>.Success("ok");
        }));
        graph.AddNode(new LambdaDependencyGraphNode<List<string>>("process", (ctx, results) =>
        {
            ctx.Add("process");
            results.TryGetValue("validate", out object? dep);
            return OperationResult<object>.Success(dep!);
        }).DependsOn("validate"));

        var executor = new DependencyGraphExecutor<List<string>>(graph);
        var context = new List<string>();

        DependencyGraphResult<List<string>> result = await executor.ExecuteAsync(context);

        result.IsSuccess.Should().BeTrue();
        result.CompletedNodes.Should().BeEquivalentTo(["validate", "process"]);
        context.Should().Equal("validate", "process");
    }

    [Fact]
    public void DependencyGraphExecutor_Should_DetectCircularDependencies()
    {
        var graph = new DependencyGraph<string>();
        graph.AddNode(new LambdaDependencyGraphNode<string>("a", (_, _) => OperationResult<object>.Success(new object())).DependsOn("b"));
        graph.AddNode(new LambdaDependencyGraphNode<string>("b", (_, _) => OperationResult<object>.Success(new object())).DependsOn("a"));

        Action act = () => _ = new DependencyGraphExecutor<string>(graph);

        act.Should().Throw<InvalidOperationException>().WithMessage("*circular*");
    }

    [Fact]
    public async Task DependencyGraphExecutor_Should_SkipNodesWhenDependencyFails()
    {
        var graph = new DependencyGraph<string>();
        graph.AddNode(new LambdaDependencyGraphNode<string>("root", (_, _) => OperationResult<object>.Failure("root failed")));
        graph.AddNode(new LambdaDependencyGraphNode<string>("child", (_, _) => OperationResult<object>.Success(new object())).DependsOn("root"));

        var executor = new DependencyGraphExecutor<string>(graph, new DependencyGraphOptions { StopOnFirstFailure = true });
        DependencyGraphResult<string> result = await executor.ExecuteAsync("ctx");

        result.IsSuccess.Should().BeFalse();
        result.FailedNodes.Should().Contain("root");
        result.SkippedNodes.Should().Contain("child");
    }

    [Fact]
    public void DependencyGraphNode_Should_AddDependencies()
    {
        var node = new LambdaDependencyGraphNode<string>("node", (_, _) => OperationResult<object>.Success(new object()))
            .DependsOn("dep1", "dep2");

        node.Dependencies.Should().BeEquivalentTo(["dep1", "dep2"]);
        node.Id.Should().Be("node");
    }

    [Fact]
    public void DependencyGraph_Should_RejectDuplicateNodeIds()
    {
        var graph = new DependencyGraph<string>();
        graph.AddNode(new LambdaDependencyGraphNode<string>("same", (_, _) => OperationResult<object>.Success(new object())));

        Action act = () => graph.AddNode(new LambdaDependencyGraphNode<string>("same", (_, _) => OperationResult<object>.Success(new object())));

        act.Should().Throw<ArgumentException>().WithMessage("*already exists*");
    }

    [Fact]
    public void DependencyGraph_Should_ReturnTopologicalOrder()
    {
        var graph = new DependencyGraph<string>();
        graph.AddNode(new LambdaDependencyGraphNode<string>("c", (_, _) => OperationResult<object>.Success(new object()), priority: 1).DependsOn("b"));
        graph.AddNode(new LambdaDependencyGraphNode<string>("b", (_, _) => OperationResult<object>.Success(new object()), priority: 2).DependsOn("a"));
        graph.AddNode(new LambdaDependencyGraphNode<string>("a", (_, _) => OperationResult<object>.Success(new object()), priority: 3));

        var order = graph.GetTopologicalOrder().Select(n => n.Id).ToList();

        order.Should().Equal("a", "b", "c");
    }
}
