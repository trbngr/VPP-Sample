using System;
using System.Collections;
using System.Security.Permissions;
using System.Web;
using System.Web.Caching;
using System.Web.Hosting;
using Lokad.Cqrs.Feature.StreamingStorage;

namespace VPP.PathProvider
{
    [AspNetHostingPermission(SecurityAction.Demand, Level = AspNetHostingPermissionLevel.Medium)]
    [AspNetHostingPermission(SecurityAction.InheritanceDemand, Level = AspNetHostingPermissionLevel.High)]
    public class CustomerPathProvider : VirtualPathProvider
    {
        private readonly IStreamingContainer _root;

        public CustomerPathProvider(IStreamingContainer root)
        {
            _root = root;
        }

        private static bool IsPathVirtual(string virtualPath)
        {
            return true;
        }

        public override bool FileExists(string virtualPath)
        {
            if (IsPathVirtual(virtualPath))
            {
                return CreateFile(virtualPath).Exists();
            }

            return Previous.FileExists(virtualPath);
        }

        public override VirtualDirectory GetDirectory(string virtualDir)
        {
            return new CustomerDirectory(_root, virtualDir);
        }

        public override VirtualFile GetFile(string virtualPath)
        {
            if (IsPathVirtual(virtualPath))
            {
                return CreateFile(virtualPath);
            }

            return Previous.GetFile(virtualPath);
        }

        public override CacheDependency GetCacheDependency(string virtualPath, IEnumerable virtualPathDependencies,
                                                           DateTime utcStart)
        {
            if (IsPathVirtual(virtualPath))
            {
                return null;
            }

            return Previous.GetCacheDependency(virtualPath, virtualPathDependencies, utcStart);
        }

        public override string GetFileHash(string virtualPath, IEnumerable virtualPathDependencies)
        {
            if (IsPathVirtual(virtualPath))
            {
                CustomerFile file = CreateFile(virtualPath);
                return file.ETag;
            }

            return Previous.GetFileHash(virtualPath, virtualPathDependencies);
        }

        private CustomerFile CreateFile(string virtualPath)
        {
            return new CustomerFile(_root, virtualPath);
        }

    }
}