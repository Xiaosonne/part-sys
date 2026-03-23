using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Moq;
using Xunit;
using InventorySystem.Core.Interfaces;
using InventorySystem.Core.Models;
using InventorySystem.Infrastructure.Services;

namespace InventorySystem.Tests.Services;

public class WorkflowServiceTests
{
    private readonly Mock<IWorkflowDefinitionRepository> _definitionRepoMock;
    private readonly Mock<IWorkflowInstanceRepository> _instanceRepoMock;
    private readonly Mock<IWorkflowTaskRepository> _taskRepoMock;
    private readonly Mock<IWorkflowHistoryRepository> _historyRepoMock;
    private readonly WorkflowService _service;

    public WorkflowServiceTests()
    {
        _definitionRepoMock = new Mock<IWorkflowDefinitionRepository>();
        _instanceRepoMock = new Mock<IWorkflowInstanceRepository>();
        _taskRepoMock = new Mock<IWorkflowTaskRepository>();
        _historyRepoMock = new Mock<IWorkflowHistoryRepository>();

        _service = new WorkflowService(
            _definitionRepoMock.Object,
            _instanceRepoMock.Object,
            _taskRepoMock.Object,
            _historyRepoMock.Object);
    }

    [Fact]
    public async Task CreateDefinitionAsync_ShouldCreateWithTimestamps()
    {
        // Arrange
        var definition = new WorkflowDefinition
        {
            Name = "Test Workflow",
            Category = "Project",
            Nodes = new()
        };

        _definitionRepoMock.Setup(r => r.CreateAsync(It.IsAny<WorkflowDefinition>()))
            .Callback<WorkflowDefinition>(d => d.Id = "def1")
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.CreateDefinitionAsync(definition);

        // Assert
        Assert.NotNull(result.Id);
        Assert.Equal("Test Workflow", result.Name);
        Assert.NotEqual(default, result.CreatedAt);
        _definitionRepoMock.Verify(r => r.CreateAsync(It.IsAny<WorkflowDefinition>()), Times.Once);
    }

    [Fact]
    public async Task StartInstanceAsync_ShouldCreateInstanceAndFirstTask()
    {
        // Arrange
        var definition = new WorkflowDefinition
        {
            Id = "def1",
            Name = "Test",
            Version = 1,
            Nodes = new()
            {
                new WorkflowNode
                {
                    Id = "node1",
                    NodeType = "SingleApproval",
                    Name = "Approval",
                    Approvers = new() { "user1" }
                },
                new WorkflowNode
                {
                    Id = "node2",
                    NodeType = "End",
                    Name = "End"
                }
            }
        };

        _definitionRepoMock.Setup(r => r.GetByIdAsync("def1"))
            .ReturnsAsync(definition);

        _instanceRepoMock.Setup(r => r.CreateAsync(It.IsAny<WorkflowInstance>()))
            .Callback<WorkflowInstance>(i => i.Id = "inst1")
            .Returns(Task.CompletedTask);

        // Act
        var instance = await _service.StartInstanceAsync("def1", "Project", "proj1", "user0");

        // Assert
        Assert.NotNull(instance.Id);
        Assert.Equal("Running", instance.Status);
        Assert.Equal("user0", instance.StartedBy);
        Assert.Equal("node1", instance.CurrentNodeId);

        _instanceRepoMock.Verify(r => r.CreateAsync(It.IsAny<WorkflowInstance>()), Times.Once);
        _taskRepoMock.Verify(r => r.CreateAsync(It.IsAny<WorkflowTask>()), Times.Once);
    }

    [Fact]
    public async Task GetDefinitionsAsync_ShouldFilterByCategory()
    {
        // Arrange
        var definitions = new List<WorkflowDefinition>
        {
            new() { Id = "1", Name = "Def1", Category = "Project" }
        };

        _definitionRepoMock.Setup(r => r.GetByCategoryAsync("Project"))
            .ReturnsAsync(definitions);

        // Act
        var result = await _service.GetDefinitionsAsync("Project");

        // Assert
        Assert.Single(result);
        _definitionRepoMock.Verify(r => r.GetByCategoryAsync("Project"), Times.Once);
    }

    [Fact]
    public async Task CancelInstanceAsync_ShouldUpdateStatus()
    {
        // Arrange
        var instance = new WorkflowInstance
        {
            Id = "inst1",
            Status = "Running"
        };

        _instanceRepoMock.Setup(r => r.GetByIdAsync("inst1"))
            .ReturnsAsync(instance);

        // Act
        await _service.CancelInstanceAsync("inst1");

        // Assert
        _instanceRepoMock.Verify(r => r.UpdateAsync("inst1", It.Is<WorkflowInstance>(
            i => i.Status == "Cancelled")), Times.Once);
    }

    [Fact]
    public async Task StartInstanceAsync_ShouldSaveFormData()
    {
        // Arrange
        var formData = new Dictionary<string, object>
        {
            { "projectCode", "PROJ-001" },
            { "projectDesc", "Test Project" }
        };

        var definition = new WorkflowDefinition
        {
            Id = "def1",
            Name = "Test",
            Version = 1,
            Nodes = new()
            {
                new WorkflowNode
                {
                    Id = "node1",
                    NodeType = "SingleApproval",
                    Name = "Approval",
                    Approvers = new() { "user1" },
                    FormFields = new()
                    {
                        new FormField
                        {
                            Id = "field1",
                            FieldType = "text",
                            Label = "Project Code",
                            Key = "projectCode",
                            Required = true
                        }
                    }
                }
            }
        };

        _definitionRepoMock.Setup(r => r.GetByIdAsync("def1"))
            .ReturnsAsync(definition);

        _instanceRepoMock.Setup(r => r.CreateAsync(It.IsAny<WorkflowInstance>()))
            .Callback<WorkflowInstance>(i => i.Id = "inst1")
            .Returns(Task.CompletedTask);

        // Act
        var instance = await _service.StartInstanceAsync("def1", "Project", "proj1", "user0", null, null, formData);

        // Assert
        Assert.NotNull(instance.FormData);
        Assert.Equal("PROJ-001", instance.FormData["projectCode"]);
        Assert.Equal("Test Project", instance.FormData["projectDesc"]);

        _instanceRepoMock.Verify(r => r.CreateAsync(It.Is<WorkflowInstance>(
            i => i.FormData.Count == 2)), Times.Once);
    }

    [Fact]
    public async Task StartInstanceAsync_ShouldSaveFormDataInHistory()
    {
        // Arrange
        var formData = new Dictionary<string, object>
        {
            { "projectCode", "PROJ-001" }
        };

        var definition = new WorkflowDefinition
        {
            Id = "def1",
            Name = "Test",
            Version = 1,
            Nodes = new()
            {
                new WorkflowNode
                {
                    Id = "node1",
                    NodeType = "SingleApproval",
                    Name = "Approval",
                    Approvers = new() { "user1" }
                }
            }
        };

        _definitionRepoMock.Setup(r => r.GetByIdAsync("def1"))
            .ReturnsAsync(definition);

        _instanceRepoMock.Setup(r => r.CreateAsync(It.IsAny<WorkflowInstance>()))
            .Callback<WorkflowInstance>(i => i.Id = "inst1")
            .Returns(Task.CompletedTask);

        // Act
        await _service.StartInstanceAsync("def1", "Project", "proj1", "user0", null, null, formData);

        // Assert
        _historyRepoMock.Verify(r => r.CreateAsync(It.Is<WorkflowHistory>(
            h => h.FormData.Count == 1 && h.FormData["projectCode"].Equals("PROJ-001"))), Times.Once);
    }
}
