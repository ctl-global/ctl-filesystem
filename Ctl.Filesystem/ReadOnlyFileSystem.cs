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
using System.Threading.Tasks;

namespace Ctl
{
    /// <summary>
    /// Provides a read-only FileSystem that either throws exceptions on write operations, or ignores them.
    /// </summary>
    /// <remarks>
    /// A non-throwing read-only FileSystem can be ued as a limited non-destructive test environment.
    /// </remarks>
    sealed class ReadOnlyFileSystem : FileSystem
    {
        readonly FileSystem child;
        readonly bool throwOnWrite;

        public ReadOnlyFileSystem(FileSystem child)
            : this(child, true)
        {
        }

        public ReadOnlyFileSystem(FileSystem child, bool throwOnWrite)
            : base(child.BaseUri)
        {
            if (child == null) throw new ArgumentNullException("child");

            this.child = child;
            this.throwOnWrite = throwOnWrite;
        }

        protected internal override FileSystemEntry GetListingImpl(Uri uri)
        {
            return child.GetListingImpl(uri);
        }

        protected internal override string[] ListDirectoryImpl(Uri uri)
        {
            return child.ListDirectoryImpl(uri);
        }

        protected internal override FileSystemEntry[] ListFullDirectoryImpl(Uri uri)
        {
            return child.ListFullDirectoryImpl(uri);
        }

        protected internal override Stream CreateFileImpl(Uri uri)
        {
            if (throwOnWrite) throw new InvalidOperationException("Read-only filesystems may not create files.");
            return Stream.Null;
        }

        protected internal override Stream OpenReadImpl(Uri uri)
        {
            return child.OpenReadImpl(uri);
        }

        protected internal override void CreateDirectoryImpl(Uri uri)
        {
            if (throwOnWrite) throw new InvalidOperationException("Read-only filesystems may not create directories.");
        }

        protected internal override void DeleteFileImpl(Uri uri)
        {
            if (throwOnWrite) throw new InvalidOperationException("Read-only filesystems may not delete files.");
        }

        protected internal override void DeleteDirectoryImpl(Uri uri)
        {
            if (throwOnWrite) throw new InvalidOperationException("Read-only filesystems may not delete directories.");
        }
    }
}
