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
using System.Net;
using FluentFTP;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Ctl
{
    class FtpFileSystem : FileSystem
    {
        FtpClient client;

        public FtpFileSystem(Uri baseUri, NetworkCredential credentials, bool encrypted)
            : base(baseUri)
        {
            if (credentials == null) throw new ArgumentNullException("credentials");

            client = new FtpClient();
            client.Host = baseUri.Host;

            if (baseUri.Port != -1)
            {
                client.Port = baseUri.Port;
            }

            client.EncryptionMode = encrypted ? FtpEncryptionMode.Implicit : FtpEncryptionMode.None;
            client.Credentials = credentials;

            client.ValidateCertificate += client_ValidateCertificate;
        }

        void client_ValidateCertificate(FtpClient control, FtpSslValidationEventArgs e)
        {
#if NET40 || NETSTANDARD2_0
            string hash = e.Certificate.GetCertHashString();
#else
            string hash = EncodeHexString(e.Certificate.GetCertHash());

            string EncodeHexString(byte[] sArray)
            {
                string result = null;
                if (sArray != null)
                {
                    char[] array = new char[sArray.Length * 2];
                    int i = 0;
                    int num = 0;
                    while (i < sArray.Length)
                    {
                        int num2 = (sArray[i] & 240) >> 4;
                        array[num++] = HexDigit(num2);
                        num2 = (int)(sArray[i] & 15);
                        array[num++] = HexDigit(num2);
                        i++;
                    }
                    result = new string(array);
                }
                return result;
            }

            char HexDigit(int num)
            {
                return (char)((num < 10) ? (num + 48) : (num + 55));
            }
#endif
            e.Accept = OnValidateCertificate(hash);
        }

        protected internal override void Dispose(bool disposing)
        {
            if (client != null)
            {
                client.Dispose();
                client = null;
            }

            base.Dispose(disposing);
        }

        void Connect()
        {
            if (client == null) throw new ObjectDisposedException("FileSystem");

            if (!client.IsConnected)
            {
                try
                {
                    client.Connect();
                }
                catch (FtpCommandException ex)
                {
                    if (string.Equals(ex.Message, "Login incorrect.", StringComparison.OrdinalIgnoreCase))
                    {
                        throw new BadCredentialsException("Unable to authorize with the supplied network credentials. See InnerException for more details.", ex);
                    }

                    throw;
                }
            }
        }

        protected internal override FileSystemEntry GetListingImpl(Uri uri)
        {
            // MLSD makes this easy.

            if (client.HasFeature(FtpCapability.MLSD))
            {
                FtpListItem f = client.GetObjectInfo(GetPath(uri));
                FtpListItem item = f;

                if (item.Type == FtpFileSystemObjectType.Link)
                {
                    item = client.DereferenceLink(item);
                }

                return GetEntry(uri, item, f);
            }

            // otherwise, if it's a directory...

            string path = GetPath(uri);
            string fileName = uri.Segments.Last();

            int lastIdx = fileName.LastIndexOf('/');

            if (lastIdx != -1)
            {
                fileName = fileName.Substring(lastIdx + 1);
            }

            fileName = Uri.UnescapeDataString(fileName);

            if (client.DirectoryExists(path))
            {
                return new DirectoryEntry(uri, fileName);
            }

            // not a directory, try to get file size and mod times.

            if (client.HasFeature(FtpCapability.SIZE))
            {
                long size = client.GetFileSize(path);

                DateTime modTime = DateTime.MinValue;

                if (client.HasFeature(FtpCapability.MDTM))
                {
                    modTime = client.GetModifiedTime(path);
                }

                if (size != -1)
                {
                    return new FileEntry(uri, fileName, size != -1 ? size : (long?)null, (DateTime?)null, modTime != DateTime.MinValue ? modTime : (DateTime?)null);
                }
            }

            // otherwise, do nothing.

            return new FileSystemEntry(uri, fileName);
        }

        protected internal override string[] ListDirectoryImpl(Uri uri)
        {
            Connect();

            string[] names = client.GetNameListing(GetPath(uri));

            for (int i = 0; i < names.Length; ++i)
            {
                string n = names[i];

                int lastIdx = n.LastIndexOf('/');

                if (lastIdx != -1)
                {
                    names[i] = n.Substring(lastIdx + 1);
                }
            }

            return names;
        }

        protected internal override FileSystemEntry[] ListFullDirectoryImpl(Uri uri)
        {
            Connect();

            FtpListItem[] listItems = client.GetListing(GetPath(uri), FtpListOption.DerefLinks | FtpListOption.SizeModify);
            FileSystemEntry[] entries = new FileSystemEntry[listItems.Length];

            for (int i = 0; i < listItems.Length; ++i)
            {
                FtpListItem f = listItems[i];
                FtpListItem item = f;

                while (item.Type == FtpFileSystemObjectType.Link && item.LinkObject != null)
                {
                    item = item.LinkObject;
                }

                Uri eUri = new Uri(uri, f.Name);

                entries[i] = GetEntry(eUri, item, f);
            }

            return entries;
        }

        static FileSystemEntry GetEntry(Uri uri, FtpListItem item, FtpListItem link)
        {
            switch (item.Type)
            {
                case FtpFileSystemObjectType.Directory:
                    return new DirectoryEntry(uri, link.Name);
                case FtpFileSystemObjectType.File:
                    return new FileEntry(uri, link.Name, item.Size != -1 ? item.Size : (long?)null, item.Created != DateTime.MinValue ? item.Created : (DateTime?)null, item.Modified != DateTime.MinValue ? item.Modified : (DateTime?)null);
                default:
                    return new FileSystemEntry(uri, link.Name);
            }
        }

        protected internal override Stream CreateFileImpl(Uri uri)
        {
            Connect();
            return client.OpenWrite(GetPath(uri));
        }

        protected internal override Stream OpenReadImpl(Uri uri)
        {

            Connect();
            return client.OpenRead(GetPath(uri));
        }

        protected internal override void CreateDirectoryImpl(Uri uri)
        {
            Connect();
            client.CreateDirectory(GetPath(uri));
        }

        protected internal override void DeleteFileImpl(Uri uri)
        {
            Connect();
            client.DeleteFile(GetPath(uri));
        }

        protected internal override void DeleteDirectoryImpl(Uri uri)
        {
            Connect();
            client.DeleteDirectory(GetPath(uri));
        }

        static string GetPath(Uri uri)
        {
            return Uri.UnescapeDataString(uri.AbsolutePath);
        }
    }
}
