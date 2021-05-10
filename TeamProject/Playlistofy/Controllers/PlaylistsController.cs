﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Playlistofy.Models;
using Playlistofy.Models.ViewModel;
using Playlistofy.Utils;
using Playlistofy.Controllers;
using Playlistofy.Data.Abstract;

namespace Playlistofy.Controllers
{
    public class PlaylistsController : Controller
    {
        private readonly IPlaylistRepository _pRepo;
        private readonly ITrackRepository _tRepo;
        private readonly IPlaylistofyUserRepository _puRepo;
        private readonly IKeywordRepository _kRepo;
        private readonly IHashtagRepository _hRepo;
        private readonly UserManager<IdentityUser> _userManager;

        private readonly IConfiguration _config;

        private static string _spotifyClientId;
        private static string _spotifyClientSecret;

        public PlaylistsController(IPlaylistRepository pRepo, ITrackRepository tRepo, IPlaylistofyUserRepository puRepo, IKeywordRepository kRepo, IHashtagRepository hRepo, IConfiguration config, UserManager<IdentityUser> userManager)
        {
            _pRepo = pRepo;
            _tRepo = tRepo;
            _puRepo = puRepo;
            _kRepo = kRepo;
            _hRepo = hRepo;
            _userManager = userManager;

            _config = config;

            _spotifyClientId = config["Spotify:ClientId"];
            _spotifyClientSecret = config["Spotify:ClientSecret"];
        }

        private Task<IdentityUser> GetCurrentUserAsync() => _userManager.GetUserAsync(HttpContext.User);

        // GET: Playlists
        public async Task<IActionResult> Index()
        {
            if (await GetCurrentUserAsync() != null)
            {
                var spotifyDBContext = _pRepo.GetAllWithUser().ToList();
                return View(spotifyDBContext);
            }
            else
            {
                return RedirectToAction("Error", "Error");
            }
        }

        // GET: Playlists
        public async Task<IActionResult> UserPlaylists(string id)
        {
            var viewModel = new userPlaylistsTracks();

            //Finds current logged in user using identity 
            IdentityUser usr = await GetCurrentUserAsync();
            if (usr == null) { return View("~/Views/Home/Privacy.cshtml"); }

            //Instantiates the Model to call it's functions - Finds current logged in user's spotify ID
            var getUserPlaylists = new getCurrentUserPlaylists(_userManager, _spotifyClientId, _spotifyClientSecret);
            string _userSpotifyId = await getUserPlaylists.GetCurrentUserId(usr);
            if (_userSpotifyId == null || _userSpotifyId == "") { return View("~/Views/Home/Privacy.cshtml"); }

            //Create's client and then finds all playlists for current logged in user
            var _spotifyClient = getCurrentUserPlaylists.makeSpotifyClient(_spotifyClientId, _spotifyClientSecret);
            viewModel.Playlists = await getUserPlaylists.GetCurrentUserPlaylists(_spotifyClient, _userSpotifyId, usr.Id);

            //Get current logged in user's information
            var getUserInfo = new getCurrentUserInformation(_userManager, _spotifyClientId, _spotifyClientSecret);
            viewModel.User = await getUserInfo.GetCurrentUserInformation(_spotifyClient, _userSpotifyId);

            //Get current logged in user's tracks
            var getUserTracks = new getCurrentUserTracks(_userManager, _spotifyClientId, _spotifyClientSecret);

            if (await _pRepo.FindByIdAsync(id) == null)
            {
                var newPlaylist = viewModel.Playlists.Where(name => name.Id == id);
                foreach (var playlist in newPlaylist)
                {
                    Console.WriteLine(playlist.Id);
                    
                    if (await _pRepo.FindByIdAsync(id) == null)
                    {
                        List<Track> Tracks = await getUserTracks.GetPlaylistTrack(_spotifyClient, _userSpotifyId, playlist.Id);
                        await _pRepo.AddAsync(playlist);
                        foreach (Track track in Tracks)
                        {
                            Console.WriteLine(track.Id);
                            if (await _tRepo.FindByIdAsync(track.Id) == null)
                            {
                                await _tRepo.AddAsync(track);
                            }
                            await _tRepo.AddTrackPlaylistMap(track.Id, playlist.Id);
                        }
                    }
                }
            }

            var currentUserID = await _userManager.GetUserIdAsync(usr);
            var userPlaylists = _pRepo.GetAllWithUser().Where(i => i.User.Id == currentUserID);
            
            viewModel._PlaylistsDB = await userPlaylists.ToListAsync();
            return View(viewModel);
        }

        // GET: Playlists/Details/5
        public IActionResult DetailsFromSearch(string id)
        {
            if (id == null)
            {
                return NotFound();
            }
            
            var playlist = _pRepo.GetAllWithUser().Where(i => i.Id == id).FirstOrDefault();
            if (playlist == null)
            {
                return NotFound();
            }
            var Tracks = _pRepo.GetAllPlaylistTracks(playlist);
            var hashtags = _hRepo.GetAllForPlaylist(playlist.Id);
            var keywords = _kRepo.GetAllForPlaylist(playlist.Id);
            List<string> words = new List<string>();
            foreach(Hashtag hash in hashtags)
            {
                words.Add(hash.HashTag1);
            }
            foreach(Keyword key in keywords)
            {
                words.Add(key.Keyword1);
            }
            foreach(Track t in Tracks)
            {
                if(t.Duration == null)
                {
                    t.Duration = Utils.AlgorithmicOperations.MsConversion.ConvertMsToMinSec(t.DurationMs);
                }
            }
            var TracksForPlaylistModel = new TracksForPlaylist
            {
                Playlist = playlist,
                Tracks = Tracks,
                Tags = words
            };
            return View(TracksForPlaylistModel);
        }

        // GET: Playlists/Create
        public async Task<IActionResult> Create()
        {
            if (await GetCurrentUserAsync() != null)
            {
                ViewData["UserId"] = new SelectList(_puRepo.GetAll(), "Id", "Id");
                return View();
            }
            else
            {
                return RedirectToAction("Error", "Error");
            }
        }

        // POST: Playlists/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Description,Name")] Playlist playlist, string keywordList)
        {
            if (await GetCurrentUserAsync() != null)
            {
                    Random ran = new Random();

                string b = "a1b2c3d4e5f6g7h8i9jklmnopqrstuvwxyz";
                int length = 25;
                string randomId = "";

                for (int i = 0; i < length; i++)
                {
                    int a = ran.Next(35);
                    randomId = randomId + b.ElementAt(a);
                }

                Console.WriteLine("The random alphabet generated is: {0}", randomId);

                IdentityUser usr = await GetCurrentUserAsync();
                playlist.User = await _puRepo.FindByIdAsync(usr.Id);
                playlist.UserId = usr.Id;
                playlist.Id = randomId;
                playlist.DateCreated = DateTime.Now;
                //playlist.Href = " ";

                //playlist.UserId =
                if (ModelState.IsValid)
                {
                    await _pRepo.AddAsync(playlist);

                    string[] list = keywordList.Split(',',' ');
                    List<string> kwList = list.ToList();
                    foreach (var word in kwList)
                    {
                        if(word.Length > 0)
                        {
                            if(word.StartsWith("#"))
                            {
                                if (_hRepo.FindByHashtag(word) == null)
                                {
                                    Hashtag h = new Hashtag()
                                    {
                                        HashTag1 = word
                                    };
                                    await _hRepo.AddAsync(h);
                                    await _hRepo.AddPlaylistHashtagMap(playlist.Id, h.Id);
                                }
                                else
                                {
                                    await _hRepo.AddPlaylistHashtagMap(playlist.Id, _hRepo.FindByHashtag(word).Id);
                                }
                            }
                            else
                            {
                                if (_kRepo.FindByKeyword(word) == null)
                                {
                                    Keyword k = new Keyword()
                                    {
                                        Keyword1 = word
                                    };
                                    await _kRepo.AddAsync(k);
                                    await _kRepo.AddPlaylistKeywordMap(playlist.Id, k.Id);
                                }
                                else
                                {
                                    await _kRepo.AddPlaylistKeywordMap(playlist.Id, _kRepo.FindByKeyword(word).Id);
                                }
                            }
                        }
                    }
                    return RedirectToAction("SearchTracks", "Tracks", new { id = playlist.Id });
                }
                var errors = ModelState.Values.SelectMany(v => v.Errors);
                ViewData["UserId"] = new SelectList(_puRepo.GetAll(), "Id", "Id", playlist.UserId);
                return View(playlist);
            }
            else
            {
                return RedirectToAction("Error", "Error");
            }
        }

        // GET: Playlists/Edit/5
        public async Task<IActionResult> Edit(string id)
        {
            var usr = await GetCurrentUserAsync();
            if (id == null)
            {
                return NotFound();
            }

            var playlist = _pRepo.GetAll().Include("PlaylistHashtagMaps").Include("PlaylistKeywordMaps").Where(i => i.Id == id).FirstOrDefault();
            if (playlist == null)
            {
                return NotFound();
            }
            if(usr.Id == playlist.UserId)
            {
                List<string> tags = new List<string>();
                List<Hashtag> hashtags = _hRepo.GetAllForPlaylist(id);
                List<Keyword> keywords = _kRepo.GetAllForPlaylist(id);
                foreach (var i in hashtags)
                {
                    tags.Add(i.HashTag1);
                }
                foreach (var i in keywords)
                {
                    tags.Add(i.Keyword1);
                }
                ViewData["UserId"] = new SelectList(_puRepo.GetAll(), "Id", "Id", playlist.UserId);
                ViewData["Tags"] = tags;
                return View(playlist);
            }
            else
            {
                return RedirectToAction("Error", "Error");
            }
        }

        // POST: Playlists/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit([Bind("Id,Name,Public,Collaborative,Description,User")] Playlist playlist, string tags)
        {
            var usr = await GetCurrentUserAsync();
            if (playlist == null)
            {
                return NotFound();
            }
            if(playlist.User == null)
            {
                playlist.User = await _puRepo.FindByIdAsync(usr.Id);
                playlist.UserId = playlist.User.Id;
            }
            
                if (ModelState.IsValid)
                {
                    try
                    {
                        await _pRepo.UpdateAsync(playlist);
                    }
                    catch (DbUpdateConcurrencyException)
                    {
                        if (!await _pRepo.ExistsAsync(playlist.Id))
                        {
                            return NotFound();
                        }
                        else
                        {
                            throw;
                        }
                    }
                    return RedirectToAction(nameof(UserPlaylists));
                }
                ViewData["UserId"] = new SelectList(_puRepo.GetAll(), "Id", "Id", playlist.UserId);
                return View(playlist);
            
        }

        // GET: Playlists/Delete/5
        public async Task<IActionResult> Delete(string id)
        {
            var usr = await GetCurrentUserAsync();
            if (id == null)
            {
                return NotFound();
            }
            var playlist = _pRepo.GetAllWithUser().Where(i => i.Id == id).FirstOrDefault();
            if (usr.Id == playlist.UserId)
            {
                if (playlist == null)
                {
                    return NotFound();
                }

                return View(playlist);
            }
            else
            {
                return RedirectToAction("Error", "Error");
            }
        }

        // POST: Playlists/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            var playlist = await _pRepo.FindByIdAsync(id);
            //Added to remove tracks too
            var playlistmaps = _pRepo.GetPlaylistTrackMaps(id);
            foreach (var map in playlistmaps)
            {
                await _pRepo.DeleteTrackMapAsync(map);
            }
            await _pRepo.DeleteAsync(playlist);

            
            
            return RedirectToAction(nameof(UserPlaylists));
        }

        public async Task<IActionResult> AddSpotifyPlaylistsAsync(string userID, string playlistID)
        {
            //Checks if a user is logged in before proceeding, else takes them to login page
            IdentityUser usr = await GetCurrentUserAsync();
            if (usr == null) { return RedirectToPage("/Account/Login", new { area = "Identity" }); }

            var viewModel = new SearchingSpotifyPlaylists();

            viewModel.PersonalPlaylists = _pRepo.GetAll().Include("PlaylistTrackMaps").Where(name => name.UserId == userID).ToList();
            viewModel.UserID = userID;
            viewModel.SpotifyPlaylists = new List<Playlist>();

            if (playlistID != null)
            {
                //Creates searchSpotify folder with necessary functions to use later
                var SearchSpotify = new searchSpotify(_userManager, _spotifyClientId, _spotifyClientSecret);
                //Creates spotify client
                var _spotifyClient = SearchSpotify.makeSpotifyClient(_spotifyClientId, _spotifyClientSecret);
                var playlist = await SearchSpotify.GetPlaylist(_spotifyClient, playlistID);

                if (await _pRepo.FindByIdAsync(playlistID) == null)
                {
                    playlist.UserId = _userManager.GetUserId(User);
                    await _pRepo.AddAsync(playlist);

                    var playlistTracks = await SearchSpotify.GetPlaylistTracks(_spotifyClient, playlistID);
                    foreach (var track in playlistTracks)
                    {
                        if (await _tRepo.FindByIdAsync(track.Id) == null)
                        {
                            await _tRepo.AddAsync(track);
                        }
                        await _tRepo.AddTrackPlaylistMap(track.Id, playlist.Id);
                    }
                }
            }

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddSpotifyPlaylistsAsync(string userID, [Bind("SearchingPlaylistParameter")] SearchingSpotifyPlaylists viewModel)
        {
            var NewViewModel = new SearchingSpotifyPlaylists();

            NewViewModel.PersonalPlaylists = _pRepo.GetAll().Include("PlaylistTrackMaps").Where(name => name.UserId == userID).ToList();
            NewViewModel.UserID = userID;

            if (ModelState.IsValid)
            {
                NewViewModel.SearchingPlaylistParameter = viewModel.SearchingPlaylistParameter;

                //Creates searchSpotify folder with necessary functions to use later
                var SearchSpotify = new searchSpotify(_userManager, _spotifyClientId, _spotifyClientSecret);
                //Creates spotify client
                var _spotifyClient = SearchSpotify.makeSpotifyClient(_spotifyClientId, _spotifyClientSecret);
                //Search and return a list of tracks
                var SearchPlaylists = await SearchSpotify.SearchPlaylists(_spotifyClient, viewModel.SearchingPlaylistParameter, NewViewModel.PersonalPlaylists);

                NewViewModel.SpotifyPlaylists = SearchPlaylists;
            }

            return View(NewViewModel);
        }
    }
}
