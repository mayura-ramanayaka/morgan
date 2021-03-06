﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Morgan.Core
{
    /// <summary>
    /// View Model for the <see cref="ViewFilePage"/>
    /// </summary>
    public class ViewFilePageViewModel : BaseViewModel
    {
        #region Public Properties

        /// <summary>
        /// Service to organize the music files in a hierachical order!
        /// </summary>
        public IFileStructureService FileStructureService { get; set; }

        /// <summary>
        /// List of all the music folder locations to scan for music files
        /// </summary>
        public IList<string> LocationsList { get; set; } = new List<string>();

        /// <summary>
        /// List of music files after each music file is mapped to <see cref="MusicFileViewModel"/> instances.
        /// So that each music file contains all the required metadata that is used to re-arrange files
        /// </summary>
        public ObservableCollection<MusicFileViewModel> MusicFileList { get; set; }
            = new ObservableCollection<MusicFileViewModel>();

        /// <summary>
        /// Number of locations stored in the Location list
        /// </summary>
        public int LocationCount => LocationsList.Count;

        /// <summary>
        /// Number of music files stored in the Music File list
        /// </summary>
        public int MusicFileCount => MusicFileList.Count;

        /// <summary>
        /// Number of music genres
        /// </summary>
        public int GenreCount { get; set; }

        /// <summary>
        /// Number of music artists
        /// </summary>
        public int ArtistCount { get; set; }

        /// <summary>
        /// Number of music albums
        /// </summary>
        public int AlbumCount { get; set; }

        /// <summary>
        /// Number of music titles, or files
        /// </summary>
        public int TitleCount { get; set; }

        /// <summary>
        /// Flag indicating if everything is finished loading
        /// </summary>
        public bool EverythingLoaded { get; set; } = false;

        /// <summary>
        /// Data context of the settings form
        /// </summary>
        public SettingsFormViewModel SettingsForm { get; set; } = new SettingsFormViewModel();

        #endregion

        #region Commands

        /// <summary>
        /// Command to organize music files in a folder structure
        /// </summary>
        public ICommand OrganizeCommand { get; set; }

        /// <summary>
        /// Command to show the file organize settings
        /// </summary>
        public ICommand ShowSettingCommand { get; set; }

        #endregion

        #region Constructors

        /// <summary>
        /// Default constructor
        /// </summary>
        public ViewFilePageViewModel(IFileStructureService fileStructureService)
        {
            // Init Services
            FileStructureService = fileStructureService;
            LocationsList = DI.ApplicationViewModel.LocationList;

            // Create commands
            OrganizeCommand = new ActionCommand(Organize);
            ShowSettingCommand = new ActionCommand(ShowSettingsForm);

            // Load the music files
            LoadMusicFiles();
        }

        #endregion

        #region Command Methods

        /// <summary>
        /// Handler for the <see cref="OrganizeCommand"/>
        /// </summary>
        private async void Organize()
        {
            // Get the whole structure as a string and remove the title
            var structure = SettingsForm.FileStructure.ToUpperInvariant();
            if (structure.Contains("TITLE"))
            {
                int index = structure.IndexOf("TITLE");
                structure = structure.Remove(index, 5);
            }

            // List of tags that the user has specified in the settings
            var tags = structure.Split(",- ".ToCharArray(), StringSplitOptions.RemoveEmptyEntries)
                .Select(t => t.Trim())
                .ToList();

            // Placeholder variables for future content
            TagType? tt1, tt2, tt3;
            tt1 = tt2 = tt3 = TagType.NONE;

            // Recursively initialize the tags
            if (!string.IsNullOrEmpty((tags.ElementAtOrDefault(0))))
            {
                // Get the first tag
                tt1 = (TagType)Enum.Parse(typeof(TagType), tags.ElementAtOrDefault(0));

                if (!string.IsNullOrEmpty((tags.ElementAtOrDefault(1))))
                {
                    // Get the second tag
                    tt2 = (TagType)Enum.Parse(typeof(TagType), tags.ElementAtOrDefault(1));

                    if (!string.IsNullOrEmpty((tags.ElementAtOrDefault(2))))
                    {
                        // Get the third tag
                        tt3 = (TagType)Enum.Parse(typeof(TagType), tags.ElementAtOrDefault(2));
                    }
                }
            }

            // Start organizing the files using any file organizer provider!
            await FileStructureService.OrganizeAsync(
                SettingsForm.SaveFilePath,
                MusicFileList, 
                new FileHierachicalOrder
                {
                    Level1 = tt1,
                    Level2 = tt2,
                    Level3 = tt3,
                });
        }

        /// <summary>
        /// Displays the <see cref="SettingsFormControl"/>
        /// </summary>
        private void ShowSettingsForm()
        {
            // Show the configuration form
            SettingsForm.SettingsFormVisible ^= true;
        }

        #endregion

        #region Private Helpers

        /// <summary>
        /// Loads all the music files in the specified locations
        /// </summary>
        private async void LoadMusicFiles()
        {
            // Get all the music files in the different locations
            var list = await DI.Get<IDirectoryService>().GetMusicFilesFromAMultipleLocationsAsync(LocationsList);

            // Map each music file into MusicFileViewModel objects
            MusicFileList = new ObservableCollection<MusicFileViewModel>(await MapFilesToModelsAsync(list));

            // At least take one second when there is no music files, to make it more realistic
            if (MusicFileCount < 10)
                await Task.Delay(2000);

            // Load tag counters
            EverythingLoaded = await LoadTagCountsAsync();
        }

        /// <summary>
        /// Accepts a plain file list and returns a collection that each file is mapped to <see cref="MusicFileViewModel"/>
        /// </summary>
        /// <param name="list">List of plain music files</param>
        /// <returns></returns>
        private Task<List<MusicFileViewModel>> MapFilesToModelsAsync(List<string> list)
        {
            return Task.Run(() =>
            {
                return list.Select(f => new MusicFileViewModel(f)).ToList();
            });
        }

        /// <summary>
        /// Initializes the tag counts
        /// </summary>
        private Task<bool> LoadTagCountsAsync()
        {
            return Task.Run(() =>
            {
                GenreCount = MusicFileList.Select(t => t.Genre).Distinct().Count();
                ArtistCount = MusicFileList.Select(t => t.Artist).Distinct().Count();
                AlbumCount = MusicFileList.Select(t => t.Album).Distinct().Count();
                TitleCount = MusicFileList.Select(t => t.Title).Distinct().Count();
                return true;
            });
        }

        #endregion
    }
}
