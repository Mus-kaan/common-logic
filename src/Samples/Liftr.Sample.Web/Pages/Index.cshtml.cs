using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Liftr.DataSource;
using System.Threading.Tasks;

namespace Liftr.Sample.Web.Pages
{
    public class IndexModel : PageModel
    {
        private const string c_countrtName = "pv-index";
        private readonly ICounterEntityDataSource _counter;
        private readonly Serilog.ILogger _logger;

        public IndexModel(ICounterEntityDataSource counter, Serilog.ILogger logger)
        {
            _counter = counter;
            _logger = logger;
        }

        public async Task OnGetAsync()
        {
            await _counter.IncreaseCounterAsync(c_countrtName);
            var val = await _counter.GetCounterAsync(c_countrtName);
            _logger.Information("Page view counter value: {pvCounter}.", val);
        }
    }
}
