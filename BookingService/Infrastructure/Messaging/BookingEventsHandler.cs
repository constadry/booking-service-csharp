using BookingService.Infrastructure.Data;
using BookingService.Infrastructure.Messaging.Contracts;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Rebus.Handlers;
using System.Data.Common;

namespace BookingService.Infrastructure.Messaging;

/// <summary>
/// Обработчик событий от Catalog Service.
/// Принимает BookingJobConfirmed и BookingJobDenied, делегирует обработку в BookingService.
/// </summary>
public class BookingEventsHandler :
    IHandleMessages<BookingJobConfirmed>,
    IHandleMessages<BookingJobDenied>
{
    private readonly Services.BookingService _bookingService;

    private readonly ProcessedEventRepository _processedEventRepository;
    private readonly ILogger<BookingEventsHandler> _logger;

    public BookingEventsHandler(Services.BookingService bookingService, ILogger<BookingEventsHandler> logger, ProcessedEventRepository processedEventRepository)
    {
        _bookingService = bookingService;
        _logger = logger;
        _processedEventRepository = processedEventRepository;
    }

    public async Task Handle(BookingJobConfirmed message)
    {
        _logger.LogInformation(
            "Получено событие BookingJobConfirmed: eventId={EventId}, requestId={RequestId}",
            message.EventId, message.RequestId);

        var processedEvent = await _processedEventRepository.FindByIdAsync(message.EventId);

        if (processedEvent != null)
        {
            _logger.LogWarning(
                "Событие BookingJobConfirmed с eventId={EventId} уже обработано ранее, повторная обработка игнорируется",
                message.EventId);
            return;
        }

        _processedEventRepository.Save(new Entities.ProcessedEvent
        {
            EventId = message.EventId,
            ProcessedAt = DateTime.UtcNow
        });

        try
        {
            await _bookingService.HandleBookingJobConfirmed(message.RequestId);
        }
        catch (DbUpdateException ex)
        {
            if (ex.InnerException is PostgresException sqlEx)
            {
                if (sqlEx.SqlState == PostgresErrorCodes.UniqueViolation)
                {
                    _logger.LogWarning(
                        "Событие BookingJobConfirmed с eventId={EventId} уже обработано ранее, повторная обработка игнорируется",
                        message.EventId);
                }
            }
        }
    }

    public async Task Handle(BookingJobDenied message)
    {
        _logger.LogInformation(
            "Получено событие BookingJobDenied: eventId={EventId}, requestId={RequestId}",
            message.EventId, message.RequestId);

        var processedEvent = await _processedEventRepository.FindByIdAsync(message.EventId);

        if (processedEvent != null)
        {
            _logger.LogWarning(
                "Событие BookingJobDenied с eventId={EventId} уже обработано ранее, повторная обработка игнорируется",
                message.EventId);
            return;
        }

        _processedEventRepository.Save(new Entities.ProcessedEvent
        {
            EventId = message.EventId,
            ProcessedAt = DateTime.UtcNow
        });

        try
        {
            await _bookingService.HandleBookingJobDenied(message.RequestId);
        }
        catch (DbUpdateException ex)
        {
            if (ex.InnerException is PostgresException sqlEx)
            {
                if (sqlEx.SqlState == PostgresErrorCodes.UniqueViolation)
                {
                    _logger.LogWarning(
                        "Событие BookingJobDenied с eventId={EventId} уже обработано ранее, повторная обработка игнорируется",
                        message.EventId);
                }
            }
        }
    }
}
