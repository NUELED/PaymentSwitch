using PaymentSwitch.Data.Abstraction;
using PaymentSwitch.Services.Implementation;

namespace PaymentSwitch.WorkerService.BackgroundServices
{
    public class ReconcilerBackgroundService : BackgroundService
    {
        private readonly ITransferRepository _repo;
        private readonly TransferService _service;
        private readonly ILogger<ReconcilerBackgroundService> _log;
        private readonly TimeSpan _pollInterval = TimeSpan.FromSeconds(30);
        private readonly int _maxRetries = 5;

        public ReconcilerBackgroundService(ITransferRepository repo, TransferService service, ILogger<ReconcilerBackgroundService> log)
        {
            _repo = repo; _service = service; _log = log;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var pending = await _repo.GetPendingAsync(_maxRetries, TimeSpan.FromMinutes(1));
                    foreach (var tx in pending)
                    {
                        try
                        {
                            await _service.HandlePendingTransactionAsync(tx);
                        }
                        catch (Exception ex)
                        {
                            _log.LogError(ex, "Error handling pending tx {ref}", tx.TransactionRef);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _log.LogError(ex, "Reconciler loop failure");
                }

                await Task.Delay(_pollInterval, stoppingToken);
            }
        }

    }

}
