using BookingService.Infrastructure.Data;
using BookingService.Infrastructure.Messaging.Contracts;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Rebus.Handlers;

namespace BookingService.Infrastructure.Messaging;

/// <summary>
/// Обработчик ошибок отмены бронирования из Dead Letter Queue Catalog Service.
/// Вызывается когда Catalog Service не смог обработать CancelBookingJobByRequestIdRequest.
/// </summary>
public class CancelBookingErrorsHandler : IHandleMessages<CancelBookingJobByRequestIdRequest>
{
    private readonly Services.BookingService _bookingService;
    private readonly ProcessedEventRepository _processedEventRepository;
    private readonly ILogger<CancelBookingErrorsHandler> _logger;

    public CancelBookingErrorsHandler(Services.BookingService bookingService, ILogger<CancelBookingErrorsHandler> logger, ProcessedEventRepository processedEventRepository)
    {
        _bookingService = bookingService;
        _logger = logger;
        _processedEventRepository = processedEventRepository;
    }

    public async Task Handle(CancelBookingJobByRequestIdRequest message)
    {
        _logger.LogInformation(
            "Получено событие CancellationError: eventId={EventId}, requestId={RequestId}",
            message.EventId, message.RequestId);

        var processedEvent = await _processedEventRepository.FindByIdAsync(message.EventId);

        if (processedEvent != null)
        {
            _logger.LogWarning(
                "Событие CancellationError с eventId={EventId} уже обработано ранее, повторная обработка игнорируется",
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
            await _bookingService.HandleCancellationError(message.RequestId);
        }
        catch (DbUpdateException ex)
        {
            if (ex.InnerException is PostgresException sqlEx)
            {
                if (sqlEx.SqlState == PostgresErrorCodes.UniqueViolation)
                {
                    _logger.LogWarning(
                        "Событие CancellationError с eventId={EventId} уже обработано ранее, повторная обработка игнорируется",
                        message.EventId);
                }
            }
        }
    }
}
