using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Web.Hosting;
using Lokad.Cqrs;
using Lokad.Cqrs.Feature.StreamingStorage;
using System.Linq;

namespace VPP.PathProvider
{
    public class CustomerDirectory : VirtualDirectory
    {
        private readonly IStreamingContainer _root;
        private readonly Lazy<IEnumerable<string>> _children;

        public CustomerDirectory(IStreamingContainer root, string virtualPath)
            : base(virtualPath)
        {
            _root = GetContainer(root, virtualPath);
            _children = new Lazy<IEnumerable<string>>(() => _root.ListItems());
        }

        public override IEnumerable Directories
        {
            get { return _children.Value.Where(s => s.EndsWith("/")); }
        }

        public override IEnumerable Files
        {
            get { return _children.Value.Except((IEnumerable<string>) Directories); }
        }

        public override IEnumerable Children
        {
            get { return _children.Value; }
        }

        private IStreamingContainer GetContainer(IStreamingContainer root, string virtualPath)
        {
            var parts = virtualPath.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);

            IStreamingContainer container = root.GetContainer(parts[0]);

            for (int i = 1; i < parts.Length - 1; i++)
            {
                var part = parts[i];
                container = container.GetContainer(part);
            }

            return container;
        }
    }

    public class CustomerFile : VirtualFile
    {
        private readonly IStreamingContainer _root;
        private readonly string _virtualPath;
        private readonly Lazy<IStreamingItem> _item;
        private readonly Lazy<Optional<StreamingItemInfo>> _itemInfo;
        private Stream _contents;

        public CustomerFile(IStreamingContainer root, string virtualPath)
            : base(virtualPath)
        {
            _contents = null;
            _root = root;
            _virtualPath = virtualPath.TrimStart('~');
            _item = new Lazy<IStreamingItem>(GetItem);
            _itemInfo = new Lazy<Optional<StreamingItemInfo>>(() => _item.Value.GetInfo());
        }

        public string ETag
        {
            get { return Exists() ? _itemInfo.Value.Value.ETag : ""; }
        }

        public string FullPath
        {
            get { return Exists() ? _item.Value.FullPath : ""; }
        }

        public override Stream Open()
        {
            if (_contents != null)
            {
                var info = _item.Value.GetInfo();
                if (info.HasValue && info.Value.ETag != ETag)
                {
                    return ReadStream();
                }

                return _contents;
            }

            return ReadStream();
        }

        private Stream ReadStream()
        {
            _contents = new MemoryStream();
            var item = _item.Value;
            item.ReadInto((props, s) => s.CopyTo(_contents));
            return _contents;
        }

        public bool Exists()
        {
            return _itemInfo.Value.HasValue;
        }

        private IStreamingItem GetItem()
        {
            var parts = _virtualPath.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);

            //make sure we have at least a directory and file
            if (parts.Length < 2)
            {
                return _root.GetContainer("File")
                            .GetItem("NotFound");
            }

            IStreamingContainer container = _root.GetContainer(parts[0]);

            for (int i = 1; i < parts.Length - 1; i++)
            {
                var part = parts[i];
                container = container.GetContainer(part);
            }

            var fileName = Path.GetFileName(_virtualPath);
            return container.GetItem(fileName);
        }

    }
}