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

using Renci.SshNet;
using Renci.SshNet.Common;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Ctl
{
    class SftpFileSystem : FileSystem
    {
        SftpClient client;

        public SftpFileSystem(Uri baseUri, NetworkCredential credentials)
            : base(baseUri)
        {
            if (credentials == null) throw new ArgumentNullException("credentials");

            string host = baseUri.Host;
            int port = baseUri.Port == -1 ? 22 : baseUri.Port;

            client = new SftpClient(host, port, credentials.UserName, credentials.Password);

            client.HostKeyReceived += client_HostKeyReceived;
        }

        void client_HostKeyReceived(object sender, Renci.SshNet.Common.HostKeyEventArgs e)
        {
            e.CanTrust = OnValidateCertificate(string.Join(string.Empty, from b in e.FingerPrint
                                                                         select b.ToString("X2", CultureInfo.InvariantCulture)));
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
                catch (Renci.SshNet.Common.SshAuthenticationException ex)
                {
                    throw new BadCredentialsException("Unable to authorize with the supplied network credentials. See InnerException for more details.", ex);
                }
            }
        }

        protected internal override FileSystemEntry GetListingImpl(Uri uri)
        {
            Connect();
            return GetEntry(uri, client.Get(uri.AbsolutePath));
        }

        static FileSystemEntry GetEntry(Uri uri, Renci.SshNet.Sftp.SftpFile f)
        {
            if (f.IsDirectory)
            {
                return new DirectoryEntry(uri, f.Name);
            }
            
            if (f.IsRegularFile)
            {
                return new FileEntry(uri, f.Name, f.Length, null, f.LastWriteTimeUtc);
            }
            
            return new FileSystemEntry(uri, f.Name);
        }

        protected internal override string[] ListDirectoryImpl(Uri uri)
        {
            Connect();

            return (from f in client.ListDirectory(uri.AbsolutePath)
                    select f.Name).ToArray();
        }

        protected internal override FileSystemEntry[] ListFullDirectoryImpl(Uri uri)
        {
            Uri fileBase;

            if (!uri.AbsolutePath.EndsWith("/"))
            {
                // Make sure to have a / at the end to denote a directory.
                UriBuilder b = new UriBuilder(uri);
                b.Path += "/";
                fileBase = b.Uri;
            }
            else
            {
                fileBase = uri;
            }

            Connect();

            List<FileSystemEntry> entries = new List<FileSystemEntry>();

            foreach (var f in client.ListDirectory(uri.AbsolutePath))
            {
                Uri eUri = new Uri(fileBase, f.Name);
                entries.Add(GetEntry(eUri, f));
            }

            return entries.ToArray();
        }

        protected internal override Stream CreateFileImpl(Uri uri)
        {
            Connect();

            return client.Create(uri.AbsolutePath);
        }

        protected internal override Stream OpenReadImpl(Uri uri)
        {
            Connect();

            try
            {
                return client.OpenRead(uri.AbsolutePath);
            }
            catch (SftpPathNotFoundException ex)
            {
                throw new FileNotFoundException("A file does not exist at the given URI. See InnerException for details.", ex);
            }
        }

        protected internal override void CreateDirectoryImpl(Uri uri)
        {
            Connect();
            client.CreateDirectory(uri.AbsolutePath);
        }

        protected internal override void DeleteFileImpl(Uri uri)
        {
            Connect();
            client.DeleteFile(uri.AbsolutePath);
        }

        protected internal override void DeleteDirectoryImpl(Uri uri)
        {
            Connect();
            client.DeleteDirectory(uri.AbsolutePath);
        }
    }
}
