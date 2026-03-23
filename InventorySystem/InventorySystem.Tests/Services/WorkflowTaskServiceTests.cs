using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Moq;
using Xunit;
using InventorySystem.Core.Interfaces;
using InventorySystem.Core.Models;
using InventorySystem.Infrastructure.Services;

namespace InventorySystem.Tests.Services;

public class WorkflowTaskServiceTests
{
    private readonly Mock<IWorkflowTaskRepository> _taskRepoMock;
    private readonly Mock<IWorkflowInstanceRepository> _instanceRepoMock;
    private readonly Mock<IWorkflowDefinitionRepository> _definitionRepoMock;
    private readonly Mock<IWorkflowHistoryRepository> _historyRepoMock;
    private readonly WorkflowTaskService _service;

    public WorkflowTaskServiceTests()
    {
        _taskRepoMock = new Mock<IWorkflowTaskRepository>();
        _instanceRepoMock = new Mock<IWorkflowInstanceRepository>();
        _definitionRepoMock = new Mock<IWorkflowDefinitionRepository>();
        _historyRepoMock = new Mock<IWorkflowHistoryRepository>();

        _service = new WorkflowTaskService(
            _taskRepoMock.Object,
            _instanceRepoMock.Object,
            _definitionRepoMock.Object,
            _historyRepoMock.Object);
    }

    [Fact]
    public async Task ApproveTaskAsync_ShouldTransitionToNextNode()
    {
        // Arrange
        var task = new WorkflowTask
        {
            Id = "task1",
            InstanceId = "inst1",
            NodeId = "node1",
            NodeName = "Approval",
            AssigneeId = "user1",
            Status = "Pending"
        };

        var instance = new WorkflowInstance
        {
            Id = "inst1",
            DefinitionId = "def1",
            CurrentNodeId = "node1",
            Status = "Running"
        };

        var definition = new WorkflowDefinition
        {
            Id = "def1",
            Nodes = new()
            {
                new WorkflowNode
                {
                    Id = "node1",
                    NodeType = "SingleApproval",
                    Name = "Approval",
                    NextNodes = new() { "node2" }
                },
                new WorkflowNode
                {
                    Id = "node2",
                    NodeType = "SingleApproval",
                    Name = "SecondApproval",
                    Approvers = new() { "user2" }
                }
            }
        };

        _taskRepoMock.Setup(r => r.GetByIdAsync("task1")).ReturnsAsync(task);
        _instanceRepoMock.Setup(r => r.GetByIdAsync("inst1")).ReturnsAsync(instance);
        _definitionRepoMock.Setup(r => r.GetByIdAsync("def1")).ReturnsAsync(definition);

        // Act
        await _service.ApproveTaskAsync("task1", "Approved");

        // Assert
        _taskRepoMock.Verify(r => r.UpdateAsync("task1", It.Is<WorkflowTask>(
            t => t.Status == "Approved" && t.Comment == "Approved")), Times.Once);

        _instanceRepoMock.Verify(r => r.UpdateAsync("inst1", It.Is<WorkflowInstance>(
            i => i.CurrentNodeId == "node2")), Times.Once);

        _taskRepoMock.Verify(r => r.CreateAsync(It.Is<WorkflowTask>(
            t => t.NodeId == "node2" && t.AssigneeId == "user2")), Times.Once);

        _historyRepoMock.Verify(r => r.CreateAsync(It.IsAny<WorkflowHistory>()), Times.Once);
    }

    [Fact]
    public async Task ApproveTaskAsync_ShouldCompleteWhenReachingEndNode()
    {
        // Arrange
        var task = new WorkflowTask
        {
            Id = "task1",
            InstanceId = "inst1",
            NodeId = "node1",
            Status = "Pending"
        };

        var instance = new WorkflowInstance
        {
            Id = "inst1",
            DefinitionId = "def1",
            CurrentNodeId = "node1",
            Status = "Running"
        };

        var definition = new WorkflowDefinition
        {
            Id = "def1",
            Nodes = new()
            {
                new WorkflowNode
                {
                    Id = "node1",
                    NodeType = "SingleApproval",
                    NextNodes = new() { "node2" }
                },
                new WorkflowNode
                {
                    Id = "node2",
                    NodeType = "End"
                }
            }
        };

        _taskRepoMock.Setup(r => r.GetByIdAsync("task1")).ReturnsAsync(task);
        _instanceRepoMock.Setup(r => r.GetByIdAsync("inst1")).ReturnsAsync(instance);
        _definitionRepoMock.Setup(r => r.GetByIdAsync("def1")).ReturnsAsync(definition);

        // Act
        await _service.ApproveTaskAsync("task1", "Done");

        // Assert
        _instanceRepoMock.Verify(r => r.UpdateAsync("inst1", It.Is<WorkflowInstance>(
            i => i.Status == "Completed" && i.CompletedAt != null)), Times.Once);
    }

    [Fact]
    public async Task RejectTaskAsync_ShouldMarkInstanceAsRejected()
    {
        // Arrange
        var task = new WorkflowTask
        {
            Id = "task1",
            InstanceId = "inst1",
            NodeId = "node1",
            Status = "Pending"
        };

        var instance = new WorkflowInstance
        {
            Id = "inst1",
            Status = "Running"
        };

        _taskRepoMock.Setup(r => r.GetByIdAsync("task1")).ReturnsAsync(task);
        _instanceRepoMock.Setup(r => r.GetByIdAsync("inst1")).ReturnsAsync(instance);

        // Act
        await _service.RejectTaskAsync("task1", "Not approved");

        // Assert
        _taskRepoMock.Verify(r => r.UpdateAsync("task1", It.Is<WorkflowTask>(
            t => t.Status == "Rejected")), Times.Once);

        _instanceRepoMock.Verify(r => r.UpdateAsync("inst1", It.Is<WorkflowInstance>(
            i => i.Status == "Rejected")), Times.Once);

        _historyRepoMock.Verify(r => r.CreateAsync(It.IsAny<WorkflowHistory>()), Times.Once);
    }

    [Fact]
    public async Task GetPendingTasksAsync_ShouldReturnUserTasks()
    {
        // Arrange
        var tasks = new List<WorkflowTask>
        {
            new() { Id = "1", AssigneeId = "user1", Status = "Pending" }
        };

        _taskRepoMock.Setup(r => r.GetPendingByAssigneeAsync("user1"))
            .ReturnsAsync(tasks);

        // Act
        var result = await _service.GetPendingTasksAsync("user1");

        // Assert
        Assert.Single(result);
        _taskRepoMock.Verify(r => r.GetPendingByAssigneeAsync("user1"), Times.Once);
    }

    [Fact]
    public async Task ApproveTaskAsync_ShouldSaveFormData()
    {
        // Arrange
        var formData = new Dictionary<string, object>
        {
            { "approvalOpinion", "同意" },
            { "approvalRemark", "符合要求" }
        };

        var task = new WorkflowTask
        {
            Id = "task1",
            InstanceId = "inst1",
            NodeId = "node1",
            NodeName = "Approval",
            AssigneeId = "user1",
            Status = "Pending"
        };

        var instance = new WorkflowInstance
        {
            Id = "inst1",
            DefinitionId = "def1",
            CurrentNodeId = "node1",
            Status = "Running"
        };

        var definition = new WorkflowDefinition
        {
            Id = "def1",
            Nodes = new()
            {
                new WorkflowNode
                {
                    Id = "node1",
                    NodeType = "SingleApproval",
                    Name = "Approval",
                    NextNodes = new() { "node2" },
                    FormFields = new()
                    {
                        new FormField
                        {
                            Id = "field1",
                            FieldType = "select",
                            Label = "审批意见",
                            Key = "approvalOpinion",
                            Required = true
                        }
                    }
                },
                new WorkflowNode
                {
                    Id = "node2",
                    NodeType = "End"
                }
            }
        };

        _taskRepoMock.Setup(r => r.GetByIdAsync("task1")).ReturnsAsync(task);
        _instanceRepoMock.Setup(r => r.GetByIdAsync("inst1")).ReturnsAsync(instance);
        _definitionRepoMock.Setup(r => r.GetByIdAsync("def1")).ReturnsAsync(definition);

        // Act
        await _service.ApproveTaskAsync("task1", "审批通过", formData);

        // Assert
        _taskRepoMock.Verify(r => r.UpdateAsync("task1", It.Is<WorkflowTask>(
            t => t.FormData.Count == 2 && t.FormData["approvalOpinion"].Equals("同意"))), Times.Once);

        _historyRepoMock.Verify(r => r.CreateAsync(It.Is<WorkflowHistory>(
            h => h.FormData.Count == 2 && h.FormData["approvalRemark"].Equals("符合要求"))), Times.Once);
    }

    [Fact]
    public async Task ApproveTaskAsync_ShouldSaveFormDataInHistory()
    {
        // Arrange
        var formData = new Dictionary<string, object>
        {
            { "approvalOpinion", "同意" }
        };

        var task = new WorkflowTask
        {
            Id = "task1",
            InstanceId = "inst1",
            NodeId = "node1",
            NodeName = "Approval",
            AssigneeId = "user1",
            Status = "Pending"
        };

        var instance = new WorkflowInstance
        {
            Id = "inst1",
            DefinitionId = "def1",
            CurrentNodeId = "node1",
            Status = "Running"
        };

        var definition = new WorkflowDefinition
        {
            Id = "def1",
            Nodes = new()
            {
                new WorkflowNode
                {
                    Id = "node1",
                    NodeType = "SingleApproval",
                    NextNodes = new() { "node2" }
                },
                new WorkflowNode
                {
                    Id = "node2",
                    NodeType = "End"
                }
            }
        };

        _taskRepoMock.Setup(r => r.GetByIdAsync("task1")).ReturnsAsync(task);
        _instanceRepoMock.Setup(r => r.GetByIdAsync("inst1")).ReturnsAsync(instance);
        _definitionRepoMock.Setup(r => r.GetByIdAsync("def1")).ReturnsAsync(definition);

        // Act
        await _service.ApproveTaskAsync("task1", "Approved", formData);

        // Assert
        _historyRepoMock.Verify(r => r.CreateAsync(It.Is<WorkflowHistory>(
            h => h.Action == "Approve" && h.FormData["approvalOpinion"].Equals("同意"))), Times.Once);
    }
}
