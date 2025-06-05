using Playnite.SDK;
using Playnite.SDK.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace RandomSorting
{
    public enum LabelType
    {
        Category,
        Tag
    }

    public class RandomSortingSettings : ObservableObject
    {
        private bool updateOnStartup = false;
        private bool updateOnGameStart = false;
        private LabelType selectedLabelType = LabelType.Category;
        private string randomPrefix = "[Random] ";
        private bool includeUninstalledGames = false;

        public bool UpdateOnStartup
        {
            get => updateOnStartup;
            set => SetValue(ref updateOnStartup, value);
        }

        public bool UpdateOnGameStart
        {
            get => updateOnGameStart;
            set => SetValue(ref updateOnGameStart, value);
        }

        public LabelType SelectedLabelType
        {
            get => selectedLabelType;
            set => SetValue(ref selectedLabelType, value);
        }

        public string RandomPrefix
        {
            get => randomPrefix;
            set => SetValue(ref randomPrefix, value);
        }

        public bool IncludeUninstalledGames
        {
            get => includeUninstalledGames;
            set => SetValue(ref includeUninstalledGames, value);
        }
    }

    public class RandomSortingSettingsViewModel : ObservableObject, ISettings
    {
        private readonly RandomSorting plugin;
        private RandomSortingSettings settings;

        public RandomSortingSettings Settings
        {
            get => settings;
            set => SetValue(ref settings, value);
        }

        public string RandomPrefix
        {
            get => settings.RandomPrefix;
            set => settings.RandomPrefix = value;
        }

        public Array LabelTypeOptions => Enum.GetValues(typeof(LabelType));

        public ICommand AssignRandomIdentifiersCommand { get; }

        public RandomSortingSettingsViewModel(RandomSorting plugin)
        {
            this.plugin = plugin;
            settings = plugin.LoadPluginSettings<RandomSortingSettings>() ?? new RandomSortingSettings();

            AssignRandomIdentifiersCommand = new RelayCommand(AssignRandomIdentifiers);
        }

        private void AssignRandomIdentifiers()
        {
            plugin.AssignRandomIdentifiers(Settings.SelectedLabelType);
        }

        public void BeginEdit() { }

        public void CancelEdit() { }

        public void EndEdit()
        {
            plugin.SavePluginSettings(settings);
        }

        public bool VerifySettings(out List<string> errors)
        {
            errors = null;
            return true;
        }
    }
}
