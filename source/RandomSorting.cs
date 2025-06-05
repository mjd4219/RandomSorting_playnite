using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Controls;
using Playnite.SDK;
using Playnite.SDK.Data;
using Playnite.SDK.Events;
using Playnite.SDK.Models;
using Playnite.SDK.Plugins;

namespace RandomSorting
{
    public class RandomSorting : GenericPlugin
    {
        private static readonly ILogger logger = LogManager.GetLogger();
        private RandomSortingSettingsViewModel settings { get; set; }
        private CancellationTokenSource cancellationTokenSource;

        public override Guid Id { get; } = Guid.Parse("a6af1255-7acc-4b6d-99f1-661065beb1b9");

        public RandomSorting(IPlayniteAPI api) : base(api)
        {
            settings = new RandomSortingSettingsViewModel(this);
            Properties = new GenericPluginProperties
            {
                HasSettings = true
            };
        }

        public override void OnGameStarted(OnGameStartedEventArgs args)
        {
            if (settings.Settings.UpdateOnGameStart)
            {
                StartUpdateRandomCategories();
            }
        }

        public override void OnApplicationStarted(OnApplicationStartedEventArgs args)
        {
            if (settings.Settings.UpdateOnStartup)
            {
                StartUpdateRandomCategories();
            }
        }

        public override void OnApplicationStopped(OnApplicationStoppedEventArgs args)
        {
            cancellationTokenSource?.Cancel();
        }

        public override ISettings GetSettings(bool firstRunSettings)
        {
            return settings;
        }

        public override UserControl GetSettingsView(bool firstRunSettings)
        {
            return new RandomSortingSettingsView();
        }

        private async void StartUpdateRandomCategories()
        {
            cancellationTokenSource = new CancellationTokenSource();
            var token = cancellationTokenSource.Token;

            try
            {
                await Task.Run(() => AssignRandomIdentifiers(settings.Settings.SelectedLabelType, token), token);
            }
            catch (OperationCanceledException)
            {
                logger.Info("Random category/tag update was cancelled.");
            }
        }

        public void AssignRandomIdentifiers(LabelType labelType, CancellationToken? token = null)
        {
            string prefix = string.IsNullOrWhiteSpace(settings.Settings.RandomPrefix) ? "[Random] " : settings.Settings.RandomPrefix;

            logger.Info("Fetching games from the Playnite database...");

            var games = PlayniteApi.Database.Games
                .Where(g => !g.Hidden &&
                            (settings.Settings.IncludeUninstalledGames || !string.IsNullOrEmpty(g.InstallDirectory)))
                .ToList();

            logger.Info($"Total games to process: {games.Count}");

            var rng = new Random();
            var allTags = PlayniteApi.Database.Tags.ToList();
            var allCategories = PlayniteApi.Database.Categories.ToList();

            foreach (var game in games)
            {
                if (token?.IsCancellationRequested == true) return;

                try
                {
                    int randomNumber = rng.Next(10000, 99999); ;
                    string newLabelName = $"{prefix}{randomNumber}";

                    if (labelType == LabelType.Tag)
                    {
                        var existingTags = allTags.Where(t => game.TagIds?.Contains(t.Id) == true && !t.Name.StartsWith(prefix)).ToList();
                        Tag newTag = allTags.FirstOrDefault(t => t.Name == newLabelName) ?? new Tag { Name = newLabelName };

                        PlayniteApi.MainView.UIDispatcher.Invoke(() =>
                        {
                            if (!allTags.Contains(newTag))
                            {
                                PlayniteApi.Database.Tags.Add(newTag);
                            }
                            game.TagIds = existingTags.Select(t => t.Id).Concat(new[] { newTag.Id }).ToList();
                            PlayniteApi.Database.Games.Update(game);
                        });
                    }
                    else
                    {
                        var existingCategories = allCategories.Where(c => game.CategoryIds?.Contains(c.Id) == true && !c.Name.StartsWith(prefix)).ToList();
                        Category newCategory = allCategories.FirstOrDefault(c => c.Name == newLabelName) ?? new Category { Name = newLabelName };

                        PlayniteApi.MainView.UIDispatcher.Invoke(() =>
                        {
                            if (!allCategories.Contains(newCategory))
                            {
                                PlayniteApi.Database.Categories.Add(newCategory);
                            }
                            game.CategoryIds = existingCategories.Select(c => c.Id).Concat(new[] { newCategory.Id }).ToList();
                            PlayniteApi.Database.Games.Update(game);
                        });
                    }
                }
                catch (Exception ex)
                {
                    logger.Error($"Error updating game '{game.Name}': {ex.Message}");
                }
            }

            logger.Info("Random category/tag update completed.");
        }
    }
}
