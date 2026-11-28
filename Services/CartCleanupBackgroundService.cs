namespace WallsShop.Services;

/// <summary>
/// Periodically removes expired in-memory shopping carts.
/// </summary>
public class CartCleanupBackgroundService : BackgroundService
{
    private static readonly TimeSpan CleanupInterval = TimeSpan.FromHours(1);
    private readonly CartService _cartService;
    private readonly ILogger<CartCleanupBackgroundService> _logger;

    public CartCleanupBackgroundService(
        CartService cartService,
        ILogger<CartCleanupBackgroundService> logger)
    {
        _cartService = cartService;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(CleanupInterval);
        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                try
                {
                    _cartService.CleanupExpiredCarts();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred while cleaning up expired carts.");
                }
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Normal shutdown
        }
    }
}
