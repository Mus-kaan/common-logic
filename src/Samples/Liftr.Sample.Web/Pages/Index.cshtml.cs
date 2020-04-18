using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Liftr.DataSource;
using Microsoft.Liftr.Queue;
using System.Threading.Tasks;

namespace Liftr.Sample.Web.Pages
{
    public class IndexModel : PageModel
    {
        private const string c_countrtName = "pv-index";
        private readonly ICounterEntityDataSource _counter;
        private readonly IQueueWriter _q;
        private readonly Serilog.ILogger _logger;

        public int CurrentCounter { get; set; }

        public IndexModel(ICounterEntityDataSource counter, IQueueWriter q, Serilog.ILogger logger)
        {
            _counter = counter;
            _q = q;
            _logger = logger;
        }

        public async Task OnGetAsync()
        {
            await _counter.IncreaseCounterAsync(c_countrtName);
            CurrentCounter = await _counter.GetCounterAsync(c_countrtName) ?? 0;
            await _q.AddMessageAsync($"Hello from Liftr.Sample.Web: PV = {CurrentCounter}");
            _logger.Information("Page view counter value: {pvCounter}.", CurrentCounter);
        }
    }
}
