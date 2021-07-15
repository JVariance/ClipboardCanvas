﻿using System;
using Windows.Storage;

namespace ClipboardCanvas.Logging
{
    public class ExceptionLogger : ILogger
    {
        private StorageFile _destinationFile;

        public ExceptionLogger()
        {
            GetFile();
        }

        public async void GetFile()
        {
            _destinationFile = await ApplicationData.Current.LocalFolder.CreateFileAsync("clipboardcanvas_exceptionlog.log", CreationCollisionOption.OpenIfExists);
        }

        public async void Log(string message)
        {
#if !DEBUG
            if (_destinationFile == null || string.IsNullOrEmpty(message))
            {
                return;
            }

            DateTime currentTime = DateTime.Now;

            string compiledString = $"[{currentTime.ToString()}] {message}";

            try
            {
                await FileIO.AppendTextAsync(_destinationFile, compiledString);
            }
            catch (Exception)
            {
            }
#endif
        }
    }
}
