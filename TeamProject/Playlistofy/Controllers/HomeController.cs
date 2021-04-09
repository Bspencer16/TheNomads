﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Playlistofy.Models;

namespace Playlistofy.Controllers
{
    public class HomeController : Controller
    {
        private readonly UserManager<IdentityUser> _userManager;

        private readonly ILogger<HomeController> _logger;
        private readonly IConfiguration _config;
        private static SpotifyDBContext _context;
        private static string _spotifyClientId;
        private static string _spotifyClientSecret;

        public HomeController(ILogger<HomeController> logger, IConfiguration config, UserManager<IdentityUser> userManager, SpotifyDBContext context)
        {
            _userManager = userManager;
            _logger = logger;
            _config = config;
            _context = context;
            _spotifyClientId = config["Spotify:ClientId"];
            _spotifyClientSecret = config["Spotify:ClientSecret"];
        }

        public async Task<IActionResult> Index()
        {
            if(_userManager.GetUserId(User) != null)
            {
                Task userData = SetUserData();
                await userData;
            }
            return View();
        }

        public async Task<IActionResult> SpotifyProfile()
        {
            var spotifyUserId = await GetUserId();

            return Redirect("https://open.spotify.com/user/" + spotifyUserId);
        }

        [HttpGet]
        private async Task<string> GetUserId()
        {
            IdentityUser usr = await GetCurrentUserAsync();

            var getUserPlaylists = new getCurrentUserPlaylists(_userManager, _spotifyClientId, _spotifyClientSecret);
            string _userSpotifyId = await getUserPlaylists.GetCurrentUserId(usr);

            var spotifyClient = getUserPlaylists.makeSpotifyClient(_spotifyClientId, _spotifyClientSecret);

            var spotifyUserInfo = await spotifyClient.UserProfile.Get(_userSpotifyId);

            string user = spotifyUserInfo.Id;

            return user;
        }

        private Task<IdentityUser> GetCurrentUserAsync() => _userManager.GetUserAsync(HttpContext.User);
        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        public async Task SetUserData()
        {
            var getUserPlaylists = new getCurrentUserPlaylists(_userManager, _spotifyClientId, _spotifyClientSecret);
            var getUserTracks = new getCurrentUserTracks(_userManager, _spotifyClientId, _spotifyClientSecret);
            var _spotifyClient = getUserPlaylists.makeSpotifyClient(_spotifyClientId, _spotifyClientSecret);
            IdentityUser usr = await GetCurrentUserAsync();
            string _userSpotifyId = await getUserPlaylists.GetCurrentUserId(usr);
            List<Playlist> Playlists = await getUserPlaylists.GetCurrentUserPlaylists(_spotifyClient, _userSpotifyId, usr.Id);
            if (_context.Pusers.Find(usr.Id) == null)
            {
                _context.Pusers.Add(new PUser()
                {
                    Id = usr.Id,
                    UserName = usr.UserName,
                    NormalizedUserName = usr.NormalizedUserName,
                    Email = usr.Email,
                    NormalizedEmail = usr.NormalizedEmail,
                    EmailConfirmed = usr.EmailConfirmed,
                    PasswordHash = usr.PasswordHash,
                    SecurityStamp = usr.SecurityStamp,
                    ConcurrencyStamp = usr.ConcurrencyStamp,
                    PhoneNumber = usr.PhoneNumber,
                    PhoneNumberConfirmed = usr.PhoneNumberConfirmed,
                    TwoFactorEnabled = usr.TwoFactorEnabled,
                    LockoutEnd = usr.LockoutEnd,
                    LockoutEnabled = usr.LockoutEnabled,
                    AccessFailedCount = usr.AccessFailedCount,
                    Followers = 0,
                    DisplayName = null,
                    ImageUrl = null,
                    SpotifyUserId = null,
                    Href = null
                });
            }
            foreach (Playlist i in Playlists)
            {
                if (_context.Playlists.Find(i.Id) == null)
                {
                    List<Track> Tracks = await getUserTracks.GetPlaylistTrack(_spotifyClient, _userSpotifyId, i.Id);
                    _context.Playlists.Add(i);
                    foreach (Track j in Tracks)
                    {
                        if (_context.Tracks.Find(j.Id) == null)
                        {
                            _context.Tracks.Add(j);
                            _context.PlaylistTrackMaps.Add(
                                new PlaylistTrackMap()
                                {
                                    PlaylistId = i.Id,
                                    TrackId = j.Id
                                }
                                );
                        }
                    }
                }
                
            }
            
            _context.SaveChanges();
            //var t = new getCurrentUserTracks(_userManager, _spotifyClientId, _spotifyClientSecret);
        }
    }
}
