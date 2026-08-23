using Moq;
using RPA.Automation.Services;
using RPA.Core.Models;
using RPA.Core.Services;
using RPA.Execution.Models;
using RPA.Execution.Services;

namespace RPA.Execution.Tests;

public class FlowExecutionEngineTests
{
    [Fact]
    public async Task RunAsync_WithSimpleFlow_ExecutesAllSteps()
    {
        var mockStepExecutor = new Mock<IStepExecutor>();
        var engine = new FlowExecutionEngine(mockStepExecutor.Object);

        var flow = new Flow
        {
            Name = "Test Flow",
            Steps = new List<Step>
            {
                new WaitStep { Milliseconds = 100 },
                new TypeTextStep { Template = "Hello {{Name}}" }
            }
        };

        var row1 = new CsvRow { RowIndex = 0 };
        row1["Name"] = "World";
        var rows = new List<CsvRow> { row1 };

        var options = new ExecutionOptions { OnErrorPolicy = OnErrorPolicy.Continue };

        var results = await engine.RunAsync(flow, rows, options, null, CancellationToken.None);

        Assert.Single(results);
        Assert.True(results[0].Success);
        mockStepExecutor.Verify(x => x.ExecuteAsync(It.IsAny<Step>(), It.IsAny<IReadOnlyDictionary<string, string>>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task RunAsync_WithMultipleRows_ExecutesFlowForEachRow()
    {
        var mockStepExecutor = new Mock<IStepExecutor>();
        var engine = new FlowExecutionEngine(mockStepExecutor.Object);

        var flow = new Flow
        {
            Name = "Test Flow",
            Steps = new List<Step>
            {
                new TypeTextStep { Template = "{{Name}}" }
            }
        };

        var row1 = new CsvRow { RowIndex = 0 };
        row1["Name"] = "John";
        var row2 = new CsvRow { RowIndex = 1 };
        row2["Name"] = "Jane";
        var row3 = new CsvRow { RowIndex = 2 };
        row3["Name"] = "Bob";
        var rows = new List<CsvRow> { row1, row2, row3 };

        var options = new ExecutionOptions { OnErrorPolicy = OnErrorPolicy.Continue };

        var results = await engine.RunAsync(flow, rows, options, null, CancellationToken.None);

        Assert.Equal(3, results.Count);
        Assert.All(results, r => Assert.True(r.Success));
        mockStepExecutor.Verify(x => x.ExecuteAsync(It.IsAny<Step>(), It.IsAny<IReadOnlyDictionary<string, string>>(), It.IsAny<CancellationToken>()), Times.Exactly(3));
    }

    [Fact]
    public async Task RunAsync_WithStepFailure_ContinuesIfPolicyIsContinue()
    {
        var mockStepExecutor = new Mock<IStepExecutor>();
        mockStepExecutor.Setup(x => x.ExecuteAsync(It.IsAny<Step>(), It.IsAny<IReadOnlyDictionary<string, string>>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Window not found"));

        var engine = new FlowExecutionEngine(mockStepExecutor.Object);

        var flow = new Flow
        {
            Name = "Test Flow",
            Steps = new List<Step> { new WaitStep { Milliseconds = 100 } }
        };

        var rows = new List<CsvRow>
        {
            new CsvRow { RowIndex = 0 },
            new CsvRow { RowIndex = 1 }
        };

        var options = new ExecutionOptions { OnErrorPolicy = OnErrorPolicy.Continue };

        var results = await engine.RunAsync(flow, rows, options, null, CancellationToken.None);

        Assert.Equal(2, results.Count);
        Assert.All(results, r => Assert.False(r.Success));
        Assert.All(results, r => Assert.NotNull(r.ErrorMessage));
    }

    [Fact]
    public async Task RunAsync_WithStepFailure_AbortsIfPolicyIsAbort()
    {
        var mockStepExecutor = new Mock<IStepExecutor>();
        mockStepExecutor.Setup(x => x.ExecuteAsync(It.IsAny<Step>(), It.IsAny<IReadOnlyDictionary<string, string>>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Window not found"));

        var engine = new FlowExecutionEngine(mockStepExecutor.Object);

        var flow = new Flow
        {
            Name = "Test Flow",
            Steps = new List<Step> { new WaitStep { Milliseconds = 100 } }
        };

        var rows = new List<CsvRow>
        {
            new CsvRow { RowIndex = 0 },
            new CsvRow { RowIndex = 1 }
        };

        var options = new ExecutionOptions { OnErrorPolicy = OnErrorPolicy.Abort };

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => engine.RunAsync(flow, rows, options, null, CancellationToken.None));

        Assert.Equal("Window not found", ex.Message);
    }

    [Fact]
    public async Task RunAsync_WithRepeatExecutionSource_ExecutesFlowMultipleTimes()
    {
        var mockStepExecutor = new Mock<IStepExecutor>();
        var engine = new FlowExecutionEngine(mockStepExecutor.Object);

        var flow = new Flow
        {
            Name = "Repeat Flow",
            Steps = new List<Step> { new WaitStep { Milliseconds = 50 } },
            ExecutionSource = new RepeatExecutionSource(5)
        };

        var options = new ExecutionOptions { OnErrorPolicy = OnErrorPolicy.Continue };

        var results = await engine.RunAsync(flow, options, null, CancellationToken.None);

        Assert.Equal(5, results.Count);
        Assert.All(results, r => Assert.True(r.Success));
        mockStepExecutor.Verify(x => x.ExecuteAsync(It.IsAny<Step>(), It.IsAny<IReadOnlyDictionary<string, string>>(), It.IsAny<CancellationToken>()), Times.Exactly(5));
    }

    [Fact]
    public async Task RunAsync_WithCsvExecutionSource_ExecutesFlowForEachRow()
    {
        var mockStepExecutor = new Mock<IStepExecutor>();
        var engine = new FlowExecutionEngine(mockStepExecutor.Object);

        var row1 = new CsvRow { RowIndex = 0 };
        row1["Name"] = "Alice";
        var row2 = new CsvRow { RowIndex = 1 };
        row2["Name"] = "Bob";

        var flow = new Flow
        {
            Name = "CSV Flow",
            Steps = new List<Step> { new TypeTextStep { Template = "{{Name}}" } },
            ExecutionSource = new CsvExecutionSource(new List<CsvRow> { row1, row2 })
        };

        var options = new ExecutionOptions { OnErrorPolicy = OnErrorPolicy.Continue };

        var results = await engine.RunAsync(flow, options, null, CancellationToken.None);

        Assert.Equal(2, results.Count);
        Assert.All(results, r => Assert.True(r.Success));
        mockStepExecutor.Verify(x => x.ExecuteAsync(It.IsAny<Step>(), It.IsAny<IReadOnlyDictionary<string, string>>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task RunAsync_WithoutExecutionSource_ThrowsInvalidOperation()
    {
        var mockStepExecutor = new Mock<IStepExecutor>();
        var engine = new FlowExecutionEngine(mockStepExecutor.Object);

        var flow = new Flow
        {
            Name = "Flow Without Source",
            Steps = new List<Step> { new WaitStep { Milliseconds = 50 } }
        };

        var options = new ExecutionOptions { OnErrorPolicy = OnErrorPolicy.Continue };

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => engine.RunAsync(flow, options, null, CancellationToken.None));

        Assert.Contains("ExecutionSource", ex.Message);
    }
}
