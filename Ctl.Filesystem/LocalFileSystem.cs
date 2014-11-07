/*
    Copyright (c) 2014, CTL Global, Inc.
    Copyright (c) 2014, iD Commerce + Logistics
    All rights reserved.

    Redistribution and use in source and binary forms, with or without modification, are permitted
    provided that the following conditions are met:

    Redistributions of source code must retain the above copyright notice, this list of conditions
    and the following disclaimer. Redistributions in binary form must reproduce the above copyright
    notice, this list of conditions and the following disclaimer in the documentation and/or other
    materials provided with the distribution.
 
    THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND ANY EXPRESS OR
    IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND
    FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR
    CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR
    CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR
    SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY
    THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR
    OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE
    POSSIBILITY OF SUCH DAMAGE.
*/

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Ctl
{
    class LocalFileSystem : FileSystem
    {
        public LocalFileSystem(Uri baseUri)
            : base(baseUri)
        {
        }

        protected internal override FileSystemEntry GetListingImpl(Uri uri)
        {
            string path = uri.LocalPath;

            if(File.Exists(path))
            {
                FileInfo fi = new FileInfo(path);
                return new FileEntry(uri, fi.Name, fi.Length, fi.CreationTimeUtc, fi.LastWriteTimeUtc);
            }

            if(Directory.Exists(path))
            {
                DirectoryInfo di = new DirectoryInfo(path);
                return new DirectoryEntry(uri, di.Name);
            }

            return new FileSystemEntry(uri, Path.GetFileName(path));
        }

        protected internal override string[] ListDirectoryImpl(Uri uri)
        {
            string directoryPath = uri.LocalPath;
            return (from p in Directory.EnumerateFileSystemEntries(directoryPath)
                    select Path.GetFileName(p)).ToArray();
        }

        protected internal override FileSystemEntry[] ListFullDirectoryImpl(Uri uri)
        {
            string directoryPath = uri.LocalPath;

            var dirs = from p in Directory.EnumerateDirectories(directoryPath)
                       select (FileSystemEntry)new DirectoryEntry(new Uri(p), Path.GetFileName(p));

            var files = from p in Directory.EnumerateFiles(directoryPath)
                        let fi = new FileInfo(Path.Combine(directoryPath, p))
                        where fi.Exists
                        select (FileSystemEntry)new FileEntry(new Uri(p), fi.Name, fi.Length, fi.CreationTimeUtc, fi.LastWriteTimeUtc);

            return Enumerable.Concat(dirs, files).ToArray();
        }

        protected internal override Stream CreateFileImpl(Uri uri)
        {
            string filePath = uri.LocalPath;
            return new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None, 4096, FileOptions.Asynchronous | FileOptions.SequentialScan);
        }

        protected internal override Stream OpenReadImpl(Uri uri)
        {
            string filePath = uri.LocalPath;
            return new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.Asynchronous | FileOptions.SequentialScan);
        }

        protected internal override void CreateDirectoryImpl(Uri uri)
        {
            Directory.CreateDirectory(uri.LocalPath);
        }

        protected internal override void DeleteFileImpl(Uri uri)
        {
            File.Delete(uri.LocalPath);
        }

        protected internal override void DeleteDirectoryImpl(Uri uri)
        {
            Directory.Delete(uri.LocalPath);
        }
    }
}
