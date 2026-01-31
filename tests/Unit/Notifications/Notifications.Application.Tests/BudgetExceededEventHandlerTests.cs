using Budgets.Domain.Events;
using FluentAssertions;
using Moq;
using Notifications.Application.Features.EventHandlers;
using Notifications.Application.Services;
using Notifications.Domain.Entities;
using Notifications.Domain.Enums;
using Notifications.Domain.Repositories;
using SpendBear.SharedKernel;

namespace Notifications.Application.Tests;

public class BudgetExceededEventHandlerTests
{
    private readonly Mock<INotificationRepository> _mockRepository;
    private readonly Mock<IEmailService> _mockEmailService;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly BudgetExceededEventHandler _handler;

    public BudgetExceededEventHandlerTests()
    {
        _mockRepository = new Mock<INotificationRepository>();
        _mockEmailService = new Mock<IEmailService>();
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _handler = new BudgetExceededEventHandler(
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
        var @event = new BudgetExceededEvent(
            budgetId,
            userId,
            "Entertainment Budget",
            300.00m,
            345.00m,
            45.00m
        );

        _mockEmailService
            .Setup(x => x.SendBudgetExceededEmailAsync(
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
                    n.Type == NotificationType.BudgetExceeded &&
                    n.Channel == NotificationChannel.Email &&
                    n.Title.Contains("Budget Exceeded") &&
                    n.Message.Contains("345.00") &&
                    n.Message.Contains("300.00") &&
                    n.Message.Contains("45.00") &&
                    n.Metadata.ContainsKey("BudgetId") &&
                    n.Metadata["BudgetId"] == budgetId.ToString()
                ),
                It.IsAny<CancellationToken>()
            ),
            Times.Once
        );

        _mockEmailService.Verify(
            x => x.SendBudgetExceededEmailAsync(
                userId,
                "Entertainment Budget",
                300.00m,
                345.00m,
                45.00m,
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
        var @event = new BudgetExceededEvent(
            Guid.NewGuid(),
            userId,
            "Test Budget",
            500.00m,
            550.00m,
            50.00m
        );

        Notification? capturedNotification = null;
        _mockRepository
            .Setup(x => x.AddAsync(It.IsAny<Notification>(), It.IsAny<CancellationToken>()))
            .Callback<Notification, CancellationToken>((n, ct) => capturedNotification = n);

        _mockEmailService
            .Setup(x => x.SendBudgetExceededEmailAsync(
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
        var @event = new BudgetExceededEvent(
            Guid.NewGuid(),
            userId,
            "Test Budget",
            500.00m,
            550.00m,
            50.00m
        );

        Notification? capturedNotification = null;
        _mockRepository
            .Setup(x => x.AddAsync(It.IsAny<Notification>(), It.IsAny<CancellationToken>()))
            .Callback<Notification, CancellationToken>((n, ct) => capturedNotification = n);

        var exceptionMessage = "Email service timeout";
        _mockEmailService
            .Setup(x => x.SendBudgetExceededEmailAsync(
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
        var @event = new BudgetExceededEvent(
            budgetId,
            userId,
            "Shopping Budget",
            750.00m,
            825.50m,
            75.50m
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
        capturedNotification.Metadata.Should().ContainKey("ExceededBy");
        capturedNotification.Metadata["BudgetId"].Should().Be(budgetId.ToString());
        capturedNotification.Metadata["BudgetName"].Should().Be("Shopping Budget");
        capturedNotification.Metadata["BudgetAmount"].Should().Be("750.00");
        capturedNotification.Metadata["CurrentSpent"].Should().Be("825.50");
        capturedNotification.Metadata["ExceededBy"].Should().Be("75.50");
    }

    [Fact]
    public async Task Handle_ShouldFormatTitleAndMessageCorrectly()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var @event = new BudgetExceededEvent(
            Guid.NewGuid(),
            userId,
            "Monthly Travel Budget",
            1000.00m,
            1150.75m,
            150.75m
        );

        Notification? capturedNotification = null;
        _mockRepository
            .Setup(x => x.AddAsync(It.IsAny<Notification>(), It.IsAny<CancellationToken>()))
            .Callback<Notification, CancellationToken>((n, ct) => capturedNotification = n);

        // Act
        await _handler.Handle(@event, CancellationToken.None);

        // Assert
        capturedNotification.Should().NotBeNull();
        capturedNotification!.Title.Should().Contain("Budget Exceeded");
        capturedNotification.Title.Should().Contain("Monthly Travel Budget");
        capturedNotification.Message.Should().Contain("exceeded your budget");
        capturedNotification.Message.Should().Contain("$1000.00");
        capturedNotification.Message.Should().Contain("$1150.75");
        capturedNotification.Message.Should().Contain("$150.75");
    }

    [Fact]
    public async Task Handle_WithSmallExceededAmount_ShouldStillNotify()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var @event = new BudgetExceededEvent(
            Guid.NewGuid(),
            userId,
            "Test Budget",
            100.00m,
            100.01m,
            0.01m
        );

        // Act
        await _handler.Handle(@event, CancellationToken.None);

        // Assert
        _mockRepository.Verify(
            x => x.AddAsync(It.IsAny<Notification>(), It.IsAny<CancellationToken>()),
            Times.Once
        );
        _mockEmailService.Verify(
            x => x.SendBudgetExceededEmailAsync(
                It.IsAny<Guid>(),
                It.IsAny<string>(),
                It.IsAny<decimal>(),
                It.IsAny<decimal>(),
                It.IsAny<decimal>(),
                It.IsAny<CancellationToken>()),
            Times.Once
        );
    }
}
