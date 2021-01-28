using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Expeditions.Models;

namespace Expeditions.Controllers
{
    public class TeamMemberController : Controller
    {
        private ExpeditionsDbContext db;

        public IActionResult Index()
        {
            return View();
        }
        public IActionResult NothingHere()
        {
            return View();
        }

        [HttpGet]
        public IActionResult searchTeamMember(string teamMemberName)
        {

            if (string.IsNullOrEmpty(teamMemberName) == false)
            {
                if (db.TeamMembers.Where(a => a.LastName.Contains(teamMemberName)).Count() < 25)
                {
                    var teamMembers = db.TeamMembers.Where(c => c.LastName.Contains(teamMemberName));
                    return View(teamMembers);
                }
                else if (db.TeamMembers.Where(a => a.LastName.Contains(teamMemberName)).Count() == 0)
                {
                    return View("NothingHere");
                }
                else
                {

                    return View("teamMemberError");
                }
            }
            return View("Index");
        }
    }
}
