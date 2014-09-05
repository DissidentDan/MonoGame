// MonoGame - Copyright (C) The MonoGame Team
// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.

using System;
using System.IO;
#if WINRT
using System.Threading.Tasks;
#elif IOS
using MonoTouch.Foundation;
using MonoTouch.UIKit;
#elif MONOMAC
using MonoMac.Foundation;
#elif PSM
using Sce.PlayStation.Core;
#endif
using Microsoft.Xna.Framework.Utilities;

namespace Microsoft.Xna.Framework
{
    public static class TitleContainer
    {
        static TitleContainer() 
        {
#if WINDOWS || LINUX
            Location = AppDomain.CurrentDomain.BaseDirectory;
#elif WINRT
            Location = Windows.ApplicationModel.Package.Current.InstalledLocation.Path;
#elif IOS || MONOMAC
			Location = NSBundle.MainBundle.ResourcePath;
#elif PSM
			Location = "/Application";
#else
            Location = string.Empty;
#endif

#if IOS
			SupportRetina = UIScreen.MainScreen.Scale == 2.0f;
#endif
		}

        static internal string Location { get; private set; }
#if IOS
        static internal bool SupportRetina { get; private set; }
#endif

#if WINRT

        private static async Task<Stream> OpenStreamAsync(string name)
        {
            var package = Windows.ApplicationModel.Package.Current;

            try
            {
                var storageFile = await package.InstalledLocation.GetFileAsync(name);
                var randomAccessStream = await storageFile.OpenReadAsync();
                return randomAccessStream.AsStreamForRead();
            }
            catch (IOException)
            {
                // The file must not exist... return a null stream.
                return null;
            }
        }

#endif // WINRT

        /// <summary>
        /// Returns an open stream to an exsiting file in the title storage area.
        /// </summary>
        /// <param name="name">The filepath relative to the title storage area.</param>
        /// <returns>A open stream or null if the file is not found.</returns>
        public static Stream OpenStream(string name)
        {
            // Normalize the file path.
            var safeName = GetFilename(name);

            // We do not accept absolute paths here.
            if (Path.IsPathRooted(safeName))
                throw new ArgumentException("Invalid filename. TitleContainer.OpenStream requires a relative path.");

#if WINRT
            var stream = Task.Run( () => OpenStreamAsync(safeName).Result ).Result;
            if (stream == null)
                throw new FileNotFoundException(name);

            return stream;
#elif ANDROID
            return Game.Activity.Assets.Open(safeName);
#elif IOS
            var absolutePath = Path.Combine(Location, safeName);
            if (SupportRetina)
            {
                // Insert the @2x immediately prior to the extension. If this file exists
                // and we are on a Retina device, return this file instead.
                var absolutePath2x = Path.Combine(Path.GetDirectoryName(absolutePath),
                                                  Path.GetFileNameWithoutExtension(absolutePath)
                                                  + "@2x" + Path.GetExtension(absolutePath));
                if (File.Exists(absolutePath2x))
                    return File.OpenRead(absolutePath2x);
            }
            return File.OpenRead(absolutePath);
#else
            var absolutePath = Path.Combine(Location, safeName);
            return File.OpenRead(absolutePath);
#endif
        }

        // TODO: This is just path normalization.  Remove this
        // and replace it with a proper utility function.  I'm sure
        // this same logic is duplicated all over the code base.
        internal static string GetFilename(string name)
        {
            return FileHelpers.NormalizeFilePathSeparators(new Uri("file:///" + name).LocalPath.Substring(1));
        }
    }
}

