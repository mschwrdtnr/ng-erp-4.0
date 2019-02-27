using Master40.BusinessLogicCentral.Simulator;
using Master40.DB.Data.Context;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Master40.Controllers
{
    public class AgentLiveController : Controller
    {
        private readonly AgentCore _agentSimulator;
        private readonly ProductionDomainContext _context;

        public AgentLiveController(AgentCore agentSimulator, ProductionDomainContext context)
        {
            _agentSimulator = agentSimulator;
            _context = context;
        }
        public async Task<IActionResult> Index()
        {
            ViewData["machines"] = _context.Machines.Select(x => x.Name).ToList();
            var masterDBContext = _context.Machines.Include(a => a.MachineGroup);
            return View(await masterDBContext.ToListAsync());
        }

        [HttpGet("[Controller]/RunAsync/{simId}")]
        public async void RunAsync(int simId)
        {
            if (simId == 0) return;
            // using Default Test Values.
            await _agentSimulator.RunAkkaSimulation(simId);
        }

        [HttpGet("[Controller]/ReloadGantt/{orderId}/{stateId}")]
        public IActionResult ReloadGantt(int orderId, int stateId)
        {
            //call to ReloadGantt Diagramm
            return ViewComponent("SimulationTimeline", new List<int> { orderId, stateId });
        }

        [HttpGet("[Controller]/MachineWorkload/{Machine}")]
        public IActionResult MachineWorkload(string machine)
        {
            //call to ReloadGantt Diagramm
            return ViewComponent("MachineWorkload", new { machine });
        }
    }
}