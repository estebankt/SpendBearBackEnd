using Budgets.Domain.Events;
using FluentAssertions;
using Moq;
using Notifications.Application.Features.EventHandlers;
using Notifications.Application.Services;
using Notifications.Domain.Entities;
using Notifications.Domain.Enums;
using Notifications.Domain.Repositories;
using Notifications.Application.Abstractions;
using SpendBear.SharedKernel;

namespace Notifications.Application.Tests;

public class BudgetWarningEventHandlerTests
{
    private readonly Mock<INotificationRepository> _mockRepository;
    private readonly Mock<IEmailService> _mockEmailService;
    private readonly Mock<INotificationsUnitOfWork> _mockUnitOfWork;
    private readonly BudgetWarningEventHandler _handler;

    public BudgetWarningEventHandlerTests()
    {
        _mockRepository = new Mock<INotificationRepository>();
        _mockEmailService = new Mock<IEmailService>();
        _mockUnitOfWork = new Mock<INotificationsUnitOfWork>();
        _handler = new BudgetWarningEventHandler(
            _mockRepository.Object,
            _mockEmailService.Object,
            _mockUnitOfWork.Object
        );
    }

    [Fact]
    public async Task Handle_WithValidEvent_ShouldCreateNotificationAndSendEmail()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var budgetId = Guid.NewGuid();
        var @event = new BudgetWarningEvent(
            budgetId,
            userId,
            "Groceries Budget",
            500.00m,
            420.00m,
            84.0m,
            80.0m
        );

        _mockEmailService
            .Setup(x => x.SendBudgetWarningEmailAsync(
                It.IsAny<Guid>(),
                It.IsAny<string>(),
                It.IsAny<decimal>(),
                It.IsAny<decimal>(),
                It.IsAny<decimal>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _handler.Handle(@event, CancellationToken.None);

        // Assert
        _mockRepository.Verify(
            x => x.AddAsync(
                It.Is<Notification>(n =>
                    n.UserId == userId &&
                    n.Type == NotificationType.BudgetWarning &&
                    n.Channel == NotificationChannel.Email &&
                    n.Title.Contains("Budget Warning") &&
                    n.Message.Contains("420.00") &&
                    n.Message.Contains("500.00") &&
                    n.Metadata.ContainsKey("BudgetId") &&
                    n.Metadata["BudgetId"] == budgetId.ToString()
                ),
                It.IsAny<CancellationToken>()
            ),
            Times.Once
        );

        _mockEmailService.Verify(
            x => x.SendBudgetWarningEmailAsync(
                userId,
                "Groceries Budget",
                500.00m,
                420.00m,
                84.0m,
                It.IsAny<CancellationToken>()
            ),
            Times.Once
        );

        _mockUnitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenEmailSucceeds_ShouldMarkNotificationAsSent()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var @event = new BudgetWarningEvent(
            Guid.NewGuid(),
            userId,
            "Test Budget",
            1000.00m,
            850.00m,
            85.0m,
            80.0m
        );

        Notification? capturedNotification = null;
        _mockRepository
            .Setup(x => x.AddAsync(It.IsAny<Notification>(), It.IsAny<CancellationToken>()))
            .Callback<Notification, CancellationToken>((n, ct) => capturedNotification = n);

        _mockEmailService
            .Setup(x => x.SendBudgetWarningEmailAsync(
                It.IsAny<Guid>(),
                It.IsAny<string>(),
                It.IsAny<decimal>(),
                It.IsAny<decimal>(),
                It.IsAny<decimal>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _handler.Handle(@event, CancellationToken.None);

        // Assert
        capturedNotification.Should().NotBeNull();
        capturedNotification!.Status.Should().Be(NotificationStatus.Sent);
        capturedNotification.SentAt.Should().NotBeNull();
    }

    [Fact]
    public async Task Handle_WhenEmailFails_ShouldMarkNotificationAsFailed()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var @event = new BudgetWarningEvent(
            Guid.NewGuid(),
            userId,
            "Test Budget",
            1000.00m,
            850.00m,
            85.0m,
            80.0m
        );

        Notification? capturedNotification = null;
        _mockRepository
            .Setup(x => x.AddAsync(It.IsAny<Notification>(), It.IsAny<CancellationToken>()))
            .Callback<Notification, CancellationToken>((n, ct) => capturedNotification = n);

        var exceptionMessage = "SMTP server unavailable";
        _mockEmailService
            .Setup(x => x.SendBudgetWarningEmailAsync(
                It.IsAny<Guid>(),
                It.IsAny<string>(),
                It.IsAny<decimal>(),
                It.IsAny<decimal>(),
                It.IsAny<decimal>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception(exceptionMessage));

        // Act
        await _handler.Handle(@event, CancellationToken.None);

        // Assert
        capturedNotification.Should().NotBeNull();
        capturedNotification!.Status.Should().Be(NotificationStatus.Failed);
        capturedNotification.FailureReason.Should().Be(exceptionMessage);
    }

    [Fact]
    public async Task Handle_ShouldIncludeAllMetadataFromEvent()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var budgetId = Guid.NewGuid();
        var @event = new BudgetWarningEvent(
            budgetId,
            userId,
            "Test Budget",
            1500.00m,
            1275.00m,
            85.0m,
            80.0m
        );

        Notification? capturedNotification = null;
        _mockRepository
            .Setup(x => x.AddAsync(It.IsAny<Notification>(), It.IsAny<CancellationToken>()))
            .Callback<Notification, CancellationToken>((n, ct) => capturedNotification = n);

        // Act
        await _handler.Handle(@event, CancellationToken.None);

        // Assert
        capturedNotification.Should().NotBeNull();
        capturedNotification!.Metadata.Should().ContainKey("BudgetId");
        capturedNotification.Metadata.Should().ContainKey("BudgetName");
        capturedNotification.Metadata.Should().ContainKey("BudgetAmount");
        capturedNotification.Metadata.Should().ContainKey("CurrentSpent");
        capturedNotification.Metadata.Should().ContainKey("PercentageUsed");
        capturedNotification.Metadata.Should().ContainKey("ThresholdPercentage");
        capturedNotification.Metadata["BudgetId"].Should().Be(budgetId.ToString());
        capturedNotification.Metadata["BudgetName"].Should().Be("Test Budget");
        capturedNotification.Metadata["BudgetAmount"].Should().Be("1500.00");
        capturedNotification.Metadata["CurrentSpent"].Should().Be("1275.00");
    }

    [Fact]
    public async Task Handle_ShouldFormatTitleAndMessageCorrectly()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var @event = new BudgetWarningEvent(
            Guid.NewGuid(),
            userId,
            "Monthly Food Budget",
            600.00m,
            510.00m,
            85.0m,
            80.0m
        );

        Notification? capturedNotification = null;
        _mockRepository
            .Setup(x => x.AddAsync(It.IsAny<Notification>(), It.IsAny<CancellationToken>()))
            .Callback<Notification, CancellationToken>((n, ct) => capturedNotification = n);

        // Act
        await _handler.Handle(@event, CancellationToken.None);

        // Assert
        capturedNotification.Should().NotBeNull();
        capturedNotification!.Title.Should().Contain("Budget Warning");
        capturedNotification.Title.Should().Contain("85%");
        capturedNotification.Title.Should().Contain("Monthly Food Budget");
        capturedNotification.Message.Should().Contain("$510.00");
        capturedNotification.Message.Should().Contain("$600.00");
        capturedNotification.Message.Should().Contain("Monthly Food Budget");
    }
}
