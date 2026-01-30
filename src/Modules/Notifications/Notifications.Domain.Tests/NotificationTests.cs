using FluentAssertions;
using Notifications.Domain.Entities;
using Notifications.Domain.Enums;
using Notifications.Domain.Events;

namespace Notifications.Domain.Tests;

public class NotificationTests
{
    [Fact]
    public void Create_WithValidData_ShouldSucceed()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var title = "Budget Warning";
        var message = "You have reached 80% of your budget";
        var metadata = new Dictionary<string, string>
        {
            { "BudgetId", Guid.NewGuid().ToString() }
        };

        // Act
        var result = Notification.Create(
            userId,
            NotificationType.BudgetWarning,
            NotificationChannel.Email,
            title,
            message,
            metadata
        );

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.UserId.Should().Be(userId);
        result.Value.Type.Should().Be(NotificationType.BudgetWarning);
        result.Value.Channel.Should().Be(NotificationChannel.Email);
        result.Value.Title.Should().Be(title);
        result.Value.Message.Should().Be(message);
        result.Value.Metadata.Should().BeEquivalentTo(metadata);
        result.Value.Status.Should().Be(NotificationStatus.Pending);
        result.Value.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        result.Value.SentAt.Should().BeNull();
        result.Value.ReadAt.Should().BeNull();
        result.Value.FailureReason.Should().BeNull();
    }

    [Fact]
    public void Create_WithEmptyUserId_ShouldFail()
    {
        // Act
        var result = Notification.Create(
            Guid.Empty,
            NotificationType.BudgetWarning,
            NotificationChannel.Email,
            "Title",
            "Message"
        );

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Notification.InvalidUser");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithInvalidTitle_ShouldFail(string invalidTitle)
    {
        // Act
        var result = Notification.Create(
            Guid.NewGuid(),
            NotificationType.BudgetWarning,
            NotificationChannel.Email,
            invalidTitle,
            "Message"
        );

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Notification.InvalidTitle");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithInvalidMessage_ShouldFail(string invalidMessage)
    {
        // Act
        var result = Notification.Create(
            Guid.NewGuid(),
            NotificationType.BudgetWarning,
            NotificationChannel.Email,
            "Title",
            invalidMessage
        );

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Notification.InvalidMessage");
    }

    [Fact]
    public void Create_WithNullMetadata_ShouldUseEmptyDictionary()
    {
        // Act
        var result = Notification.Create(
            Guid.NewGuid(),
            NotificationType.BudgetWarning,
            NotificationChannel.Email,
            "Title",
            "Message",
            null
        );

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Metadata.Should().NotBeNull();
        result.Value.Metadata.Should().BeEmpty();
    }

    [Fact]
    public void Create_ShouldRaiseNotificationCreatedEvent()
    {
        // Arrange
        var userId = Guid.NewGuid();

        // Act
        var result = Notification.Create(
            userId,
            NotificationType.BudgetWarning,
            NotificationChannel.Email,
            "Title",
            "Message"
        );

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.DomainEvents.Should().HaveCount(1);
        var domainEvent = result.Value.DomainEvents.First() as NotificationCreatedEvent;
        domainEvent.Should().NotBeNull();
        domainEvent!.NotificationId.Should().Be(result.Value.Id);
        domainEvent.UserId.Should().Be(userId);
        domainEvent.Type.Should().Be(NotificationType.BudgetWarning);
        domainEvent.Channel.Should().Be(NotificationChannel.Email);
    }

    [Fact]
    public void MarkAsSent_ShouldUpdateStatusAndSetSentAt()
    {
        // Arrange
        var notification = Notification.Create(
            Guid.NewGuid(),
            NotificationType.BudgetWarning,
            NotificationChannel.Email,
            "Title",
            "Message"
        ).Value;

        // Act
        notification.MarkAsSent();

        // Assert
        notification.Status.Should().Be(NotificationStatus.Sent);
        notification.SentAt.Should().NotBeNull();
        notification.SentAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void MarkAsSent_ShouldRaiseNotificationSentEvent()
    {
        // Arrange
        var notification = Notification.Create(
            Guid.NewGuid(),
            NotificationType.BudgetWarning,
            NotificationChannel.Email,
            "Title",
            "Message"
        ).Value;
        notification.ClearDomainEvents();

        // Act
        notification.MarkAsSent();

        // Assert
        notification.DomainEvents.Should().HaveCount(1);
        var domainEvent = notification.DomainEvents.First() as NotificationSentEvent;
        domainEvent.Should().NotBeNull();
        domainEvent!.NotificationId.Should().Be(notification.Id);
        domainEvent.UserId.Should().Be(notification.UserId);
        domainEvent.Channel.Should().Be(notification.Channel);
    }

    [Fact]
    public void MarkAsFailed_ShouldUpdateStatusAndSetFailureReason()
    {
        // Arrange
        var notification = Notification.Create(
            Guid.NewGuid(),
            NotificationType.BudgetWarning,
            NotificationChannel.Email,
            "Title",
            "Message"
        ).Value;
        var failureReason = "SMTP connection failed";

        // Act
        notification.MarkAsFailed(failureReason);

        // Assert
        notification.Status.Should().Be(NotificationStatus.Failed);
        notification.FailureReason.Should().Be(failureReason);
    }

    [Fact]
    public void MarkAsFailed_ShouldRaiseNotificationFailedEvent()
    {
        // Arrange
        var notification = Notification.Create(
            Guid.NewGuid(),
            NotificationType.BudgetWarning,
            NotificationChannel.Email,
            "Title",
            "Message"
        ).Value;
        notification.ClearDomainEvents();
        var failureReason = "SMTP connection failed";

        // Act
        notification.MarkAsFailed(failureReason);

        // Assert
        notification.DomainEvents.Should().HaveCount(1);
        var domainEvent = notification.DomainEvents.First() as NotificationFailedEvent;
        domainEvent.Should().NotBeNull();
        domainEvent!.NotificationId.Should().Be(notification.Id);
        domainEvent.UserId.Should().Be(notification.UserId);
        domainEvent.Channel.Should().Be(notification.Channel);
        domainEvent.Reason.Should().Be(failureReason);
    }

    [Fact]
    public void MarkAsRead_WhenSent_ShouldUpdateStatusAndSetReadAt()
    {
        // Arrange
        var notification = Notification.Create(
            Guid.NewGuid(),
            NotificationType.BudgetWarning,
            NotificationChannel.Email,
            "Title",
            "Message"
        ).Value;
        notification.MarkAsSent();

        // Act
        notification.MarkAsRead();

        // Assert
        notification.Status.Should().Be(NotificationStatus.Read);
        notification.ReadAt.Should().NotBeNull();
        notification.ReadAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void MarkAsRead_WhenSent_ShouldRaiseNotificationReadEvent()
    {
        // Arrange
        var notification = Notification.Create(
            Guid.NewGuid(),
            NotificationType.BudgetWarning,
            NotificationChannel.Email,
            "Title",
            "Message"
        ).Value;
        notification.MarkAsSent();
        notification.ClearDomainEvents();

        // Act
        notification.MarkAsRead();

        // Assert
        notification.DomainEvents.Should().HaveCount(1);
        var domainEvent = notification.DomainEvents.First() as NotificationReadEvent;
        domainEvent.Should().NotBeNull();
        domainEvent!.NotificationId.Should().Be(notification.Id);
        domainEvent.UserId.Should().Be(notification.UserId);
    }

    [Fact]
    public void MarkAsRead_WhenNotSent_ShouldNotUpdateStatus()
    {
        // Arrange
        var notification = Notification.Create(
            Guid.NewGuid(),
            NotificationType.BudgetWarning,
            NotificationChannel.Email,
            "Title",
            "Message"
        ).Value;

        // Act
        notification.MarkAsRead();

        // Assert
        notification.Status.Should().Be(NotificationStatus.Pending);
        notification.ReadAt.Should().BeNull();
    }

    [Fact]
    public void MarkAsRead_WhenFailed_ShouldNotUpdateStatus()
    {
        // Arrange
        var notification = Notification.Create(
            Guid.NewGuid(),
            NotificationType.BudgetWarning,
            NotificationChannel.Email,
            "Title",
            "Message"
        ).Value;
        notification.MarkAsFailed("Error");

        // Act
        notification.MarkAsRead();

        // Assert
        notification.Status.Should().Be(NotificationStatus.Failed);
        notification.ReadAt.Should().BeNull();
    }

    [Fact]
    public void Create_WithDifferentNotificationTypes_ShouldWork()
    {
        // Arrange & Act
        var budgetWarning = Notification.Create(
            Guid.NewGuid(),
            NotificationType.BudgetWarning,
            NotificationChannel.Email,
            "Warning",
            "Warning message"
        ).Value;

        var budgetExceeded = Notification.Create(
            Guid.NewGuid(),
            NotificationType.BudgetExceeded,
            NotificationChannel.Email,
            "Exceeded",
            "Exceeded message"
        ).Value;

        // Assert
        budgetWarning.Type.Should().Be(NotificationType.BudgetWarning);
        budgetExceeded.Type.Should().Be(NotificationType.BudgetExceeded);
    }

    [Fact]
    public void Create_WithDifferentChannels_ShouldWork()
    {
        // Arrange & Act
        var emailNotification = Notification.Create(
            Guid.NewGuid(),
            NotificationType.BudgetWarning,
            NotificationChannel.Email,
            "Title",
            "Message"
        ).Value;

        var pushNotification = Notification.Create(
            Guid.NewGuid(),
            NotificationType.BudgetWarning,
            NotificationChannel.Push,
            "Title",
            "Message"
        ).Value;

        var inAppNotification = Notification.Create(
            Guid.NewGuid(),
            NotificationType.BudgetWarning,
            NotificationChannel.InApp,
            "Title",
            "Message"
        ).Value;

        // Assert
        emailNotification.Channel.Should().Be(NotificationChannel.Email);
        pushNotification.Channel.Should().Be(NotificationChannel.Push);
        inAppNotification.Channel.Should().Be(NotificationChannel.InApp);
    }
}
