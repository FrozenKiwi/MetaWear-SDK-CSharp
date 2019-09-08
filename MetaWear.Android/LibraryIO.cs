using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using MbientLab.MetaWear.Impl.Platform;

namespace MetaWear.Android
{
    class LibraryIO : ILibraryIO
    {
        public Task<Stream> LocalLoadAsync(string key)
        {
            return Task.FromResult<Stream>(null);
        }

        public Task LocalSaveAsync(string key, byte[] data)
        {
            //ISharedPreferences prefs = GetSharedPreferences(btDevice.getAddress(), MODE_PRIVATE).edit();
            //prefs.PutString(key, new String(Base64.encode(data, Base64.DEFAULT)));
            //prefs.Apply();
            return Task.CompletedTask;
        }
    }
}