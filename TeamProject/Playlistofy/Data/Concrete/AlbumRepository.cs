﻿
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Playlistofy.Data.Abstract;
using Playlistofy.Models;
using SpotifyAPI.Web;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Playlistofy.Data.Concrete
{
    public class AlbumRepository :Repository<Album>, IAlbumRepository
    {
        public AlbumRepository(SpotifyDbContext ctx) : base(ctx)
        {
            
        }

        public Album GetTrackAlbum(SpotifyClient _spotifyClient, string TrackId)
        {
            FullAlbum fullAlbum = _spotifyClient.Albums.Get(_spotifyClient.Tracks.Get(TrackId).Result.Album.Id).Result;
            Album album = new Album()
            {
                AlbumType = fullAlbum.AlbumType,
                Id = fullAlbum.Id,
                Label = fullAlbum.Label,
                Name = fullAlbum.Name,
                Popularity = fullAlbum.Popularity,
                ReleaseDate = fullAlbum.ReleaseDate,
                ReleaseDatePrecision = fullAlbum.ReleaseDatePrecision
            };
            return album;
        }

        public async Task AddAlbumTrackMap(Album a, Track t)
        {
            _context.Add<TrackAlbumMap>(new TrackAlbumMap()
            {
                TrackId = t.Id,
                AlbumId = a.Id
            });
            await _context.SaveChangesAsync();
        }

        public async Task AddArtistAlbumMap(string aId, string alId)
        {
            _context.Add<ArtistAlbumMap>(new ArtistAlbumMap()
            {
                ArtistId = aId,
                AlbumId = alId
            });
            await _context.SaveChangesAsync();
        }

        public async Task<List<Track>> GetAllAlbumTracks(SpotifyClient _spotifyClient, Album a)
        {
            var t = await _spotifyClient.Albums.GetTracks(a.Id);
            List<Track> tracks = new List<Track>();
            foreach(var m in t.Items)
            {
                if(await _context.FindAsync<Track>(m.Id) == null)
                {
                    FullTrack n = await _spotifyClient.Tracks.Get(m.Id);
                    await _context.AddAsync<Track>(new Track()
                    {
                        DiscNumber = n.DiscNumber,
                        DurationMs = n.DurationMs,
                        Duration = Utils.AlgorithmicOperations.MsConversion.ConvertMsToMinSec(n.DurationMs),
                        Explicit = n.Explicit,
                        Href = n.Href,
                        Id = n.Id,
                        IsPlayable = n.IsPlayable,
                        Name = n.Name,
                        Popularity = n.Popularity,
                        PreviewUrl = n.PreviewUrl,
                        TrackNumber = n.TrackNumber,
                        Uri = n.Uri,
                        IsLocal = n.IsLocal
                    });
                    await _context.SaveChangesAsync();
                }
                tracks.Add(await _context.FindAsync<Track>(m.Id));
            }
            return tracks;

        }

        public List<Album> FindAlbumsBySearch(string searchQuery)
        {
            var t = _dbSet.Where(a => a.Name.Contains(searchQuery)).ToList();
            return t;
        }
    }
}
