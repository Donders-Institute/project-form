using System.IO;
using HandlebarsDotNet;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.FileProviders;

namespace Dccn.ProjectForm.Services
{
    public class TemplateFileSystem : ViewEngineFileSystem
    {
        private readonly IFileProvider _provider;
        private readonly string _basePath;

        public TemplateFileSystem(IHostingEnvironment environment, string basePath)
        {
            _provider = environment.ContentRootFileProvider;
            _basePath = basePath;
        }

        public override string GetFileContent(string fileName)
        {
            return File.ReadAllText(GetFileInfo(fileName).PhysicalPath);
        }

        protected override string CombinePath(string dir, string otherFileName)
        {
            return Path.Combine(dir, otherFileName);
        }

        public override bool FileExists(string filePath)
        {
            return GetFileInfo(filePath).Exists;
        }

        private IFileInfo GetFileInfo(string path)
        {
            return _provider.GetFileInfo(CombinePath(_basePath, path));
        }
    }
}