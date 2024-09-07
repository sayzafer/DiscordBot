using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.Lavalink;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Discord_Bot.Commands
{
    public class BasicCommands : BaseCommandModule
    {
        private Queue<LavalinkTrack> _trackQueue = new Queue<LavalinkTrack>();

        [Command("play")]
        public async Task PlayCommand(CommandContext ctx, [RemainingText] string search)
        {
            var lavalink = ctx.Client.GetLavalink();
            var node = lavalink.ConnectedNodes.Values.First();

            var guildConnection = node.GetGuildConnection(ctx.Guild);
            if (guildConnection == null)
            {
                var channel = ctx.Member?.VoiceState?.Channel;
                if (channel == null)
                {
                    await ctx.RespondAsync("Bir ses kanalında olmalısınız.");
                    return;
                }

                guildConnection = await node.ConnectAsync(channel);
            }
            // URL'nin playlist olup olmadığını kontrol et
            LavalinkSearchType searchType;
            if (search.Contains("playlist"))
            {
                searchType = LavalinkSearchType.Plain; // Playlist ise Plain araması yap
            }
            else
            {
                searchType = LavalinkSearchType.Youtube; // Playlist değilse Youtube'da arama yap
            }

            var loadResult = await node.Rest.GetTracksAsync(search, searchType);
            if (loadResult.LoadResultType == LavalinkLoadResultType.NoMatches)
            {
                await ctx.RespondAsync("Hiçbir sonuç bulunamadı.");
                return;
            }

            // Playlist kontrolü
            if (loadResult.LoadResultType == LavalinkLoadResultType.PlaylistLoaded)
            {
                foreach (var track in loadResult.Tracks)
                {
                    _trackQueue.Enqueue(track); // Playlist'teki tüm parçalar sıraya eklenir
                }
                await ctx.RespondAsync($"{loadResult.PlaylistInfo.Name} playlistindeki şarkılar kuyruğa eklendi.");

                // Eğer o an bir parça çalmıyorsa, ilk parçayı çalmaya başla
                if (guildConnection.CurrentState.CurrentTrack == null)
                {
                    var firstTrack = _trackQueue.Dequeue();
                    await guildConnection.PlayAsync(firstTrack);
                    await ctx.RespondAsync($"Şu an çalıyor: {firstTrack.Title}");
                }
            }
            else if (loadResult.LoadResultType == LavalinkLoadResultType.TrackLoaded || loadResult.LoadResultType == LavalinkLoadResultType.SearchResult)
            {
                var track = loadResult.Tracks.First();

                if (guildConnection.CurrentState.CurrentTrack != null)
                {
                    _trackQueue.Enqueue(track);
                    await ctx.RespondAsync($"{track.Title} kuyruğa eklendi.");
                }
                else
                {
                    await guildConnection.PlayAsync(track);
                    await ctx.RespondAsync($"Şu an çalıyor: {track.Title}");
                }
            }
        }

        [Command("skip")]
        public async Task SkipCommand(CommandContext ctx)
        {
            var lavalink = ctx.Client.GetLavalink();
            var node = lavalink.ConnectedNodes.Values.First();
            var guildConnection = node.GetGuildConnection(ctx.Guild);

            if (guildConnection == null || guildConnection.CurrentState.CurrentTrack == null)
            {
                await ctx.RespondAsync("Şu anda çalınan bir parça yok.");
                return;
            }

            if (_trackQueue.Count > 0)
            {
                var nextTrack = _trackQueue.Dequeue();
                await guildConnection.PlayAsync(nextTrack);
                await ctx.RespondAsync($"Şimdi çalıyor: {nextTrack.Title}");
            }
            else
            {
                await guildConnection.StopAsync();
                await ctx.RespondAsync("Kuyrukta başka parça yok.");
            }
        }

        [Command("pause")]
        public async Task PauseCommand(CommandContext ctx)
        {
            var lavalink = ctx.Client.GetLavalink();
            var node = lavalink.ConnectedNodes.Values.First();
            var guildConnection = node.GetGuildConnection(ctx.Guild);

            if (guildConnection == null || guildConnection.CurrentState.CurrentTrack == null)
            {
                await ctx.RespondAsync("Şu anda çalınan bir parça yok.");
                return;
            }

            await guildConnection.PauseAsync();
            await ctx.RespondAsync("Müzik duraklatıldı.");
        }

        [Command("resume")]
        public async Task ResumeCommand(CommandContext ctx)
        {
            var lavalink = ctx.Client.GetLavalink();
            var node = lavalink.ConnectedNodes.Values.First();
            var guildConnection = node.GetGuildConnection(ctx.Guild);

            if (guildConnection == null || guildConnection.CurrentState.CurrentTrack == null)
            {
                await ctx.RespondAsync("Şu anda çalınan bir parça yok.");
                return;
            }

            await guildConnection.ResumeAsync();
            await ctx.RespondAsync("Müzik devam ediyor.");
        }

        [Command("stop")]
        public async Task StopCommand(CommandContext ctx)
        {
            var lavalink = ctx.Client.GetLavalink();
            var node = lavalink.ConnectedNodes.Values.First();
            var guildConnection = node.GetGuildConnection(ctx.Guild);

            if (guildConnection == null || guildConnection.CurrentState.CurrentTrack == null)
            {
                await ctx.RespondAsync("Şu anda çalınan bir parça yok.");
                return;
            }

            await guildConnection.StopAsync();
            await ctx.RespondAsync("Müzik durduruldu ve çalma sırası temizlendi.");
            _trackQueue.Clear(); // Kuyruğu temizle
        }

        [Command("delete")]
        [RequirePermissions(Permissions.ManageMessages)]  // Only users with message management permissions can use this command
        public async Task SilCommand(CommandContext ctx, int count)
        {
            // Check if the number of messages to delete is within limits
            if (count < 1 || count > 100)
            {
                await ctx.RespondAsync("Please provide a number between 1 and 100.");
                return;
            }

            // Get and delete the messages
            var messages = await ctx.Channel.GetMessagesAsync(count + 1); // This also includes the command message
            await ctx.Channel.DeleteMessagesAsync(messages);

            // Send confirmation message and delete it after a short delay
            var confirmationMessage = await ctx.RespondAsync($"{count} messages deleted.");
            await Task.Delay(2000);  // Delete the confirmation message after 2 seconds
            await confirmationMessage.DeleteAsync();
        }
    }
}
