using MbientLab.MetaWear.Impl.Platform;
using System;
using System.IO;
using System.Threading.Tasks;

#if WINDOWS_UWP
using Windows.Storage;
#endif

namespace MetaWear.UWP
{
    class LibraryIO : ILibraryIO
    {
        private readonly string macAddrStr;

        public LibraryIO(ulong macAddr)
        {
            macAddrStr = macAddr.ToString("X");
        }

#if WINDOWS_UWP
        public async Task<Stream> LocalLoadAsync(string key)
        {
            StorageFolder root, folder;

            root = await ((await ApplicationData.Current.LocalFolder.TryGetItemAsync(Application.CachePath) != null) ?
                ApplicationData.Current.LocalFolder.GetFolderAsync(Application.CachePath) :
                ApplicationData.Current.LocalFolder.CreateFolderAsync(Application.CachePath));
            folder = await (await root.TryGetItemAsync(macAddrStr) == null ? root.CreateFolderAsync(macAddrStr) : root.GetFolderAsync(macAddrStr));

            return await folder.OpenStreamForReadAsync(string.Format("{0}.bin", key));
        }

        public async Task LocalSaveAsync(string key, byte[] data)
        {
            StorageFolder root, folder;

            root = await ((await ApplicationData.Current.LocalFolder.TryGetItemAsync(Application.CachePath) != null) ?
                ApplicationData.Current.LocalFolder.GetFolderAsync(Application.CachePath) :
                ApplicationData.Current.LocalFolder.CreateFolderAsync(Application.CachePath));
            folder = await (await root.TryGetItemAsync(macAddrStr) == null ? root.CreateFolderAsync(macAddrStr) : root.GetFolderAsync(macAddrStr));

            using (var stream = await folder.OpenStreamForWriteAsync(string.Format("{0}.bin", key), CreationCollisionOption.ReplaceExisting))
            {
                stream.Write(data, 0, data.Length);
            }
        }
#else
            public async Task<Stream> LocalLoadAsync(string key) {
                return await Task.FromResult(File.Open(Path.Combine(Directory.GetCurrentDirectory(), Application.CachePath, macAddrStr, key), FileMode.Open));
            }

            public Task LocalSaveAsync(string key, byte[] data) {
                var root = Path.Combine(Directory.GetCurrentDirectory(), Application.CachePath, macAddrStr);
                if (!Directory.Exists(root)) {
                    Directory.CreateDirectory(root);
                }
                using (Stream outs = File.Open(Path.Combine(root, key), FileMode.Create)) {
                    outs.Write(data, 0, data.Length);
                }
                return Task.CompletedTask;
            }
#endif
    }

}
