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
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Ctl
{
    /// <summary>
    /// Performs operations on a filesystem.
    /// </summary>
    public abstract class FileSystem : IDisposable
    {
        readonly string authority;

        /// <summary>
        /// The base URI for all file operations.
        /// </summary>
        public Uri BaseUri { get; private set; }

        /// <summary>
        /// If a secure channel is used, fires off to validate the certificate's thumbprint.
        /// </summary>
        public event EventHandler<CertificateValidationEventArgs> ValidateCertificate;

        internal FileSystem(Uri baseUri)
        {
            BaseUri = baseUri;
            authority = BaseUri.GetLeftPart(UriPartial.Authority);
        }

        /// <summary>
        /// Disposes any resources used by the FileSystem instance.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected internal virtual void Dispose(bool disposing)
        {
            // do nothing.
        }

        /// <summary>
        /// Calls the ValidateCertificate event, if available. If not, always returns valid.
        /// </summary>
        /// <param name="thumbPrint">A certificate thumbprint to validate.</param>
        protected internal bool OnValidateCertificate(string thumbPrint)
        {
            var eh = ValidateCertificate;

            if (eh == null)
            {
                return true;
            }

            var ea = new CertificateValidationEventArgs(thumbPrint);
            eh(this, ea);

            return ea.IsValid;
        }

        protected internal abstract FileSystemEntry GetListingImpl(Uri uri);
        protected internal abstract string[] ListDirectoryImpl(Uri uri);
        protected internal abstract FileSystemEntry[] ListFullDirectoryImpl(Uri uri);
        protected internal abstract Stream CreateFileImpl(Uri uri);
        protected internal abstract Stream OpenReadImpl(Uri uri);
        protected internal abstract void CreateDirectoryImpl(Uri uri);
        protected internal abstract void DeleteFileImpl(Uri uri);
        protected internal abstract void DeleteDirectoryImpl(Uri uri);

        /// <summary>
        /// Gets the listing entry for a single item, retrieving extra entry info such as entry type and file size.
        /// </summary>
        /// <param name="uri">The uri of the item to retrieve.</param>
        /// <returns>A filesystem entry.</returns>
        public FileSystemEntry GetListing(string uri)
        {
            return GetListingImpl(GetUri(uri));
        }

        /// <summary>
        /// Gets the listing entry for a single item, retrieving extra entry info such as entry type and file size.
        /// </summary>
        /// <param name="uri">The uri of the item to retrieve.</param>
        /// <returns>A filesystem entry.</returns>
        public FileSystemEntry GetListing(Uri uri)
        {
            return GetListingImpl(GetUri(uri));
        }

        /// <summary>
        /// Performs a full listing on the base directory, retrieving extra entry info such as entry type and file size.
        /// </summary>
        /// <returns>A collection of entries.</returns>
        public FileSystemEntry[] ListFullDirectory()
        {
            return ListFullDirectory(".");
        }

        /// <summary>
        /// Performs a full directory listing, retrieving extra entry info such as entry type and file size.
        /// </summary>
        /// <param name="uri">The uri of a directory to retrieve.</param>
        /// <returns>A collection of entries.</returns>
        public FileSystemEntry[] ListFullDirectory(string uri)
        {
            return ListFullDirectoryImpl(GetUri(uri));
        }

        /// <summary>
        /// Performs a full directory listing, retrieving extra entry info such as entry type and file size.
        /// </summary>
        /// <param name="uri">The uri of a directory to retrieve.</param>
        /// <returns>A collection of entries.</returns>
        public FileSystemEntry[] ListFullDirectory(Uri uri)
        {
            return ListFullDirectoryImpl(GetUri(uri));
        }

        /// <summary>
        /// Lists names of all entries in the base directory.
        /// </summary>
        /// <returns>A collection of entry names.</returns>
        public string[] ListDirectory()
        {
            return ListDirectory(".");
        }

        /// <summary>
        /// Lists names of all entries in a directory.
        /// </summary>
        /// <param name="uri">The uri of a directory to retrieve.</param>
        /// <returns>A collection of entry names.</returns>
        public string[] ListDirectory(string uri)
        {
            return ListDirectoryImpl(GetUri(uri));
        }

        /// <summary>
        /// Lists names of all entries in a directory.
        /// </summary>
        /// <param name="uri">The uri of a directory to retrieve.</param>
        /// <returns>A collection of entry names.</returns>
        public string[] ListDirectory(Uri uri)
        {
            return ListDirectoryImpl(GetUri(uri));
        }

        /// <summary>
        /// Creates a new file, overwriting any existing ones by the same name.
        /// </summary>
        /// <param name="uri">The uri of the file to create.</param>
        /// <returns>A writable Stream.</returns>
        /// <remarks>
        /// The stream returned by CreateFile is sequential by nature, and will not support seeking.
        /// </remarks>
        public Stream CreateFile(string uri)
        {
            return CreateFileImpl(GetUri(uri));
        }

        /// <summary>
        /// Creates a new file, overwriting any existing ones by the same name.
        /// </summary>
        /// <param name="uri">The uri of the file to create.</param>
        /// <returns>A writable Stream.</returns>
        /// <remarks>
        /// The stream returned by CreateFile is sequential by nature, and will not support seeking.
        /// </remarks>
        public Stream CreateFile(Uri uri)
        {
            return CreateFileImpl(GetUri(uri));
        }

        /// <summary>
        /// Opens an existing file.
        /// </summary>
        /// <param name="uri">The uri of the file to retrieve.</param>
        /// <returns>A readable Stream.</returns>
        /// <remarks>
        /// The stream returned by OpenRead is sequential by nature, and will not support seeking.
        /// </remarks>
        public Stream OpenRead(string uri)
        {
            return OpenReadImpl(GetUri(uri));
        }

        /// <summary>
        /// Opens an existing file.
        /// </summary>
        /// <param name="uri">The uri of the file to retrieve.</param>
        /// <returns>A readable Stream.</returns>
        /// <remarks>
        /// The stream returned by OpenRead is sequential by nature, and will not support seeking.
        /// </remarks>
        public Stream OpenRead(Uri uri)
        {
            return OpenReadImpl(GetUri(uri));
        }

        /// <summary>
        /// Creates a directory, and any non-existing directories in its path.
        /// </summary>
        /// <param name="uri">The uri of the directory to create.</param>
        public void CreateDirectory(string uri)
        {
            CreateDirectoryImpl(GetUri(uri));
        }

        /// <summary>
        /// Creates a directory, and any non-existing directories in its path.
        /// </summary>
        /// <param name="uri">The uri of the directory to create.</param>
        public void CreateDirectory(Uri uri)
        {
            CreateDirectoryImpl(GetUri(uri));
        }

        /// <summary>
        /// Deletes a file.
        /// </summary>
        /// <param name="uri">The uri of the directory to create.</param>
        public void DeleteFile(string uri)
        {
            DeleteFileImpl(GetUri(uri));
        }

        /// <summary>
        /// Deletes a file.
        /// </summary>
        /// <param name="uri">The uri of the directory to create.</param>
        public void DeleteFile(Uri uri)
        {
            DeleteFileImpl(GetUri(uri));
        }

        /// <summary>
        /// Deletes a directory.
        /// </summary>
        /// <param name="uri">The uri of the directory to create.</param>
        public void DeleteDirectory(string uri)
        {
            DeleteDirectoryImpl(GetUri(uri));
        }

        /// <summary>
        /// Deletes a directory.
        /// </summary>
        /// <param name="uri">The uri of the directory to create.</param>
        public void DeleteDirectory(Uri uri)
        {
            DeleteDirectoryImpl(GetUri(uri));
        }

        Uri GetDirectory(Uri uri)
        {
            uri = GetUri(uri);

            if (!uri.AbsolutePath.EndsWith("/"))
            {
                UriBuilder b = new UriBuilder(uri);
                b.Path += "/";
                uri = b.Uri;
            }

            return uri;
        }

        Uri GetUri(string uri)
        {
            if (uri == null) throw new ArgumentNullException("uri");

            Uri newUri = new Uri(BaseUri, uri);

            if (!string.Equals(newUri.GetLeftPart(UriPartial.Authority), authority, StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException("Absolute URIs must have the same base URI as the FileSystem instance.", "uri");
            }

            return newUri;
        }

        Uri GetUri(Uri uri)
        {
            if (uri == null) throw new ArgumentNullException("uri");

            if (!uri.IsAbsoluteUri)
            {
                uri = new Uri(BaseUri, uri);
            }
            else if (!string.Equals(uri.GetLeftPart(UriPartial.Authority), authority, StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException("Absolute URIs must have the same base URI as the FileSystem instance.", "uri");
            }

            return uri;
        }

        /// <summary>
        /// Creates a FileSystem instance for accessing files within a specific base URI.
        /// </summary>
        /// <param name="baseUri">The base URI to access files in.</param>
        /// <param name="credentials">Network credentials, if any, to perform authorization. May be null for URIs that do not need authorization.</param>
        /// <returns>A FileSystem object for the given URI.</returns>
        public static FileSystem Create(Uri baseUri, NetworkCredential credentials)
        {
            if (!baseUri.IsAbsoluteUri)
            {
                throw new ArgumentException("Base URI must be absolute.", "baseUri");
            }

            switch (baseUri.Scheme.ToLowerInvariant())
            {
                case "ftp":
                    return new FtpFileSystem(baseUri, credentials, false);
                case "ftps":
                    return new FtpFileSystem(baseUri, credentials, true);
                case "sftp":
                    return new SftpFileSystem(baseUri, credentials);
                case "file":
                    return new LocalFileSystem(baseUri);
                default:
                    throw new ArgumentException(string.Format("The URI scheme '{0}' is not supported.", baseUri.Scheme), "baseUri");
            }
        }

        /// <summary>
        /// Creates a new read-only FileSystem that throws exceptions on write operations.
        /// </summary>
        /// <param name="child">The child FileSystem to wrap.</param>
        public static FileSystem CreateReadOnly(FileSystem child)
        {
            return new ReadOnlyFileSystem(child);
        }

        /// <summary>
        /// Creates a new read-only FileSystem that either throws exceptions on write operations or ignores them.
        /// </summary>
        /// <param name="child">The child FileSystem to wrap.</param>
        /// <param name="throwOnWrite">If true, throw an exception on write operations. Otherwise, emulate the write operation but do nothing.</param>
        public static FileSystem CreateReadOnly(FileSystem child, bool throwOnWrite)
        {
            return new ReadOnlyFileSystem(child, throwOnWrite);
        }
    }

    /// <summary>
    /// Arguments passed via the FileSystem's ValidateCertificate event to validate a certificate thumbprint over a secure connection.
    /// </summary>
    public class CertificateValidationEventArgs : EventArgs
    {
        /// <summary>
        /// If set to True, the connection will continue. If false, the connection will fail and an exception will be thrown.
        /// </summary>
        public bool IsValid { get; set; }

        /// <summary>
        /// The thumbprint of the certificate used to establish a secure connection.
        /// </summary>
        public string Thumbprint { get; private set; }

        public CertificateValidationEventArgs(string thumbPrint)
        {
            this.Thumbprint = thumbPrint;
        }
    }

    /// <summary>
    /// A base FileSystemEntry of unknown type.
    /// </summary>
    public class FileSystemEntry
    {
        /// <summary>
        /// The absolute URI for the entry.
        /// </summary>
        public Uri Uri { get; private set; }

        /// <summary>
        /// The name of the entry.
        /// </summary>
        public string Name { get; private set; }

        public FileSystemEntry(Uri uri, string name)
        {
            this.Uri = uri;
            this.Name = name;
        }

        /// <summary>
        /// Retrieves the entry's name.
        /// </summary>
        public override string ToString()
        {
            return Name;
        }
    }

    /// <summary>
    /// A FileSystemEntry that is for a directory.
    /// </summary>
    public class DirectoryEntry : FileSystemEntry
    {
        internal DirectoryEntry(Uri uri, string name)
            : base(uri, name)
        {
        }
    }

    /// <summary>
    /// A FileSystemEntry that is for a file.
    /// </summary>
    public class FileEntry : FileSystemEntry
    {
        /// <summary>
        /// The size of the file, if available.
        /// </summary>
        public long? Size { get; private set; }

        /// <summary>
        /// The date the file was created, if available.
        /// </summary>
        public DateTime? DateCreated { get; private set; }

        /// <summary>
        /// The date the filew as modified, if available.
        /// </summary>
        public DateTime? DateModified { get; private set; }

        public FileEntry(Uri uri, string name, long? size, DateTime? dateCreated, DateTime? dateModified)
            : base(uri, name)
        {
            this.Size = size;
            this.DateCreated = dateCreated;
            this.DateModified = DateModified;
        }
    }
}
