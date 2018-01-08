using PanelDataFeed;
using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using System.Threading;

namespace ChromePanelDataFeed
{
    public class ChromePanelDataFeed : IDataFeed
    {
        private FileSystemWatcher _watcher;
        public IEnumerable<DataFeedSettingDeclaration> RequiredSettings()
        {
            var requiredSettings = new List<DataFeedSettingDeclaration>
            {
                new DataFeedSettingDeclaration
                {
                    SettingName = "ChromeBookmarkLocation",
                    SettingType = SettingType.File,
                    DefaultValue = "%UserProfile%\\AppData\\Local\\Google\\Chrome\\User Data\\Default\\Bookmarks",
                    DisplayRequestForSetting = "Unable to locate Bookmarks File. Please provide path to Chrome Bookmarks file"
                }
            };

            return requiredSettings;
        }

        private void SendBookmarks(string bookmarkLocation, IPanelCommunicator communicator)
        {
            try
            {
                using (FileStream stream = File.Open(bookmarkLocation, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    using (StreamReader reader = new StreamReader(stream))
                    {
                        var bookmarksJson = reader.ReadToEnd();
                        communicator.SendMessageToPanel(bookmarksJson);
                    }
                }
            }
            catch(Exception e)
            {
                Thread.Sleep(1000);
                SendBookmarks(bookmarkLocation, communicator);
            }   
        }

        public void Start(IDataFeedContext dataFeedContext, IPanelCommunicator panelCommunicators)
        {
            var bookmarkLocation = dataFeedContext.GetSettings()["ChromeBookmarkLocation"].SettingValue;
            bookmarkLocation = Environment.ExpandEnvironmentVariables(bookmarkLocation);
            SendBookmarks(bookmarkLocation, panelCommunicators);
            _watcher = new FileSystemWatcher
            {
                Path = Path.GetDirectoryName(bookmarkLocation),
                Filter = Path.GetFileName(bookmarkLocation),
                NotifyFilter = NotifyFilters.LastAccess |
                                    NotifyFilters.LastWrite |
                                    NotifyFilters.FileName |
                                    NotifyFilters.DirectoryName
            };

            _watcher.Changed += (o, e) => SendBookmarks(bookmarkLocation, panelCommunicators);
            _watcher.Created += (o, e) => SendBookmarks(bookmarkLocation, panelCommunicators);
            _watcher.Renamed += (o, e) => SendBookmarks(bookmarkLocation, panelCommunicators);

            _watcher.EnableRaisingEvents = true;
        }
    }
}
